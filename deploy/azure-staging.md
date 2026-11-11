# Azure staging runbook

Staging is https://staging.mzcampuspulse.com on a small ARM VM. Production is not deployed. Keep its future resource group, vault, database, credentials and identity audience separate.

## Provision a new environment

1. Install Azure CLI, Docker with ARM image support, Python 3 and OpenSSH. Authenticate with `az login`. Select the intended subscription explicitly; do not use a university lab subscription by accident.
2. Create an environment resource group. Check allowed regions, SKU restrictions and the selected VM-family quota. A quota alone does not prove SKU availability.
3. Generate a dedicated SSH key in an ignored owner-only directory. Keep the private key out of Git. Prepare a private Bicep parameters file with environment, location, operatorObjectId, sshPublicKey and administratorCidr (your current IPv4 address plus `/32`). The operator ID is obtained with `az ad signed-in-user show --query id -o tsv`.
4. Compile `az bicep build --file deploy/host.bicep`. Deploy with `az deployment group create --subscription <subscription> --resource-group <group> --template-file deploy/host.bicep --parameters @<parameters-file>`. Record outputs and the exact source revision.
5. Confirm cloud-init status through Azure Run Command. Obtain `/etc/ssh/ssh_host_ed25519_key.pub` through that authenticated control plane and put the matching IP/public key in a dedicated known_hosts file. Use strict host-key checking for SSH. Do not blindly accept an unverified key.
6. Cloud-init installs Docker/Compose, prepares a small swap file, and blocks container access to Azure metadata. Confirm `systemctl is-active routeboard-metadata-guard` and the DOCKER-USER rejection rule. The administrator can still use the host identity; this is an explicit trust boundary.

## Secrets and application release

1. Generate two independent credentials with `secrets.token_hex(32)`: database-password and runtime-password. Store temporary copies only in the ignored private directory. Upload each using `az keyvault secret set --vault-name <vault> --name <name> --file <private-file> --query name -o tsv`. Do not put values in command arguments. This is initialization; do not overwrite existing database credentials as an accidental rotation.
2. Build API, web and PostgreSQL/PostGIS images for `linux/arm64`. The web build currently has no identity arguments until real OIDC setup is finished. Record image IDs and source SHA. Images can be transferred over verified SSH using `docker save ... | gzip | ssh ... 'sudo docker load'`; no registry password is required for this initial manual release.
3. Copy compose.yaml, initialize-runtime.sql, runtime-grants.sql, release.sh and load-secrets.py into a versioned host release directory under `/opt/routeboard/releases/`.
4. Run `sudo python3 <release>/deploy/load-secrets.py <vault-name> /etc/routeboard/staging` on the host. Managed identity fetches credentials without logging values. The API settings use group 1654; the PostgreSQL initializer reads its runtime password through group 999. Parent directories are root-only. Recheck these IDs when changing base images.
5. Create a root-owned mode-0600 release.env using the variable list in deploy/README.md. Use immutable local Docker image IDs or registry digests. SITE_ADDRESS is `staging.mzcampuspulse.com`. Never reuse production secret paths.
6. Configure its public A record to the deployed IP. Run `sudo bash <release>/deploy/release.sh staging /etc/routeboard/staging/release.env`. The script applies migrations separately, grants runtime privileges, starts the services and checks real HTTPS health. Certificate issuance may take longer than the first health window; inspect issuance logs before retrying. Do not disable TLS verification to make the release pass.

## Verify and diagnose

- Public /healthz and /api/trips should return 200; anonymous POST /api/trips and POST /mcp should return 401. Never disable authorization while waiting for an identity provider.
- HTTP should redirect to HTTPS. Validate the real certificate chain and domain, without `-k`. Do not mistake a local development certificate test for public TLS evidence.
- Only ingress ports 80/443 should be published. Container access to 169.254.169.254 must fail. The host secret-loader should succeed.
- Secret-loading failures leave existing files intact when fetching fails. Do not print HTTP response bodies or tokens to diagnose them. Check vault role assignment, identity, connectivity and the required hex credential format.
- The secret-loader writes each file atomically, but the entire file set is not a transactional credential rotation. Stop and plan rotation separately; the existing database password is not changed by loading new files.
- A failed initialization is not permission to remove an environment's database volume. Inspect the actual failure and repair it as administrator. The disposable test script is the only workflow that deletes its own test volumes.
- The small host is suitable only for initial testing until load measurements justify capacity. Swap is an emergency buffer, not extra performance capacity.

## Remaining operational work

CAA, demonstrated certificate-expiry alert, backups and restore, immutable staging-to-production promotion, rollback, CDN, OTel and alert evidence remain separate requirements. The current manual staging release is not full CI/CD. Review docs/requirements-status.md before claiming a grading pass.

## Reference routing credential for a new deployment

Create a Geoapify project and store its API key under the name geoapify-api-key in the environment's Key Vault. Use the Azure portal's Secrets form or a private secret-input mechanism. The host managed identity needs secret-read access. Run the loader with the routing flag on every release that enables routing:

    sudo python3 <release>/deploy/load-secrets.py <vault-name> /etc/routeboard/staging --with-geoapify

Recreate the API container with the environment's compose file and release.env after loading secrets. The runtime settings file is replaced atomically, so an existing single-file bind mount may still point to the previous file. The migration and web containers do not need the routing key. For grading, use your own Geoapify project and vault or access the configured staging site. Select the route ID from /api/catalog and request /api/routes/{id}/estimate twice.

## Automatic application releases

On main, the verification job must pass before the staging job builds ARM64 API and web images. The web build includes the commit SHA at /release.txt and public staging OIDC settings. The workflow publishes both images together in a prerelease named staging-<commit>. Public release assets contain application images only. The VM uses outbound HTTPS and its existing environment-specific configuration.

Bootstrap the existing staging host with deploy/compose.yaml, release.sh, runtime-grants.sql, apply-staging.sh and pull-staging.sh under /opt/routeboard/releases/staging-oidc/deploy. Install routeboard-staging.service and routeboard-staging.timer under /etc/systemd/system, run systemctl daemon-reload, then systemctl enable --now routeboard-staging.timer. Inspect journalctl -u routeboard-staging.service for exact image IDs. Changes to the host bootstrap, Compose, SQL grants or infrastructure require a separate reviewed host update.

The timer checks main every three minutes, verifies the GitHub asset digest, loads images, locks the deployment and prepares a private candidate configuration. A failed health or revision check triggers the previous image configuration. The workflow verifies both the public revision and /healthz. Use backward-compatible migrations because application rollback preserves database contents. Retain the previous images during the rollback window and check free disk before repeated releases. Production promotion is a separate deployment step.
