# Build context is the repository root (see docker-compose.yml), because this service's
# dependency graph spans src/Proposta/* and src/BuildingBlocks/Contracts, plus the solution-wide
# Directory.Build.props / Directory.Packages.props / .editorconfig that the build depends on.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Proposta/Proposta.Api/Proposta.Api.csproj"
RUN dotnet publish "src/Proposta/Proposta.Api/Proposta.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
# Run as non-root user for security: aspnet user is provided by the base image.
USER app
ENTRYPOINT ["dotnet", "Proposta.Api.dll"]
