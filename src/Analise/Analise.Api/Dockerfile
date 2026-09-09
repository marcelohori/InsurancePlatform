# Build context is the repository root (see docker-compose.yml) - same rationale as
# Proposta.Api's Dockerfile.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Analise/Analise.Api/Analise.Api.csproj"
RUN dotnet publish "src/Analise/Analise.Api/Analise.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
# Run as non-root user for security: aspnet user is provided by the base image.
USER app
ENTRYPOINT ["dotnet", "Analise.Api.dll"]
