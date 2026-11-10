# Oral-defense practice

Use these explanations as a study guide, not a script to memorize. Open the named files and perform the exercises yourself.

**Why ASP.NET Core and PostgreSQL/PostGIS?** ASP.NET provides mature authentication, validation, DI, and HTTP infrastructure. PostgreSQL supplies transactions, constraints, indexes, and advisory locks. PostGIS stores geographic stop points in SRID 4326 and supports spatial indexing. The current app does not yet perform a proximity search, so do not claim measured spatial-query speedups.

**Why a modular monolith?** It keeps one request and one transaction easy to inspect. Separate services would add network failures and distributed consistency without solving the current student's workload.

**Show the ETA by hand.** Departure 10:00, first leg 8 minutes, dwell 3 minutes, second leg 12 minutes: final arrival 10:23. Total delay 4 minutes produces 10:27. Point to `ArrivalCalculatorTests` before changing the implementation.

**What prevents lost updates?** The client supplies a version. The server compares it in the transaction and increments it on success; EF checks the same version during persistence. The parallel-request test expects one 200 and one 409 with exactly one additional audit and event.

**Why a global advisory lock as well as optimistic concurrency?** The lock serializes event allocation and commit order. Version checking still rejects a user submitting an old screen. Sequence IDs alone are not commit-order guarantees. The tradeoff is throughput; this is a measured-scaling concern, not a claim of unlimited capacity.

**Can an arrival skip a stop?** No. After departure the next order must equal the last observed order plus one. The final observed stop completes the trip. A canceled or completed trip rejects later changes.

**What is atomic?** The trip change, before/after audit, and replay event commit together. A failure rolls the transaction back. The public stream excludes the audit actor.

**Why SSE?** Public updates mostly flow server to client, so SSE is simpler than bidirectional sockets. It supplies a reconnection cursor. The app stores committed events and the browser deduplicates IDs. Snapshots contain total delay, so reconnecting does not add the same four minutes twice.

**How do you prove authentication?** Automated tests use locally generated RSA tokens to reject wrong signature, issuer, audience, and expiry. Role/scope tests reject a viewer or missing write scope. They do not prove real provider login or live JWKS rotation. Those require the actual provider and redacted login evidence, which is still pending.

**What do PKCE, state, and nonce do?** PKCE binds the authorization-code exchange to the initiating client. State associates the callback with the initiated login. Nonce binds the ID token to that login. The browser uses an OIDC library instead of implementing the protocol by hand. Access tokens are still validated independently by the API.

**Can hiding a button secure a write?** No. Every REST write requires the server policy; every MCP operation explicitly checks its tool scope. An attacker can call HTTP directly, so UI visibility is only convenience.

**How is MCP confirmation bound?** The protected token contains exact trip, version, update, actor, and expiry. Changing or reusing it after the version changes fails. The React App shows those values and sends the token only when its confirm button is clicked. The server can enforce operation binding; it cannot independently prove a human click in an untrusted host. Host-side consent and real host verification remain necessary.

**Are audits absolutely tamper-proof?** No. The append-only trigger blocks ordinary mutations, but an owner/admin can change the schema. A production runtime role must lack that authority, and operational access and backups need controls.

**What happens when Geoapify fails?** Existing schedules stay available. A timestamped cached reference becomes stale, or the UI says unavailable. There are at most two bounded attempts. A 429 does not trigger a retry storm. Stub tests prove these paths; no live-provider success is claimed.

**Is this live GPS or live traffic?** No. Vehicle positions are synthetic linear interpolation. They are labeled with timestamps and sequence numbers and persisted; older ticks are ignored. The reference routing API is separate from vehicle observations. No rider location is collected. Speed, heading, road-following geometry, and external traffic are not implemented.

**What does the route/departure index help?** It supports route-specific trip lookup ordered or filtered by departure. A query plan and realistic load benchmark are still needed before claiming a measured performance benefit.

**How would you change the schema?** Add a migration, inspect Up and Down, test on disposable PostgreSQL, and use a reviewed migration step before deployment. Do not rewrite InitialCoreSchema or roll the development database back to zero.

**How would production rollback work?** Promote the same immutable image/build through staging, gate on health and smoke tests, then restore the previous image on failure. Use backward-compatible schema changes or a reviewed restore plan; a code rollback is not automatically a data rollback. This procedure has not yet been demonstrated on Azure.

**Which evidence is still missing?** Real login HAR/JWKS rotation, real provider access/quota, full MCP-host recording, production URLs, DNS/TLS/CDN headers and alerts, Azure staging/production isolation, rollback, Core Web Vitals before/after, full keyboard walkthrough, distributed traces/SLO alert, and cost/budget evidence. Refer to the checklist instead of claiming completion.

## Live-change drills

1. Add a negative test for cancellation after completion.
2. Explain why changing `DelayMinutes = value` to `+= value` would be dangerous during retries.
3. Find where actor identity comes from and show why a client-supplied actor is ignored.
4. Change a dwell time and predict the new second-stop arrival.
5. Break the provider stub, explain the degraded UI, and restore it.
6. Find the SQL trigger and explain why a privileged database owner is still a trust boundary.

## Why trust only one reverse proxy?

TLS ends at the ingress, so the internal API receives HTTP. The trusted proxy supplies the original HTTPS scheme. Accepting these headers from arbitrary clients would allow spoofing; ignoring them causes incorrect redirects and scheme handling. The API trusts only the configured ingress address, and its port is private.

## Does a successful local HTTPS smoke test prove public TLS?

No. The local test deliberately accepts a development certificate. Public DNS, a trusted certificate chain, automated renewal, CAA and a fired expiry alert require separate deployed evidence.

## Why separate migration and runtime identities?

Migrations need schema-changing permissions that an API handling untrusted input must not have. The runtime receives only the data operations used by application services. A real PostgreSQL test shows it cannot rewrite audit history, disable triggers, create a table or delete replay events. The privileged migration owner remains a trust boundary.
