
# RouteBoard

## Prerequisites

- .NET 10 SDK
- Node.js 26 LTS (required for the future frontend)
- Docker Desktop (required for the future database setup)

## Run the Backend Locally

From the repository root:

```bash
dotnet build RouteBoard.slnx
```

Start the API:

```bash
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=http://127.0.0.1:5080 \
dotnet run --project backend/RouteBoard.Api --no-launch-profile
```

## Verify the API

```bash
curl -i http://127.0.0.1:5080/healthz
curl -i http://127.0.0.1:5080/api/system
curl -i http://127.0.0.1:5080/api/unknown
```

Expected status codes: 200, 200, and 404.

## Architecture

See [over-all-structure.md](over-all-structure.md).
