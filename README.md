# Santander - Dev Coding Test

RESTful API built with ASP.NET Core 9 that retrieves the best stories from the [Hacker News API](https://github.com/HackerNews/API), ordered by score descending.

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

## How to run

```bash
cd Santander.DevCodingTest.Api
dotnet run
```

The API will be available at `http://localhost:5283`.

## How to test

```bash
dotnet test
```

## Endpoint

### GET /stories?count={n}

Returns the best `n` stories from Hacker News ordered by score descending.

#### Parameters

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| count | int | Yes | Number of stories to retrieve. Must be greater than zero. |

#### Response 200

```json
[
  {
    "title": "A uBlock Origin update was rejected from the Chrome Web Store",
    "uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
    "postedBy": "ismaildonmez",
    "time": "2019-10-12T13:43:01+00:00",
    "score": 1716,
    "commentCount": 572
  }
]
```

### Response codes

| Code | Description |
| --- | --- |
| 200 | Success |
| 400 | Invalid or missing `count` parameter |
| 503 | Hacker News API unavailable and no cached data available |

## Architecture

The project follows a **Layered Architecture** with the following structure:

```
Santander.DevCodingTest.Api/
├── Controllers/        # HTTP layer — routing and response
├── Services/           # Business logic — caching, fallback, HN API calls
├── Models/             # Data contracts
├── Program.cs          # Dependency injection and pipeline
└── appsettings.json    # Configuration
```

## Design decisions

- **Parallel requests** — story details are fetched concurrently using `Task.WhenAll` to minimize latency.
- **Memory cache** — responses are cached for 5 minutes (configurable) to avoid overloading the Hacker News API.
- **Fallback cache** — a secondary cache with no expiration is kept as fallback. If the Hacker News API times out (5 seconds by default), the API returns the last known data instead of failing.
- **503 on empty fallback** — if the API times out and no cached data is available, the endpoint returns HTTP 503 with a clear error message.

## Configuration

All settings are in `appsettings.json`:

| Key | Default | Description |
| --- | --- | --- |
| `HackerNews:BaseUrl` | `https://hacker-news.firebaseio.com/v0/` | Hacker News API base URL |
| `HackerNews:TimeoutSeconds` | `5` | HTTP timeout in seconds |
| `HackerNews:CacheExpirationMinutes` | `5` | Cache TTL in minutes |

## Testing

The project uses **TDD** with the following test coverage:

| Layer | Scenarios |
| --- | --- |
| Service | Ordering, caching, parallel fetch, timeout fallback, empty cache error |
| Controller | Valid count, invalid count, service unavailable |
| Integration | Full HTTP request/response cycle via `WebApplicationFactory` |
