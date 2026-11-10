# AI provenance and challenged claims

The implementation in this local increment was generated with a Codex assistant and reviewed through builds and automated tests. The student supplied the project domain, existing code/migration, complete course requirements, English-documentation rule, readability preference, and domain name. The assistant inspected the actual local repository and preserved the previously applied InitialCoreSchema.

AI-generated work includes trip rules/services, REST endpoints, test fixtures, JWT configuration, replay, reference routing, simulation, React views, MCP tools/App, CI definition and documentation. This is not a claim that the student independently wrote those files or has already mastered them.

## Challenged claims and corrections

1. Earlier conversation text claimed Work Mode had been opened. This session did not verify a mode switch, so it makes no such claim; actual work occurred against the local repository.
2. The first generated request records put validation metadata on properties. Real integration tests failed because ASP.NET required constructor-parameter metadata for positional records. Attributes were corrected and tests rerun.
3. Test-host configuration was initially read before its override existed. The initial integration run failed. Configuration resolution was deferred to the registered database service.
4. MCP setup initially used mutable JSON options without a type-info resolver. Startup tests caught it; standard web serializer options fixed it.
5. A test assumed a shared fixture was empty and another used an unsupported GET for stateless MCP. Both assumptions were corrected rather than altering evidence to show success.
6. Sequence IDs were not assumed to prove commit order. The implementation serializes trip-event writers with a transaction lock and documents its throughput tradeoff.

## Evidence and review limits

Original failed local runs and the final successful run are retained in the evidence folder. No real login, Azure deployment, provider access, MCP host recording, performance improvement, budget, or fired alert has been claimed. A source manifest identifies the dirty working tree separately from its base commit SHA.

Operational credentials are loaded from .NET User Secrets or the ignored local Docker secret file. Test keys and the CI-only disposable database password are synthetic. A full independent scan of Git history, images, traces, screenshots, and client bundles is still required before final submission. That pending scan is not described as completed.

## Container deployment increment

AI authored ingress, Compose and Bicep scaffolding and ran real builds and local smoke checks. A mistaken HTTP/1-only status assertion failed against HTTP/2 and was corrected; both outputs are retained. Cloud runtime, real identity login, CDN and deployment gates remain unverified.

## Fresh-volume release increment

AI added explicit migration execution, restricted runtime grants and release scripts. Actual testing exposed bind-mount script execution and dynamic/static IP collision failures; SQL initialization and a reserved dynamic range corrected them. The permission test rejects audit tampering, trigger disabling, schema creation and replay deletion. A North Central US B1ms quota did not prove SKU availability: the subscription SKU API explicitly restricted it, challenging the assumption that quota alone means a machine can deploy. West US ARM alternatives were checked instead.

## First remote CI failure

GitHub run 42756859693 failed on Linux because a simulation test expected PostgreSQL to retain 100-nanosecond .NET ticks. The local macOS run did not expose that assumption. Normalizing observation timestamps to database microsecond precision before comparison also prevents a replay within the same stored microsecond from incrementing the sequence. The regression test deliberately includes sub-microsecond ticks and keeps exact assertions. The original remote failure is retained.
