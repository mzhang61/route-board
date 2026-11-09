# RouteBoard

RouteBoard is a simulated shuttle dispatch and real-time arrival board. For the system design, technology stack, and planned features, see [over-all-structure.md](over-all-structure.md).

## Prerequisites

- macOS on Apple Silicon (verified development environment); other platforms are not yet tested
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Node.js 26 LTS and npm (reserved for the future React frontend)
- Docker Desktop (reserved for the future PostgreSQL/PostGIS setup)
- Git

> **Current status:** Only the ASP.NET Core backend bootstrap is implemented. The frontend, database, authentication, GPS simulator, and external integrations are not yet available.

## Get the Source

```bash
git clone https://github.com/mzhang61/route-board.git
cd route-board
```

If you already have the repository, open a terminal at the existing repository root instead.

## Build the Backend

From the repository root:

```bash
dotnet --version
dotnet restore RouteBoard.slnx
dotnet build RouteBoard.slnx
```

The build should complete without errors.

## Run the Backend Locally

```bash
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=http://127.0.0.1:5080 \
dotnet run --project backend/RouteBoard.Api --no-launch-profile
```

The console should report `Hosting environment: Development` and `Now listening on: http://127.0.0.1:5080`.

The current local development setup uses HTTP over loopback only. Public staging and production deployments must use HTTPS.

## Verify the API

Keep the backend running and open a second terminal:

```bash
curl -i http://127.0.0.1:5080/healthz
curl -i http://127.0.0.1:5080/api/system
curl -i http://127.0.0.1:5080/api/unknown
```

Expected responses:

| Endpoint           | Expected HTTP Status | Expected Result                                              |
| ------------------ | -------------------- | ------------------------------------------------------------ |
| `GET /healthz`     | `200 OK`             | `Healthy`                                                    |
| `GET /api/system`  | `200 OK`             | JSON containing `service: RouteBoard API` and `status: running` |
| `GET /api/unknown` | `404 Not Found`      | Unknown endpoint                                             |

Stop the application with `Control + C` in the terminal running the server.

## Troubleshooting

- **`dotnet` not found:** Install the .NET 10 SDK and reopen the terminal.
- **Incorrect hosting environment:** Include `ASPNETCORE_ENVIRONMENT=Development` and `--no-launch-profile` as shown above.
- **Port 5080 already in use:** Stop any previous RouteBoard server process, then rerun the start command.
- **Cannot connect:** Verify the server is running, and call `127.0.0.1:5080` (not another port).

## Project Documentation

See [over-all-structure.md](over-all-structure.md) for architecture, requirements, planned technology choices, and scope limitations.