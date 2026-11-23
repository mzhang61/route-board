# D3 — Staging revision not promoted to production

## Observed evidence

[`observations.txt`](observations.txt) preserves both the original 2026-11-19 02:08 UTC comparison and a later staging-only user-facing change observed at 04:46 UTC. The staging revision now is `db7ef584d6fe98978a9846d8c7515e957d589c3d`; production remains at `c91cf3caf0e9be08b1eb09f049d09e20fb2fbd58`. Both health checks returned `Healthy`.

GitHub Actions [run 44459864295](https://github.com/mzhang61/route-board/actions/runs/44459864295) shows successful `verify` and `staging` jobs for the newer commit. That commit adds explanatory copy to the dispatcher form: newly created trips use synthetic data and appear on the public demo board. Production was not promoted. The earlier documentation-only comparison remains as historical evidence.

## Reproduction

Open `/release.txt` and `/healthz` on both public URLs above. Open the GitHub Actions run and confirm that the push commit matches staging's `release.txt`, both `verify` and `staging` are successful, and production still reports the earlier revision. To view the changed copy on staging, sign in with a dispatcher account and open **Create a synthetic trip**. Do not promote this revision as part of the reproduction.

## Interpretation and limits

Staging and production are separate public environments with distinct release revisions and healthy endpoints. The staging-only copy change is user-visible after dispatcher sign-in, but does not alter core application behavior. The production deployment process is manual. These are point-in-time observations; repeat the endpoint checks if either environment has since been deployed.
