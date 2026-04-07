using InsaDemoRealtimePrices.Services;
using Microsoft.AspNetCore.SignalR;

namespace InsaDemoRealtimePrices.Hubs;

public sealed class PricesHub : Hub
{
    private readonly PriceSnapshotStore _snapshotStore;

    public PricesHub(PriceSnapshotStore snapshotStore)
    {
        _snapshotStore = snapshotStore;
    }

    public Task GetPrices()
    {
        var snapshot = _snapshotStore.GetAll();
        return Clients.Caller.SendAsync("PriceSnapshot", snapshot);
    }
}
