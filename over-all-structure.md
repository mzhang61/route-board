# RouteBoard

**Real-Time Shuttle Tracking and Dispatch Platform**

## 1. Project Overview

RouteBoard is a full-stack web application designed to simulate shuttle fleet operations, real-time vehicle tracking, route management, and estimated arrival times.

The application provides a public-facing arrival board and an authenticated dispatcher dashboard. Users can view routes, vehicle locations, scheduled trips, delays, and estimated arrival times. Authorized dispatchers can create trips, assign vehicles, and record operational events.

RouteBoard is designed as a production-oriented educational application emphasizing security, reliability, concurrency control, real-time communication, observability, and cloud deployment.

All vehicles, trips, operations, and GPS telemetry are synthetic. External routing and traffic services provide reference data only.

## 2. Technology Stack

| Component         | Technology                               |
| ----------------- | ---------------------------------------- |
| Backend           | ASP.NET Core 10 / C# 14                  |
| Frontend          | React + TypeScript                       |
| Database          | PostgreSQL + PostGIS                     |
| ORM               | Entity Framework Core                    |
| Mapping           | MapLibre GL JS                           |
| Real-Time Updates | Server-Sent Events (SSE)                 |
| GPS Simulation    | .NET Background Worker                   |
| Routing Provider  | Geoapify Routing API                     |
| Traffic Provider  | Azure Maps or TomTom (pending selection) |
| Authentication    | Microsoft Entra ID / OAuth 2.0 / OIDC    |
| Authorization     | Role-Based and Scope-Based Authorization |
| MCP               | Official C# MCP SDK + React MCP App      |
| Caching           | ASP.NET Core Memory Cache                |
| Testing           | xUnit, Integration Tests, Playwright     |
| Deployment        | Microsoft Azure                          |
| CI/CD             | GitHub Actions                           |
| Observability     | OpenTelemetry                            |
| Infrastructure    | Docker and Infrastructure as Code        |

## 3. Core Functional Requirements

### Public Arrival Board

- Browse shuttle routes and scheduled trips.
- Search and filter trips by route, stop, departure time, and status.
- View simulated vehicle locations on an interactive map.
- Monitor live vehicle location updates.
- View scheduled, estimated, and simulated observed arrival times.
- Display external traffic reference information with timestamps.
- Receive real-time trip updates without refreshing the page.
- Recover the correct application state after disconnection.

### Dispatcher Dashboard

- Authenticate using a real identity provider.
- Create and manage scheduled trips.
- Assign synthetic vehicles to trips.
- Record departure and stop-arrival events.
- Mark trips as delayed or canceled.
- Enforce valid trip-state transitions.
- Reject stale concurrent updates.
- View operational event history and audit records.

### Vehicle GPS Simulation

- Generate synthetic vehicle positions along predefined road routes.
- Include latitude, longitude, speed, heading, timestamps, and sequence numbers.
- Validate telemetry data before processing.
- Prevent stale GPS updates from replacing newer positions.
- Display moving vehicle markers on the map.
- Store the latest vehicle position and selected historical telemetry.

### Routing and Traffic

- Retrieve baseline road routes and travel durations from Geoapify.
- Retrieve traffic information from a supported third-party provider.
- Cache API responses according to provider limitations.
- Handle API timeouts, bounded retries, and service failures.
- Calculate estimated arrivals using travel time, dwell time, and recorded delays.
- Clearly distinguish simulated information from external traffic reference data.

## 4. System Architecture

RouteBoard will initially follow a modular monolith architecture.

The React frontend communicates with an ASP.NET Core REST API. PostgreSQL stores persistent application data, while PostGIS provides geospatial functionality.

A background worker generates simulated GPS observations. The backend validates and processes vehicle updates and publishes authorized operational changes to connected clients.

Server-Sent Events provide live updates. Persisted event identifiers enable replay or snapshot-based recovery after a client disconnects.

The MCP server exposes authorized read and write tools through the same business services used by the REST API.

The initial application will not require Kafka, Redis, or a separate microservices infrastructure.

## 5. Data Model

The initial relational model will include:

| Entity          | Responsibility                                   |
| --------------- | ------------------------------------------------ |
| Route           | Defines a fixed shuttle route                    |
| Stop            | Defines a geographic stop                        |
| RouteStop       | Defines ordered route stops and dwell times      |
| Vehicle         | Represents a synthetic shuttle                   |
| Trip            | Represents a scheduled shuttle journey           |
| TripEvent       | Records operational changes                      |
| VehiclePosition | Stores synthetic GPS telemetry                   |
| TrafficSnapshot | Stores cached external traffic reference data    |
| RouteEstimate   | Stores calculated baseline route information     |
| AuditEvent      | Records authorized application changes           |
| OutboxEvent     | Supports reliable event publication and recovery |

Database design will include primary keys, foreign keys, appropriate indexes, versioned migrations, and concurrency control.

## 6. Security Requirements

- OAuth 2.0 / OpenID Connect authentication.
- Authorization Code Flow with PKCE, state, and nonce.
- JWT validation of signature, issuer, audience, expiration, and scopes.
- Server-enforced viewer and dispatcher roles.
- Per-tool authorization for MCP operations.
- Confirmation before MCP write operations.
- Secure server-side management of API credentials.
- Rate limiting, input validation, CORS restrictions, and security headers.
- Append-only audit records with actors derived from validated authentication claims.

## 7. Reliability and Concurrency

RouteBoard must preserve consistency during concurrent dispatcher actions and asynchronous vehicle updates.

The application will use optimistic concurrency control for trip modifications, sequence validation for GPS telemetry, and persistent event history for real-time recovery.

External service failures must not prevent users from viewing existing trip schedules or dispatchers from performing supported local operations.

## 8. Deployment and Operations

The application will be deployed to Microsoft Azure with separate staging and production environments.

Required capabilities include HTTPS, DNS configuration, CDN caching, CI/CD, health checks, rollback, infrastructure reproducibility, monitoring, alerting, and cost controls.

Both environments will maintain separate application data and secrets.

## 9. Testing and Verification

Automated tests will cover:

- Input validation.
- Authentication and authorization.
- Core ETA calculations.
- Concurrent trip updates.
- GPS event ordering.
- Third-party API failures.
- SSE reconnection and event deduplication.
- MCP tool authorization.

The simulated GPS and live traffic extensions will be included in the project charter for instructor approval.

