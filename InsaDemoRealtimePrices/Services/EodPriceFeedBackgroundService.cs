using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using InsaDemoRealtimePrices.Hubs;
using InsaDemoRealtimePrices.Models;
using InsaDemoRealtimePrices.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace InsaDemoRealtimePrices.Services;

public sealed class EodPriceFeedBackgroundService : BackgroundService
{
    private readonly EodPriceFeedOptions _options;
    private readonly PriceSnapshotStore _snapshotStore;
    private readonly SqlServerPriceRepository _sqlRepository;
    private readonly IHubContext<PricesHub> _hubContext;
    private readonly ILogger<EodPriceFeedBackgroundService> _logger;

    public EodPriceFeedBackgroundService(
        IOptions<EodPriceFeedOptions> options,
        PriceSnapshotStore snapshotStore,
        SqlServerPriceRepository sqlRepository,
        IHubContext<PricesHub> hubContext,
        ILogger<EodPriceFeedBackgroundService> logger)
    {
        _options = options.Value;
        _snapshotStore = snapshotStore;
        _sqlRepository = sqlRepository;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _sqlRepository.EnsureTableCanBeOpenedAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQL table open check failed for InsaDemo.dbo.tbSecurityPriceFeed.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            using var ws = new ClientWebSocket();

            try
            {
                var wsUri = new Uri(_options.WebSocketUrl);
                _logger.LogInformation("Connecting to external feed: {WsUri}", wsUri);
                await ws.ConnectAsync(wsUri, stoppingToken);

                await SendSubscribeMessageAsync(ws, _options.Symbols, stoppingToken);
                await ReceiveLoopAsync(ws, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Feed connection failed. Reconnecting shortly.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.ReconnectDelaySeconds)), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private static async Task SendSubscribeMessageAsync(ClientWebSocket ws, string symbols, CancellationToken cancellationToken)
    {
        var subscribePayload = JsonSerializer.Serialize(new
        {
            action = "subscribe",
            symbols
        });

        var subscribeBytes = Encoding.UTF8.GetBytes(subscribePayload);
        await ws.SendAsync(subscribeBytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
    }

    private async Task ReceiveLoopAsync(ClientWebSocket ws, CancellationToken cancellationToken)
    {
        var buffer = new byte[8 * 1024];
        using var messageBuffer = new MemoryStream();

        while (ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var result = await ws.ReceiveAsync(buffer, cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", cancellationToken);
                break;
            }

            messageBuffer.Write(buffer, 0, result.Count);
            if (!result.EndOfMessage)
            {
                continue;
            }

            var payload = Encoding.UTF8.GetString(messageBuffer.GetBuffer(), 0, (int)messageBuffer.Length);
            messageBuffer.SetLength(0);

            await HandleFeedMessageAsync(payload, cancellationToken);
        }
    }

    private async Task HandleFeedMessageAsync(string payload, CancellationToken cancellationToken)
    {
        SecurityPriceTick? tick;

        try
        {
            tick = JsonSerializer.Deserialize<SecurityPriceTick>(payload);
        }
        catch (JsonException)
        {
            return;
        }

        if (tick is null || string.IsNullOrWhiteSpace(tick.Symbol))
        {
            return;
        }

        _snapshotStore.Upsert(tick);
        await _hubContext.Clients.All.SendAsync("PriceUpdated", tick, cancellationToken);
        await _sqlRepository.TryInsertAsync(tick, cancellationToken);
    }
}
