using InsaDemoRealtimePrices.Models;
using InsaDemoRealtimePrices.Services;
using Microsoft.AspNetCore.Mvc;

namespace InsaDemoRealtimePrices.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PricesController : ControllerBase
{
    private readonly PriceSnapshotStore _snapshotStore;

    public PricesController(PriceSnapshotStore snapshotStore)
    {
        _snapshotStore = snapshotStore;
    }

    [HttpGet("GetPrices")]
    public ActionResult<GetPricesResponse> GetPrices()
    {
        var prices = _snapshotStore.GetAll();

        return Ok(new GetPricesResponse
        {
            HubUrl = "/hubs/prices",
            SignalRGetPricesMethod = "GetPrices",
            SignalREventName = "PriceUpdated",
            CurrentSnapshot = prices
        });
    }
}

public sealed class GetPricesResponse
{
    public string HubUrl { get; set; } = "/hubs/prices";
    public string SignalRGetPricesMethod { get; set; } = "GetPrices";
    public string SignalREventName { get; set; } = "PriceUpdated";
    public IReadOnlyCollection<SecurityPriceTick> CurrentSnapshot { get; set; } = Array.Empty<SecurityPriceTick>();
}
