# Staging route observability

Run scripts/observability/routing_probe.py from a virtual environment containing the pinned SDK. Find the client span's trace ID and span ID in client.txt. Search the API container log for the same trace ID. The server span's parent must match the client span, and the structured log should carry the server's span ID. The route counter groups by fresh, cached, stale or unavailable. Console metrics are cumulative and export periodically; they are not a per-request success denominator.

The probe is external to the application. It crosses public HTTPS ingress to ASP.NET. A cached route can complete the entire trace without contacting Geoapify, and this instrumentation does not trace the provider. The SLO is evaluated by the separate monitor so a failed request that never reaches the API still counts.

Codex predicted one 404 and two successes in the failure drill. The initial run instead produced three 403 responses. A direct diagnostic received Cloudflare error 1010 for the default Python client. The monitor now identifies itself with a project-specific User-Agent and requests JSON. A normal request with that identity returned 200. The original failed run is preserved in initial-403.txt.

Inspect the workflow summary for a table of samples, status codes, elapsed seconds and trace IDs. The dashboard covers staging and sampled route estimation only. No credentials are needed to reproduce the external request. Published server excerpts are restricted to routing telemetry; they exclude headers, URLs, request payloads and token values.
