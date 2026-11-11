# Staging reconnect drill

Prediction recorded before the drill: create a synthetic trip at 10:00 UTC on Synthetic River Loop. The final stop is scheduled for 10:23 UTC (8 + 3 + 12 minutes). Disconnect the public board after creation. The dispatcher sets the total delay to 2 minutes and then 4 minutes. On reconnect the browser should send its previous SSE cursor, receive versions 2 and 3 once each, and show a final ETA of 10:27 UTC with a total delay of 4 minutes.

Run from the repository root with the staging synthetic dispatcher fixture in the ignored local secret store:

    node frontend/tests/staging-replay.mjs

The drill creates one synthetic trip per run. A loopback tunnel forwards the public browser's HTTPS connection to staging. Closing its sockets interrupts only that browser. The dispatcher uses a separate direct connection.

## Observations and recovery

Repository baseline: 3c39def4ddc981e05238d1998e80a6cebb779bc8. GitHub Actions run 42958270486 passed. The staging drill exercised the public deployment. A binary-to-commit attestation was not collected during this drill.

The public board displayed its disconnected state while the separate dispatcher session committed two updates. The browser received no new trip events during the outage. After the tunnel reopened, Chromium sent Last-Event-ID 5. The browser received IDs 6 and 7 in order. The board row changed to delayed, and opening its details showed version 3 and the predicted ETA. Details already open before a disconnect are versioned snapshots and must be reopened. See browser.txt and database.txt for the observed results. For a repeat run, use the trip ID printed by the browser script:

    sudo docker exec -i routeboard-staging-db-1 psql -X -v ON_ERROR_STOP=1 -v trip=3 -U routeboard_migrator -d routeboard < verify.sql

Run this SQL through the staging administrator's SSH session with verify.sql on standard input. It reads only the synthetic event history.

## Test instrumentation failure and AI provenance

Codex wrote and ran the drill. The first run on trip 2 failed at the cursor assertion after the dispatcher committed both writes. Its observer assumed Chromium's ordinary request event would expose Last-Event-ID. The wire-level extra-info event exposed that header on the successful rerun using a fresh trip. This corrected the measurement without changing the application. Database snapshots independently corroborated the final values.

## Login capture and redaction

    node frontend/tests/staging-login-evidence.mjs

The HAR contains selected responses from a separate synthetic viewer login. Query values are redacted except for public OAuth settings. No request or response bodies, cookies, authorization headers, tokens, authorization codes, passwords or private key material were retained. Timing fields are placeholders required by the export format, not latency measurements. No production infrastructure was involved.
