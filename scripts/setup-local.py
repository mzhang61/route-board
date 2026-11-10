"""Create local development credentials without printing their values."""
import json
from pathlib import Path
import secrets
import subprocess

secret = Path(".local-secrets/postgres_password")
secret.parent.mkdir(mode=0o700, exist_ok=True)
if not secret.exists():
    secret.write_text(secrets.token_urlsafe(32))
    secret.chmod(0o600)
password = secret.read_text().strip().replace('"', '""')
connection = f'Host=127.0.0.1;Port=5433;Database=routeboard_dev;Username=routeboard;Password="{password}"'
result = subprocess.run(["dotnet", "user-secrets", "set", "--project", "backend/RouteBoard.Api"],
    input=json.dumps({"ConnectionStrings:RouteBoard": connection}), text=True, capture_output=True)
if result.returncode:
    raise SystemExit("Could not configure .NET User Secrets. Check the .NET SDK and project path.")
print("Local credentials configured. No secret values were printed.")
