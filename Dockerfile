FROM node:26-alpine AS frontend
WORKDIR /src/frontend
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build && npm run build:mcp

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY backend/RouteBoard.Api/ backend/RouteBoard.Api/
COPY --from=frontend /src/frontend/dist-mcp/mcp-app.html backend/RouteBoard.Api/Assets/mcp-app.html
RUN dotnet publish backend/RouteBoard.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=backend /app ./
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RouteBoard.Api.dll"]
