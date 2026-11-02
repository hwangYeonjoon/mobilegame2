FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Server/ScrapGuard.Server/ScrapGuard.Server.csproj Server/ScrapGuard.Server/
COPY Assets/Game/Core/ Assets/Game/Core/
RUN dotnet restore Server/ScrapGuard.Server/ScrapGuard.Server.csproj
COPY Server/ScrapGuard.Server/ Server/ScrapGuard.Server/
RUN dotnet publish Server/ScrapGuard.Server/ScrapGuard.Server.csproj -c Release --no-restore -o /published /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app
RUN mkdir /data /backups && chown app:app /data /backups
COPY --from=build --chown=app:app /published/ .
USER app
ENV ASPNETCORE_ENVIRONMENT=Production SERVER_URLS=http://+:8080 GAME_DB=/data/scrapguard.db DOTNET_EnableDiagnostics=0
EXPOSE 8080
ENTRYPOINT ["dotnet", "ScrapGuard.Server.dll"]
