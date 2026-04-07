using System.Collections.Concurrent;
using InsaDemoRealtimePrices.Models;

namespace InsaDemoRealtimePrices.Services;

public sealed class PriceSnapshotStore
{
    private readonly ConcurrentDictionary<string, SecurityPriceTick> _latestBySymbol = new(StringComparer.OrdinalIgnoreCase);

    public void Upsert(SecurityPriceTick tick)
    {
        if (string.IsNullOrWhiteSpace(tick.Symbol))
        {
            return;
        }

        _latestBySymbol[tick.Symbol] = tick;
    }

    public IReadOnlyCollection<SecurityPriceTick> GetAll()
        => _latestBySymbol.Values
            .OrderBy(t => t.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
