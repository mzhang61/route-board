# Staging route observability

Run scripts/observability/routing_probe.py from a virtual environment containing the pinned SDK. Find the client span's trace ID and span ID in client.txt. Search the API container log for the same trace ID. The server span's parent must match the client span, and the structured log should carry the server's span ID. The route counter groups by fresh, cached, stale or unavailable. Console metrics are cumulative and export periodically; they are not a per-request success denominator.

The probe is external to the application. It crosses public HTTPS ingress to ASP.NET. A cached route can complete the entire trace without contacting Geoapify, and this instrumentation does not trace the provider. The SLO is evaluated by the separate monitor so a failed request that never reaches the API still counts.

Codex predicted one 404 and two successes in the failure drill. The initial run instead produced three 403 responses. A direct diagnostic received Cloudflare error 1010 for the default Python client. The monitor now identifies itself with a project-specific User-Agent and requests JSON. A normal request with that identity returned 200. The original failed run is preserved in initial-403.txt.

Inspect the workflow summary for a table of samples, status codes, elapsed seconds and trace IDs. The dashboard covers staging and sampled route estimation only. No credentials are needed to reproduce the external request. Published server excerpts are restricted to routing telemetry; they exclude headers, URLs, request payloads and token values.

Verified deployment: 8a9b3b6e80b4f5f4a8ef98ac38ef51712f34b32b. Probe identification fix: 3993413. For trace e5485808a48992fe189c6a83e14703ee, the client span was e7008eef8d24a3d0. The API exported server span 8c5ab3fbe73f9313 with that client parent, and its structured log carried the same server span ID. The client observed 406 ms and ASP.NET processing took about 83 ms. The metrics interval counted one fresh and two cached estimates.

[Staging build and deploy](https://github.com/mzhang61/route-board/actions/runs/43164490900). [First monitor failure](https://github.com/mzhang61/route-board/actions/runs/43164571311). [Controlled alert and dashboard](https://github.com/mzhang61/route-board/actions/runs/43164970813). [Normal check and dashboard](https://github.com/mzhang61/route-board/actions/runs/43164974041). The controlled drill returned 404, 200, 200 and fired the workflow alert. The normal run passed all three samples. These were separate read-only checks; no public route was taken offline.
