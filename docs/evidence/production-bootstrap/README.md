Production uses a dedicated VM, managed identity, Key Vault and PostGIS volume. The production A record points to 52.225.26.107. The configured TTL of 600 seconds permits changing the origin with a ten-minute cache window.

The initial public release used API revision 72a41426553489940d4b91488af5e0a1bd0c9500 after checking the staging revision and the GitHub release asset digest. The ARM frontend was built separately from that source without staging login settings. The public catalog contains fixed synthetic reference data. Production login and the external routing credential are pending.

Verify with curl -fsS against /healthz and /api/catalog on the recorded site. An anonymous POST to /api/trips must return 401. Check HTTP redirection with curl -I against the HTTP site. Certificate checks initially failed while Caddy obtained its first certificate, then the HTTPS release gate passed. The public checks used normal certificate validation.
