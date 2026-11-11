# Staging MCP drill

Predictions: anonymous requests receive HTTP 401. Each viewer read succeeds, while a viewer cannot preview or commit. A dispatcher previews 4 minutes. An override to 10 minutes must fail without changing the trip. A human confirmation in the embedded App advances the version exactly once. Reusing the confirmation must fail. The final total is 4 minutes.

## Reproduction and observations

Start with staging running and the two ignored synthetic fixtures provisioned as described in the staging identity evidence. From the repository root run:

    bash scripts/test.sh
    node frontend/tests/staging-mcp.mjs

The browser driver requests explicit MCP scopes during the real Auth0 authorization-code flow. The acceptance harness is a minimal MCP Apps test host. It loads the HTML from the authenticated resources/read endpoint into a sandboxed iframe, handles initialization and tool-result messages, and forwards its allowed tool call to staging. The recording covers the preview and confirmation in this host. Independent desktop-host discovery and consent have not been verified.

The previous commit 41844aaf999a45598b2cd6376f6bd37122ddc65d passed CI run 42960250178. Against that staging API, the assertion expecting a rejected 10-minute override failed on trip 4. The server ignored the extra field and applied the original four minutes. The corrected API rejects override arguments before writing and separates preview and record scopes. The successful run on fresh trip 5 is in transport.txt. The API release health gate passed after an initial 502 during startup. No schema migration was required.

The video shows the real React resource in the test host. The viewer read all three tools and was refused on the preview and on a valid confirmation. The dispatcher override was refused; an independent public GET still returned version 1 and delay 0. Clicking confirm advanced the trip to version 2 and delay 4. The replay failed and the version stayed at 2. Database output independently shows one delay event. A true scope_and_role_allowed entry records the authorization decision; later validation can still refuse the operation.

For the database check, pipe check.sql into psql on staging with variables baseline=4 and trip=5. For another run substitute its synthetic trip ID. The query intentionally omits actors and uses a short time window for tool calls, which can include other concurrent requests. Codex authored the harness, challenged the assumption that extra arguments would be rejected, ran both versions, and inspected raw results. Retained outputs contain synthetic trip IDs and statuses. The recorder starts after login in a separate browser context; credentials, bearer tokens and confirmation tokens are excluded from the evidence.
