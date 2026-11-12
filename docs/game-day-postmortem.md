# Game Day postmortem: disconnected staging arrival board

## Scope and prediction

One public browser viewed the staging Synthetic River Loop trip. The dispatcher used a separate direct connection. The drill deliberately closed the public browser's HTTPS tunnel while the dispatcher set the total delay twice. Before the test, the predicted final ETA was 10:27 UTC: departure at 10:00, legs of 8 and 12 minutes, 3 minutes of dwell, and a final total delay of 4 minutes. The scope was one synthetic trip and one viewer.

## Observed sequence

| Event | Impact and observation |
|---|---|
| Create event 5 | Trip 3 was at version 1. The last stop was due at 10:23. |
| Public connection interrupted | The browser showed the disconnected message. It remained open. |
| Delay events 6 and 7 | The dispatcher committed total delays of 2 and 4 minutes. The disconnected viewer received neither event during the outage. |
| Tunnel restored | The browser reconnected with Last-Event-ID 5 and received events 6 and 7 in order. Opening details showed version 3 and an ETA of 10:27. |

The trigger was a deliberate client-side network interruption. Writes and persisted history remained available. Recovery used the browser cursor and committed PostgreSQL snapshots. Timings in seconds were not captured.

## What needed correction

The initial trip 2 rehearsal failed an assertion: the ordinary browser request event did not expose the reconnect header. Switching the observer to Chromium's extra-info event made the actual wire header visible. A fresh run on trip 3 then verified the cursor, event versions and final persisted state. The first failed assertion alone could not distinguish a replay defect from a measurement defect.

## Follow-up and reproduction

Keep the wire-level assertion and compare final values against database history whenever the replay implementation changes. An already open detail view remains a versioned snapshot; reopen it to see the current version. The drill does not measure recovery after a full browser restart.

From the repository root run node frontend/tests/staging-replay.mjs with the ignored synthetic staging dispatcher fixture. The script creates one new trip. Run the read-only SQL in docs/evidence/staging-replay/verify.sql for the printed trip ID. The original browser output, expected results and SQL transcript are in docs/evidence/staging-replay at commit 41844aaf999a45598b2cd6376f6bd37122ddc65d. Never export the synthetic account password, browser session or access token with the report.
