# Sign-in, dispatch and live connection states

Tested commit `6e99a2caa57cd5f8be4518ebe965c2098c673e1e`. Run `bash scripts/test.sh` from the repository root after local setup. Playwright produces the original PNG files in `frontend/test-results/operator-states-*`. The [summary](test-summary.txt) contains selected lines from that run.

| Operation and prediction | Browser evidence |
| --- | --- |
| Delayed identity discovery should show progress. A 503 should offer another attempt. After discovery recovers, clicking Sign in again should reach the test provider page. | [Opening](sign-in-opening.png), [failed](sign-in-failed.png) |
| A forbidden creation should explain the missing dispatcher permission. A pending update should disable Save. A stale update should ask the dispatcher to review the trip and re-enable Save. | [Forbidden](dispatch-forbidden.png), [pending](dispatch-saving.png), [conflict](dispatch-conflict.png) |
| A stream error should display a reconnecting warning. Opening the stream again should replace that warning with the connected status. | [Disconnected](live-disconnected.png), [connected](live-reconnected.png) |

These tests exercise the local React interface with controlled responses, a synthetic browser session and explicitly delivered EventSource callbacks. Staging independently exercised [provider login and role enforcement](../staging-oidc/README.md) and [live stream replay](../staging-replay/README.md). AI prepared the tests and collected the screenshots. All displayed trips and roles are synthetic. The captures contain no live credentials.
