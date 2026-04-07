using System.Text.Json.Serialization;

namespace InsaDemoRealtimePrices.Models;

public sealed class SecurityPriceTick
{
    [JsonPropertyName("s")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("ap")]
    public decimal AskPrice { get; set; }

    [JsonPropertyName("as")]
    public int AskSize { get; set; }

    [JsonPropertyName("bp")]
    public decimal BidPrice { get; set; }

    [JsonPropertyName("bs")]
    public int BidSize { get; set; }

    [JsonPropertyName("t")]
    public long FeedEpochMs { get; set; }
}
