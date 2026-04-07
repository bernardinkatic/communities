# InsaDemo Real-Time Security Prices API

This project exposes:

- `GET /api/prices/GetPrices` (Web API endpoint)
- `GET /health` (simple health check)
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

## Test client

A lightweight browser test client is included at:

- `/` (served from `wwwroot/index.html`)

What it does:

- Connects to SignalR hub `/hubs/prices`
- Invokes hub method `GetPrices` to get initial snapshot
- Listens to `PriceUpdated` and updates rows in real-time

How to use:

1. Start the API.
2. Verify the API is up:
   - `https://localhost:<port>/health` or `http://localhost:<port>/health`
3. Open `https://localhost:<port>/` (or `http://localhost:<port>/` if HTTPS disabled).
4. Click **Connect**.
5. Click **Request Snapshot** (or use **Auto-request snapshot on connect**).

### Troubleshooting

- If `/` returns not found, open `/index.html` directly.
- If `https://localhost:50640` is not available, run the app with the included launch profile:
  - HTTPS profile: `https://localhost:50640`
  - HTTP profile: `http://localhost:50641`
- With `dotnet run`, always use the URL printed in the startup logs (for example: `Now listening on: ...`).

