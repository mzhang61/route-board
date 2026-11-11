FROM node:26-alpine AS frontend
WORKDIR /src
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
ARG VITE_OIDC_AUTHORITY
ARG VITE_OIDC_CLIENT_ID
ARG VITE_OIDC_AUDIENCE
ARG VITE_OIDC_SCOPE
RUN npm run build

FROM caddy:2-alpine
COPY --from=frontend /src/dist /srv
COPY deploy/Caddyfile /etc/caddy/Caddyfile
