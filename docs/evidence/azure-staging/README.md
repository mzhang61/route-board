# First Azure staging deployment

Application source: `4a6a94332cec65fbfeb80e66e7eff41fe11950b1`. Infrastructure source hashes are in source-manifest.txt; the infrastructure changes were uncommitted during provisioning. The user corrected the actual domain from `.net` to `.com` and added its staging A record.

## Verified

- ARM deployment returned Succeeded for the staging VM, network, IP, Key Vault and role assignments.
- Cloud-init completed; Docker/Compose and the metadata guard are active.
- Database secrets were fetched by the host managed identity. Values were never printed. The API receives its restricted runtime connection; the separate migration job receives the administrator connection.
- Migrations ran and runtime grants were applied. No database or API port is published; only ingress ports 80/443 are published.
- The public site https://staging.mzcampuspulse.com, /healthz and /api/trips returned 200. Anonymous POST /api/trips and POST /mcp returned 401. HTTP redirects to HTTPS.
- Python's standard TLS verification and curl succeeded without disabling certificate validation. Caddy obtained a Let's Encrypt certificate. certificate-event.json is an explicitly selected issuance event, not the entire server log; request logs containing visitor IP addresses were not committed.
- An application container could not access Azure metadata. The host identity could load secrets. runtime-check.txt records the actual denial and memory snapshot.

## Reproduction

Follow deploy/azure-staging.md for infrastructure, image transfer, secret loading and release steps. Public checks can be repeated with `curl -i https://staging.mzcampuspulse.com/healthz`, `curl -i -X POST https://staging.mzcampuspulse.com/api/trips`, `curl -I http://staging.mzcampuspulse.com`, and `dig staging.mzcampuspulse.com A`.

## Failure and limits

The first HTTPS checks failed while the certificate was being issued; release.txt retains those failures followed by the successful health gate. Earlier `.net` DNS never resolved and was corrected to the user's `.com` domain. An Entra app-registration request was refused because the user lacks directory permission; no authentication bypass was added.

This is a manual staging deployment, not production, a CDN, a full CI/CD promotion or a complete rubric pass. The public board is empty until a controlled synthetic catalog is initialized. Real OIDC login, Geoapify credentials, CAA, expiry alerts, backup/restore, staging-to-production promotion, rollback, SLOs and load evidence remain outstanding. A single memory snapshot does not prove capacity. Automated renewal is configured, but renewal/expiry alert demonstrations are not complete.

## Cost and redaction

At 730 hours, selected VM compute is 730 × USD 0.01 = USD 7.30; E4 SSD capacity is USD 2.40/month; Standard IPv4 is 730 × USD 0.005 = USD 3.65. Baseline total: USD 13.35/month, before disk operations, any applicable additional disk meters, Key Vault operations, traffic, backup, telemetry and tax. This is not a guaranteed total or proof of available credit. Stopping compute does not remove disk/IP charges.

Credentials, private SSH keys and CLI token caches remain outside Git. Artifact scanning checks the known generated credentials before publication. Public IPs, host public keys, resource IDs and domain names are deployment identifiers, not secrets. No live visitor request log or real rider information is included.
