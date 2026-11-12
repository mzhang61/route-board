# Production reference routing

Production API revision: 72a41426553489940d4b91488af5e0a1bd0c9500. The production host loaded geoapify-api-key from its own vault using its managed identity. Recreate the API after atomically replacing its mounted runtime settings. See deploy/azure-staging.md for the loader procedure, substituting the production vault and settings directory.

Before querying, the expected cold result was fresh followed by cached, with identical positive distance, duration and retrieval time. To reproduce, request /api/catalog on the production site, select the ID with code RB-DEMO, and call /api/routes/{id}/estimate twice. Both calls can return cached while an existing entry is fresh.

The public health probe returned Healthy. The attached responses show the predicted cold and warm behavior. The values describe reference travel on a fixed synthetic route. The production provider was available throughout the check; fault injection evidence is in the staging-routing dossier. Codex ran the probes and checked the public response fields. No secret value or authenticated provider request URL was retained.
