# Keyboard and accessibility verification

The live staging application served revision ae17c4c5dd12159edf4a858d5c72999667e5aaee. The predictions were zero serious or critical axe violations, recovery from a failed search using Retry, and one persisted four-minute delay per successful keyboard submission. Both drivers create a fresh synthetic trip for their write check.

From the repository root, with frontend dependencies, Playwright Chromium and the ignored staging smoke identities provisioned:

    node frontend/tests/staging-accessibility.mjs
    MCP_EVIDENCE_DIR=docs/evidence/accessibility node frontend/tests/staging-mcp.mjs

The board driver uses real keyboard events and verifies the focused element before pressing Enter. No scripted focus or mouse clicks are used for the board walkthrough. It scans the populated board, empty search, detail panel, recoverable error, dispatcher creation form and update form. One trip-list request was deliberately returned as 503 within the test browser. Keyboard Retry recovered the live list. Creating the isolated fixture via REST is test setup; the dispatcher update is submitted using the keyboard.

The MCP driver signs in and sets up its host, then scans the actual served React document inside the sandboxed iframe. Tab reached Confirm, Tab reached Cancel, Shift+Tab returned to Confirm, and Enter saved the exact proposal. The readback and video document the outcome. Keyboard interaction in the MCP recording starts in the host with the proposal already loaded.

Across these seven states axe reported zero violations. The reports include the IDs of rules requiring further review. This covers Chromium and the listed tasks; a user study or screen-reader walkthrough was not performed. The first board scan stopped before delivering a result because axe needed an explicit Playwright browser context. Codex corrected the driver and reran it. The saved reports omit credentials and authentication traffic.
