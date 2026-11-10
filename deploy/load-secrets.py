"""Fetch deployment secrets through the host identity without printing secret values."""
import argparse
import json
import os
from pathlib import Path
import re
import urllib.error
import urllib.request


def read_json(opener, request):
    with opener.open(request, timeout=10) as response:
        payload = response.read(65537)
    if len(payload) > 65536:
        raise ValueError("Secret-service response exceeded the size limit.")
    return json.loads(payload)


def write_private(path, content, group=None):
    temporary = path.with_suffix(".new")
    descriptor = os.open(temporary, os.O_WRONLY | os.O_CREAT | os.O_TRUNC, 0o600)
    with os.fdopen(descriptor, "w") as stream:
        stream.write(content)
    if group is not None:
        os.chown(temporary, 0, group)
        temporary.chmod(0o440)
    temporary.replace(path)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("vault", help="Environment-specific Azure Key Vault name")
    parser.add_argument("directory", type=Path, help="Root-owned settings directory")
    arguments = parser.parse_args()
    if os.geteuid() != 0:
        parser.error("Run as root on the deployment host.")
    if not re.fullmatch(r"[a-zA-Z][a-zA-Z0-9-]{1,22}[a-zA-Z0-9]", arguments.vault):
        parser.error("Invalid Key Vault name.")
    directory = arguments.directory.resolve()
    directory.mkdir(parents=True, exist_ok=True, mode=0o700)
    directory.chmod(0o700)

    # Bypass proxy environment variables for the link-local metadata request.
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
    identity_url = ("http://169.254.169.254/metadata/identity/oauth2/token"
                    "?api-version=2018-02-01&resource=https%3A%2F%2Fvault.azure.net")
    identity = read_json(opener, urllib.request.Request(identity_url, headers={"Metadata": "true"}))
    token = identity.get("access_token")
    if not isinstance(token, str) or not token:
        raise ValueError("Managed identity did not return a token.")

    # Retrieve and validate everything before replacing any existing settings.
    values = {}
    for name in ("database-password", "runtime-password"):
        url = f"https://{arguments.vault}.vault.azure.net/secrets/{name}?api-version=7.4"
        request = urllib.request.Request(url, headers={"Authorization": "Bearer " + token})
        secret = read_json(opener, request).get("value")
        if not isinstance(secret, str) or not re.fullmatch(r"[a-f0-9]{64}", secret):
            raise ValueError("Expected a generated 32-byte hexadecimal database credential.")
        values[name] = secret
    for filename, username, secret_name in (
        ("migration.json", "routeboard_migrator", "database-password"),
        ("runtime.json", "routeboard_runtime", "runtime-password"),
    ):
        settings = {"ConnectionStrings": {"RouteBoard": (
            f"Host=db;Database=routeboard;Username={username};Password={values[secret_name]}"
        )}}
        # The API image runs as .NET's non-root UID/GID 1654.
        write_private(directory / filename, json.dumps(settings), group=1654)
    for name, value in values.items():
        write_private(directory / name, value, group=999 if name == "runtime-password" else None)
    print("Environment database secrets loaded; values were not logged.")


if __name__ == "__main__":
    try:
        main()
    except (urllib.error.URLError, ValueError, OSError, KeyError):
        # HTTP exception bodies and arbitrary remote payloads must not enter deployment logs.
        raise SystemExit("Secret loading failed. Check identity permissions and Key Vault availability.") from None
