# InsaDemo Real-Time Security Prices API

This project exposes:

- `GET /api/prices/GetPrices` (Web API endpoint)
- SignalR hub at `/hubs/prices`

It ingests live prices from:

- `wss://ws.eodhistoricaldata.com/ws/us-quote?api_token=demo` (default sample config)
- Subscription payload:
  - `{"action":"subscribe","symbols":"AAPL,MSFT,TSLA"}`

If you provide your own EODHD token, set it in `EodPriceFeed:WebSocketUrl`.
With the demo token, use supported symbols such as `AAPL`, `MSFT`, and `TSLA`.

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

## Test client

A lightweight browser test client is included at:

- `/` (served from `wwwroot/index.html`)

What it does:

- Connects to SignalR hub `/hubs/prices`
- Invokes hub method `GetPrices` to get initial snapshot
- Listens to `PriceUpdated` and updates rows in real-time

How to use:

1. Start the API.
2. Open `https://localhost:<port>/` (or `http://localhost:<port>/` if HTTPS disabled).
3. Click **Connect**.
4. Click **Request Snapshot** (or use **Auto-request snapshot on connect**).

