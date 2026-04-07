using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Globalization;
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
        IReadOnlyCollection<SecurityPriceTick> ticks;

        try
        {
            ticks = ParseTicks(payload);
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Unable to parse feed payload: {Payload}", payload);
            return;
        }

        if (ticks.Count == 0)
        {
            return;
        }

        foreach (var tick in ticks)
        {
            _snapshotStore.Upsert(tick);
            await _hubContext.Clients.All.SendAsync("PriceUpdated", tick, cancellationToken);
            await _sqlRepository.TryInsertAsync(tick, cancellationToken);
        }
    }

    private static IReadOnlyCollection<SecurityPriceTick> ParseTicks(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var ticks = new List<SecurityPriceTick>();
        CollectTicks(document.RootElement, ticks);
        return ticks;
    }

    private static void CollectTicks(JsonElement element, List<SecurityPriceTick> ticks)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (TryParseTick(element, out var tick))
                {
                    ticks.Add(tick);
                    return;
                }

                if (TryGetProperty(element, out var data, "data"))
                {
                    CollectTicks(data, ticks);
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectTicks(item, ticks);
                }

                break;
        }
    }

    private static bool TryParseTick(JsonElement element, out SecurityPriceTick tick)
    {
        tick = new SecurityPriceTick();

        if (!TryGetString(element, out var symbol, "s", "symbol") || string.IsNullOrWhiteSpace(symbol))
        {
            return false;
        }

        TryGetDecimal(element, out var askPrice, "ap", "askPrice", "a");
        TryGetDecimal(element, out var bidPrice, "bp", "bidPrice", "b");
        TryGetDecimal(element, out var tradePrice, "p", "price", "lastPrice");

        if (!askPrice.HasValue)
        {
            askPrice = tradePrice;
        }

        if (!bidPrice.HasValue)
        {
            bidPrice = tradePrice;
        }

        TryGetInt32(element, out var askSize, "as", "askSize");
        TryGetInt32(element, out var bidSize, "bs", "bidSize");
        TryGetInt32(element, out var tradeSize, "v", "size");

        if (!askSize.HasValue)
        {
            askSize = tradeSize;
        }

        if (!bidSize.HasValue)
        {
            bidSize = tradeSize;
        }

        TryGetInt64(element, out var feedEpochMs, "t", "ts", "feedEpochMs");

        tick = new SecurityPriceTick
        {
            Symbol = symbol.Trim(),
            AskPrice = askPrice ?? 0m,
            AskSize = askSize ?? 0,
            BidPrice = bidPrice ?? 0m,
            BidSize = bidSize ?? 0,
            FeedEpochMs = feedEpochMs ?? 0
        };

        return true;
    }

    private static bool TryGetProperty(JsonElement element, out JsonElement property, params string[] names)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var currentProperty in element.EnumerateObject())
            {
                foreach (var candidateName in names)
                {
                    if (string.Equals(currentProperty.Name, candidateName, StringComparison.OrdinalIgnoreCase))
                    {
                        property = currentProperty.Value;
                        return true;
                    }
                }
            }
        }

        property = default;
        return false;
    }

    private static bool TryGetString(JsonElement element, out string? value, params string[] names)
    {
        value = null;
        if (!TryGetProperty(element, out var property, names))
        {
            return false;
        }

        switch (property.ValueKind)
        {
            case JsonValueKind.String:
                value = property.GetString();
                return !string.IsNullOrWhiteSpace(value);

            case JsonValueKind.Number:
                value = property.ToString();
                return !string.IsNullOrWhiteSpace(value);

            default:
                return false;
        }
    }

    private static bool TryGetDecimal(JsonElement element, out decimal? value, params string[] names)
    {
        value = null;
        if (!TryGetProperty(element, out var property, names))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var numberValue))
        {
            value = numberValue;
            return true;
        }

        if (property.ValueKind == JsonValueKind.String &&
            decimal.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
        {
            value = parsedValue;
            return true;
        }

        return false;
    }

    private static bool TryGetInt32(JsonElement element, out int? value, params string[] names)
    {
        value = null;
        if (!TryGetProperty(element, out var property, names))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var numberValue))
        {
            value = numberValue;
            return true;
        }

        if (property.ValueKind == JsonValueKind.String &&
            int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
        {
            value = parsedValue;
            return true;
        }

        return false;
    }

    private static bool TryGetInt64(JsonElement element, out long? value, params string[] names)
    {
        value = null;
        if (!TryGetProperty(element, out var property, names))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var numberValue))
        {
            value = numberValue;
            return true;
        }

        if (property.ValueKind == JsonValueKind.String &&
            long.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
        {
            value = parsedValue;
            return true;
        }

        return false;
    }
}
