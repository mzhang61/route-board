FROM postgres:17.11-bookworm

# Install the PostGIS extension for PostgreSQL 17.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       postgresql-17-postgis-3 \
    && rm -rf /var/lib/apt/lists/*