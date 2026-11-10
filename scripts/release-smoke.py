"""Verify fresh-volume initialization and migration without touching development data."""
import json
import os
from pathlib import Path
import secrets
import subprocess
import tempfile
import time

ROOT = Path(__file__).resolve().parents[1]
PROJECT = "routeboard-release-check"


def run(arguments, **options):
    result = subprocess.run(arguments, check=True, capture_output=True, text=True, **options)
    return result.stdout


def write_settings(path, username, password):
    path.write_text(json.dumps({
        "ConnectionStrings": {
            "RouteBoard": f"Host=db;Database=routeboard;Username={username};Password={password}"
        },
        "ReverseProxy": {"Address": "172.30.98.2"},
    }))
    # The enclosing temporary directory is owner-only; the non-root container needs read access.
    path.chmod(0o644)


def main():
    with tempfile.TemporaryDirectory(prefix="routeboard-release-") as temporary:
        directory = Path(temporary)
        admin_password = secrets.token_hex(32)
        runtime_password = secrets.token_hex(32)
        (directory / "admin-password").write_text(admin_password)
        (directory / "runtime-password").write_text(runtime_password)
        write_settings(directory / "migration.json", "routeboard_migrator", admin_password)
        write_settings(directory / "runtime.json", "routeboard_runtime", runtime_password)
        environment = os.environ.copy()
        environment.update({
            "API_IMAGE": "routeboard-api:release-check",
            "WEB_IMAGE": "routeboard-web:deployment-check",
            "DB_IMAGE": "routeboard-postgis:17-local",
            "SITE_ADDRESS": "localhost",
            "OIDC_ORIGIN": "https://identity.example.test",
            "HTTP_PORT": "127.0.0.1:8090",
            "HTTPS_PORT": "127.0.0.1:8444",
            "APP_SUBNET": "172.30.98.0/24",
            "APP_DYNAMIC_RANGE": "172.30.98.128/25",
            "PROXY_IP": "172.30.98.2",
            "SETTINGS_FILE": str(directory / "runtime.json"),
            "MIGRATION_SETTINGS_FILE": str(directory / "migration.json"),
            "DATABASE_PASSWORD_FILE": str(directory / "admin-password"),
            "RUNTIME_PASSWORD_FILE": str(directory / "runtime-password"),
        })
        compose = ["docker", "compose", "--project-name", PROJECT,
                   "-f", str(ROOT / "deploy/compose.yaml")]
        try:
            run(compose + ["config", "--quiet"], env=environment)
            run(compose + ["up", "-d", "--wait", "--wait-timeout", "120", "db"], env=environment)
            print("Fresh isolated database is accepting connections.")
            for attempt in range(2):
                output = run(compose + ["--profile", "release", "run", "--rm", "migrate"], env=environment)
                assert "Database migrations completed." in output
                print(f"Migration command run {attempt + 1}: successful.")
            run(compose + ["exec", "-T", "db", "psql", "-U", "routeboard_migrator",
                           "-d", "routeboard", "--set", "ON_ERROR_STOP=1"],
                input=(ROOT / "deploy/runtime-grants.sql").read_text(), env=environment)
            run(compose + ["up", "-d", "api", "web"], env=environment)
            for attempt in range(30):
                response = subprocess.run(["curl", "-ksf", "https://localhost:8444/healthz"],
                                          capture_output=True, text=True)
                if response.returncode == 0:
                    break
                time.sleep(1)
            assert response.returncode == 0, "Runtime database health check failed."
            result = run(["curl", "-ksf", "https://localhost:8444/api/trips"])
            assert json.loads(result)["items"] == [], "Expected an empty trip board."
            print("Restricted runtime API: health 200 and empty board 200 over local HTTPS.")
            print("Local certificate bypass is test-only; public TLS remains unverified.")
        except subprocess.CalledProcessError as error:
            diagnostic = error.stderr or ""
            logs = subprocess.run(compose + ["logs", "--no-color", "db"],
                                  capture_output=True, text=True, env=environment)
            diagnostic += logs.stdout + logs.stderr
            for credential in (admin_password, runtime_password):
                diagnostic = diagnostic.replace(credential, "[REDACTED]")
            print(diagnostic)
            raise
        finally:
            run(compose + ["--profile", "release", "down", "--volumes", "--remove-orphans"], env=environment)
            print("Only disposable release-check containers and volumes were removed.")


if __name__ == "__main__":
    main()
