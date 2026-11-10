# Milestone 1: committed foundation verification

Tested commit: `69510b36cd9cdaface78353ec81b3a280f8ba834`. The working tree was clean during this verification. Evidence collected at 2026-11-10T04:35:38.467110+00:00. This evidence-only follow-up commit does not change application behavior.

## Commands and outcomes

1. `bash scripts/test.sh`: strict frontend build, MCP bundle, 2 browser tests and 36 backend tests passed. Raw output: tests.txt.
2. `docker build -t routeboard-api:release-check .`: succeeded; raw output and local image digest: api-build.txt.
3. `docker build -f deploy/web.Dockerfile -t routeboard-web:deployment-check .`: succeeded; raw output and local image digest: web-build.txt.
4. `python3 scripts/release-smoke.py`: fresh database initialization, two successful migration runs, runtime grants, HTTPS health and empty-board checks passed. Temporary volumes removed. Raw output: fresh-release.txt.

Predictions are recorded in docs/test-predictions.md. The new runtime-permission test deliberately rejects audit UPDATE/DELETE/TRUNCATE, trigger disabling, schema creation and replay deletion with PostgreSQL 42501.

## Failures and interpretation

Earlier working-tree attempts failed because a mounted initialization shell script could not execute, and because a dynamically allocated container IP collided with the fixed proxy IP. Their diagnostic output is retained in initialization-failure.txt and network-failure.txt. SQL initialization and a separate dynamic IP allocation range fixed them. These failures occurred before the tested commit; they are not failures of the committed final run.

The fresh-volume test uses local Docker and a development certificate with `curl -k`. It does not prove public TLS, Azure deployment, CDN behavior, real OIDC or a successful remote GitHub workflow. Those require separate evidence. Local image digests are not a claim that images have been published to a registry.

## Evidence integrity and redaction

Raw tool output is retained, including trailing whitespace emitted by Vite. The evidence path opts out of whitespace linting so evidence need not be edited to satisfy a formatting check. Source formatting checks remain enabled.

A pre-commit scan of 117 candidate files found no known local database password, private-key block, GitHub-token pattern or pasted Azure device code. The fresh-database test keeps randomly generated credentials out of command arguments and replaces them in failure diagnostics. Secrets and Azure login caches remain ignored outside the commit. This is a scoped check, not an exhaustive security audit of all historical images or external systems.
