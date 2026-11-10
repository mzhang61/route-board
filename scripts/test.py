"""Run tests with a local database credential kept out of command arguments and logs."""
import os
from pathlib import Path
import subprocess
import sys

environment = os.environ.copy()
if not environment.get("ROUTEBOARD_TEST_ADMIN_CONNECTION"):
    secret = Path(".local-secrets/postgres_password")
    if not secret.is_file():
        sys.exit("Set ROUTEBOARD_TEST_ADMIN_CONNECTION to a test PostgreSQL administrator connection.")
    password = secret.read_text().strip().replace('"', '""')
    environment["ROUTEBOARD_TEST_ADMIN_CONNECTION"] = (
        'Host=127.0.0.1;Port=5433;Database=postgres;Username=routeboard;'
        f'Password="{password}";Include Error Detail=false'
    )
for command, directory in [(["npm", "run", "build"], "frontend"),
                           (["python3", "scripts/build-mcp.py"], "."),
                           (["npm", "test"], "frontend")]:
    subprocess.run(command, cwd=directory, check=True)
result = subprocess.run(["dotnet", "test", "RouteBoard.slnx", "--logger", "console;verbosity=normal"], env=environment)
sys.exit(result.returncode)
