"""Check the verified public TLS certificate on each RouteBoard environment."""
import argparse
import socket
import ssl
import sys
from datetime import datetime, timedelta, timezone

HOSTS = ("routeboard.mzcampuspulse.com", "staging.mzcampuspulse.com")


def check(host, threshold):
    with socket.create_connection((host, 443), timeout=10) as connection:
        with ssl.create_default_context().wrap_socket(connection, server_hostname=host) as tls:
            expires = tls.getpeercert()["notAfter"]
    expiry = datetime.fromtimestamp(ssl.cert_time_to_seconds(expires), timezone.utc)
    remaining = expiry - datetime.now(timezone.utc)
    print(f"{host} expires {expiry.isoformat()}, {remaining.days} full days remaining")
    if remaining <= timedelta(days=threshold):
        print(f"::error::{host} certificate is within the {threshold}-day alert window")
        return False
    return True


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--drill", action="store_true", help="Exercise the alert with a 365-day window")
    args = parser.parse_args()
    healthy = True
    for hostname in HOSTS:
        try:
            if not check(hostname, 365 if args.drill else 21):
                healthy = False
        except (OSError, ValueError) as error:
            print(f"::error::{hostname} TLS check failed: {error}")
            healthy = False
    sys.exit(0 if healthy else 1)
