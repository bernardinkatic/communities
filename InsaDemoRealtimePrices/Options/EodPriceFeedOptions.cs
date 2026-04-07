namespace InsaDemoRealtimePrices.Options;

public sealed class EodPriceFeedOptions
{
    public const string SectionName = "EodPriceFeed";

    public string WebSocketUrl { get; set; } = "wss://ws.eodhistoricaldata.com/ws/us?api_token=69b27001577999.54629717";

    public string Symbols { get; set; } = "AMZN,TSLA";

    public int ReconnectDelaySeconds { get; set; } = 5;
}
