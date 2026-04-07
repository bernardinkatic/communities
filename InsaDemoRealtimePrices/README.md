# InsaDemo Real-Time Security Prices API

This project exposes:

- `GET /api/prices/GetPrices` (Web API endpoint)
- SignalR hub at `/hubs/prices`

It ingests live prices from:

- `wss://ws.eodhistoricaldata.com/ws/us?api_token=69b27001577999.54629717`
- Subscription payload:
  - `{"action":"subscribe","symbols":"AMZN,TSLA"}`

## SQL Server

Connection string key: `ConnectionStrings:InsaDemo`

Default configuration uses Windows Authentication:

`Server=localhost;Database=InsaDemo;Integrated Security=true;TrustServerCertificate=true;Encrypt=false;`

On startup, the service attempts to open:

- `InsaDemo.dbo.tbSecurityPriceFeed`

Every received tick is also inserted with this expected schema:

- `Symbol` (nvarchar)
- `AskPrice` (decimal)
- `AskSize` (int)
- `BidPrice` (decimal)
- `BidSize` (int)
- `FeedEpochMs` (bigint)
- `ReceivedAtUtc` (datetimeoffset)

## SignalR contract

- Server push event: `PriceUpdated`
- Snapshot event (after client invokes hub method): `PriceSnapshot`
- Hub method: `GetPrices`

