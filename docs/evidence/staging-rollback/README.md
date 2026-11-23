# Staging rollback drill

This is a real, staging-only rollback drill. On 2026-11-19 UTC, commit `a96c84e7cbcadec1ee90274fb2df1ff4052c55a7` temporarily changed the CI workflow to build a staging web image whose `release.txt` did not match its commit SHA. The API and production configuration were not changed. The staging host's timer picked up the candidate and ran its normal health and revision checks.

The staging host journal records a transient 502 during candidate startup, followed by a passing health check for the prior release and `Previous staging release recovered.` The prior public staging revision was `02a8388d32b85d232b4ac2addc5dc1fffa84fa51`. The candidate did not become the active staging release. The corresponding GitHub Actions run was cancelled after the restore commit superseded it; it is labeled cancelled, not failed. Its automated `verify` job completed successfully.

The original marker was restored in commit `12798944841ff5fdce4b8b55695158e4aff1252a`. GitHub Actions run [44454513027](https://github.com/mzhang61/route-board/actions/runs/44454513027) passed both `verify` and `staging`. At the post-restore check, staging served that restore commit from `/release.txt`, and `/healthz` returned `Healthy`. Production remained at `c91cf3caf0e9be08b1eb09f049d09e20fb2fbd58` and returned `Healthy`.

## Later staging deployment

After the rollback drill, the separate D3 demonstration change was deployed to staging by GitHub Actions [run 44459864295](https://github.com/mzhang61/route-board/actions/runs/44459864295). The latest observed staging revision is `db7ef584d6fe98978a9846d8c7515e957d589c3d`; production remains at `c91cf3caf0e9be08b1eb09f049d09e20fb2fbd58`. Both `/healthz` endpoints returned `Healthy` in the 2026-11-19 04:46 UTC comparison recorded in [`../staging-gate/observations.txt`](../staging-gate/observations.txt). This later deployment does not change what the rollback drill proved; it updates which revision is currently on staging.

## Raw observations

Sources: GitHub Actions run details and Azure Portal **Run command** output on the staging VM. The journal excerpt is copied from the VM's `systemctl status` output; account, subscription, network addresses, and unrelated log lines are omitted.

```text
GitHub Actions #43: verify completed successfully (1m 57s).
GitHub Actions #43: staging image build (42s) and versioned image publication (6s) completed; its public revision/health verification was still running when cancelled because the higher-priority restore commit superseded it.

Staging VM journal, 2026-11-19 UTC:
03:51:30 wget: server returned error: HTTP/1.1 502 Bad Gateway
03:51:33 Release health check passed.
03:51:33 Previous staging release recovered.
03:51:33 routeboard-staging.service exited with status 1 after rejecting the candidate.

GitHub Actions #44, restore commit 12798944841ff5fdce4b8b55695158e4aff1252a:
verify completed successfully (2m 06s).
staging completed successfully (3m 19s).
Workflow-reported image IDs: API sha256:7ebddfb7027d2f41fdecb2ea2da10309b362b106455c417197c3ddd0b1d2d676; web sha256:a7dfe5ae36d1c6c62c293e76a2b5a6e5e861845e7f73971f960f90afa14b3fec.

Public check immediately after the rollback restore run:
https://staging.mzcampuspulse.com/release.txt -> 12798944841ff5fdce4b8b55695158e4aff1252a
https://staging.mzcampuspulse.com/healthz -> Healthy
https://routeboard.mzcampuspulse.com/release.txt -> c91cf3caf0e9be08b1eb09f049d09e20fb2fbd58
https://routeboard.mzcampuspulse.com/healthz -> Healthy
```

## Interpretation and reproduction

The staging image can pass its general health check yet still be rejected when its published revision marker does not equal the commit SHA. The host retains the previous image configuration, restores it on rejection, and verifies the recovered service. The deployment workflow then verifies the restored revision and `/healthz`. Production promotion is a separate manual operation and was not triggered.

To review the implementation, inspect [the CI workflow](../../../.github/workflows/ci.yml), [the staging pull timer](../../../deploy/routeboard-staging.timer), [the pull script](../../../deploy/pull-staging.sh), and [the apply/restore script](../../../deploy/apply-staging.sh). The public Actions records are [drill run #43](https://github.com/mzhang61/route-board/actions/runs/44453642923) and [restore run #44](https://github.com/mzhang61/route-board/actions/runs/44454513027). The timer's VM journal is the raw rollback proof; the GitHub run is explicitly cancelled because the restore commit superseded it.

## Limits

This validates rollback of the running application images, not database contents. Schema changes still need to remain backward-compatible. The candidate startup briefly returned 502 before the previous release passed its health check. The drill used the production-like staging environment only; no production release was attempted.
