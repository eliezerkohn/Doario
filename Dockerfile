# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Install Node.js for React build
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs

WORKDIR /src

# Copy solution and restore
COPY . .
RUN dotnet restore Doario.Web/Doario.Web.csproj

# Publish (this runs npm install + npm run build automatically)
RUN dotnet publish Doario.Web/Doario.Web.csproj -c Release -o /app/out

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app
COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Doario.Web.dll"]
