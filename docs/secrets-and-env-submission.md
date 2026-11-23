# Secrets and environment files for course submission

## Local application run

The instructor does not need the student's local secrets. From a clean clone, run `python3 scripts/setup-local.py`. It generates `.local-secrets/postgres_password` on that computer and stores the API connection string in that user's .NET User Secrets store. The script does not write credentials into source code or print them. The local startup does not require a `.env` or `.env.example` file, an OIDC account, Azure credentials, or a Geoapify key. Public browsing works without OIDC; protected writes remain protected.

There is no `.env` or `.env.example` file in this project. `ROUTEBOARD_DB_PORT` is an optional environment variable and defaults to 5433. The `VITE_OIDC_*` entries are public build-time settings used only for a configured login flow. Never put a client secret, database password, Geoapify key, or cloud credential in a Vite variable.

## Optional local integrations

- OIDC: obtain the authority/domain, client ID, and API audience from the identity provider's application/API settings. Register the exact local callback origin. The client ID is public; do not provide a client secret to the browser.
- Geoapify: obtain an API key from the Geoapify account dashboard. Store it server-side in .NET User Secrets as `Geoapify:ApiKey`; it is not required for basic local startup.
- Test database administrator: `ROUTEBOARD_TEST_ADMIN_CONNECTION` is only needed when the test suite cannot use the generated local database credentials. Use a disposable PostgreSQL role permitted to create test databases.

## Cloud-only credentials

Staging and production use separate Azure Key Vaults and must remain separate. The deployment host retrieves application secrets through its managed identity. The deployment procedure references `database-password`, `runtime-password`, and optionally `geoapify-api-key`. These values are not needed to run the project locally. Do not export or submit Azure CLI login state, SSH private keys, access tokens, or the `.local-secrets/` directory. Give the instructor cloud access only through the course's approved identity/access process; do not bundle administrative credentials into an env file.

## Safe handling

Submit this explanation as configuration documentation if requested. The instructor can generate a separate local database password by running the setup script. If the instructor explicitly requires a live cloud secret, confirm the exact secret name and use only the private submission mechanism provided by the course. Never submit `.local-secrets/`, Azure CLI login state, actual `.env` files, or secret values through the repository or a public channel.
