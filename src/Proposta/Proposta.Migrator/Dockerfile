# Build context is the repository root (see docker-compose.yml) - same reasoning as
# src/Proposta/Proposta.Api/Dockerfile.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Proposta/Proposta.Migrator/Proposta.Migrator.csproj"
RUN dotnet publish "src/Proposta/Proposta.Migrator/Proposta.Migrator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Run as non-root user for security: aspnet user is provided by the base image.
USER app
ENTRYPOINT ["dotnet", "Proposta.Migrator.dll"]
