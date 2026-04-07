using InsaDemoRealtimePrices.Models;
using Microsoft.Data.SqlClient;

namespace InsaDemoRealtimePrices.Services;

public sealed class SqlServerPriceRepository
{
    private readonly ILogger<SqlServerPriceRepository> _logger;
    private readonly string _connectionString;

    public SqlServerPriceRepository(IConfiguration configuration, ILogger<SqlServerPriceRepository> logger)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("InsaDemo")
            ?? throw new InvalidOperationException("Connection string 'InsaDemo' is missing.");
    }

    public async Task EnsureTableCanBeOpenedAsync(CancellationToken cancellationToken)
    {
        const string sql = "SELECT TOP 1 * FROM [dbo].[tbSecurityPriceFeed];";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteScalarAsync(cancellationToken);

        _logger.LogInformation("Connected to SQL Server and opened table dbo.tbSecurityPriceFeed successfully.");
    }

    public async Task TryInsertAsync(SecurityPriceTick tick, CancellationToken cancellationToken)
    {
        const string sql = """
                           INSERT INTO [dbo].[tbSecurityPriceFeed]
                               ([Symbol], [AskPrice], [AskSize], [BidPrice], [BidSize], [FeedEpochMs], [ReceivedAtUtc])
                           VALUES
                               (@Symbol, @AskPrice, @AskSize, @BidPrice, @BidSize, @FeedEpochMs, @ReceivedAtUtc);
                           """;

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Symbol", tick.Symbol);
            command.Parameters.AddWithValue("@AskPrice", tick.AskPrice);
            command.Parameters.AddWithValue("@AskSize", tick.AskSize);
            command.Parameters.AddWithValue("@BidPrice", tick.BidPrice);
            command.Parameters.AddWithValue("@BidSize", tick.BidSize);
            command.Parameters.AddWithValue("@FeedEpochMs", tick.FeedEpochMs);
            command.Parameters.AddWithValue("@ReceivedAtUtc", DateTimeOffset.UtcNow);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Some environments may use a different table schema; keep broadcasting even if persistence fails.
            _logger.LogWarning(ex, "Unable to insert tick into dbo.tbSecurityPriceFeed.");
        }
    }
}
