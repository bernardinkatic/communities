namespace InsaDemoRealtimePrices.Options;

public sealed class EodPriceFeedOptions
{
    public const string SectionName = "EodPriceFeed";

    public string WebSocketUrl { get; set; } = "wss://ws.eodhistoricaldata.com/ws/us-quote?api_token=demo";

    public string Symbols { get; set; } = "AAPL,MSFT,TSLA";

    public int ReconnectDelaySeconds { get; set; } = 5;
}
