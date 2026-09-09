# InsurancePlatformV01

[![CI Pipeline](https://github.com/marcelohori/InsurancePlatformV01/actions/workflows/ci.yml/badge.svg)](https://github.com/marcelohori/InsurancePlatformV01/actions/workflows/ci.yml)
[![CodeQL](https://github.com/marcelohori/InsurancePlatformV01/actions/workflows/codeql.yml/badge.svg)](https://github.com/marcelohori/InsurancePlatformV01/security/code-scanning)
![.NET 10](https://img.shields.io/badge/.NET-10.0-blue)
![License](https://img.shields.io/badge/license-MIT-green)

Projeto de referência em .NET 10 — plataforma de gestão de propostas e contratação composta por três microsserviços independentes que seguem Arquitetura Hexagonal (Ports & Adapters), DDD e Clean Architecture.

Serviços e portas por padrão:

- Proposta.Api — CRUD de propostas e endpoints REST (porta local: 5080)
- Contratacao.Api — contratação a partir de propostas aprovadas (porta local: 5081)
- Analise.Api — análise de risco usando adaptador de IA (Anthropic) (porta local: 5082)

Componentes principais
- Comunicação: REST síncrono entre serviços e eventos assíncronos via RabbitMQ (MassTransit).
- Persistência: PostgreSQL (um banco por serviço). EF Core + migrations.
- Observabilidade: Serilog + OpenTelemetry (OTLP exporter).

Prerequisitos
- .NET 10 SDK
- Docker & Docker Compose (recomendado para ambiente de desenvolvimento)

Subir o ambiente local (Docker)
1. Copie `.env.example` para `.env` e ajuste valores sensíveis (JWT_SIGNING_KEY, POSTGRES_PASSWORD, RABBITMQ_PASSWORD, ANTHROPIC_API_KEY, senhas dos roles `*_migrator`/`*_app`).
2. Execute:

```bash
docker compose up --build
```

Isso sobe Postgres, RabbitMQ e os três serviços. Antes de cada API iniciar, o respectivo job `*-migrator` roda uma vez e aplica as migrations pendentes (ver seção "Migrations" abaixo); se você já tem um volume `postgres-data` de uma versão anterior deste projeto (sem os roles `*_migrator`/`*_app`), rode `docker compose down -v` primeiro para que o script de inicialização os crie.

Cada serviço expõe endpoints de liveness/readiness:

```bash
curl http://localhost:5080/health/ready   # Proposta.Api
curl http://localhost:5081/health/ready   # Contratacao.Api
curl http://localhost:5082/health/ready   # Analise.Api
```

Configuração e variáveis relevantes
- Variáveis de ambiente usadas pelo docker-compose e pelas APIs:
  - ConnectionStrings__AnaliseDb, ConnectionStrings__PropostaDb, ConnectionStrings__ContratacaoDb (usuário `*_app`, DML apenas)
  - ConnectionStrings__PropostaDbMigrator, ConnectionStrings__ContratacaoDbMigrator, ConnectionStrings__AnaliseDbMigrator (usuário `*_migrator`, DDL - usadas só pelos jobs `*.Migrator`)
  - RabbitMq__Host, RabbitMq__Port, RabbitMq__Username, RabbitMq__Password
  - Jwt__Issuer, Jwt__Audience, Jwt__SigningKey (mínimo recomendado: 32 bytes)
  - Anthropic__ApiKey, Anthropic__Model
  - RunMigrationsOnStartup (bool) - só tem efeito quando `ASPNETCORE_ENVIRONMENT=Development`; ver seção "Migrations" abaixo

Execução sem Docker (desenvolvimento)

  dotnet restore
  dotnet build InsurancePlatformV01.slnx

  # iniciar serviços (cada um em um terminal separado)
  dotnet run --project src/Proposta/Proposta.Api
  dotnet run --project src/Contratacao/Contratacao.Api
  dotnet run --project src/Analise/Analise.Api

Testes
- Unit tests:
  dotnet test tests/Proposta.UnitTests
  dotnet test tests/Contratacao.UnitTests
  dotnet test tests/Analise.UnitTests
- Integration tests (requer Docker/Testcontainers):
  dotnet test tests/Proposta.IntegrationTests
  dotnet test tests/Contratacao.IntegrationTests
  dotnet test tests/Analise.IntegrationTests

CI/CD Pipeline (GitHub Actions)

Cada push para `main` ou `develop` e todo PR dispara automaticamente:

1. **Build & Test** — Compila a solução, executa testes unitários e de integração
2. **Code Quality** — Análise estática com CodeQL (detecção de vulnerabilidades)
3. **Docker Build** — Constrói as 3 imagens de API (validação de Dockerfile)
4. **Test Report** — Publica resultados dos testes no GitHub

Workflow definido em `.github/workflows/ci.yml`. Status visível no badge acima.

Qualidade de código

- Format: `dotnet format --verify-no-changes InsurancePlatformV01.slnx`
- Build: `dotnet build InsurancePlatformV01.slnx`
- Vulnerabilidades: `dotnet list package --vulnerable` (executar periodicamente)
- A solução inclui analisadores (.NET analyzers, StyleCop) via Directory.Build.props
- CodeQL automatizado a cada push (GitHub Actions)

Migrations
- Cada serviço (Proposta, Contratacao, Analise) tem um projeto `*.Migrator` (`src/<Serviço>/<Serviço>.Migrator`) - um console app que só aplica `Database.MigrateAsync()` e sai; nunca é executado pela API.
- Os `Program.cs` das APIs **não** aplicam migrations em produção. O bloco `MigrateAsync()` só roda quando `ASPNETCORE_ENVIRONMENT=Development` **e** `RunMigrationsOnStartup=true` (já habilitado em `appsettings.Development.json`, para `dotnet run` local sem depender do Docker) - ambas as condições são checadas para que a flag nunca tenha efeito fora de Development, mesmo se definida por engano.
- Em produção/staging, rode o migrator explicitamente **antes** de subir/atualizar as réplicas da API, como um step de pipeline:
  ```bash
  docker compose run --rm proposta-migrator
  docker compose run --rm contratacao-migrator
  docker compose run --rm analise-migrator
  ```
  (em `docker compose up`, o `depends_on: ... condition: service_completed_successfully` já faz isso automaticamente uma vez, por conveniência local.)
- Coordenação: cada `*.Migrator` adquire um `pg_advisory_lock` antes de aplicar migrations e libera ao final - protege contra duas execuções concorrentes do mesmo job (ex.: retry de pipeline) tentando alterar o schema ao mesmo tempo.
- Usuários de banco: `docker/postgres-init/init-databases.sh` cria dois roles Postgres por serviço - `<serviço>_migrator` (owner do schema, único com permissão de DDL, usado só pelo Migrator) e `<serviço>_app` (somente SELECT/INSERT/UPDATE/DELETE, usado pela API em runtime). `ALTER DEFAULT PRIVILEGES` garante que tabelas criadas por futuras migrations já nasçam com DML liberado para o `_app`, sem regrant manual. Em produção real, esses roles/senhas devem ser provisionados pela infraestrutura (Terraform/DBA) e injetados via secrets manager, não pelo script de bootstrap do docker-compose (que só roda uma vez, no primeiro start de um volume vazio).

Observações operacionais e recomendações de segurança
- Não comitar segredos: appsettings.json e docker-compose possuem valores de exemplo. Use secrets manager (Azure Key Vault, HashiCorp, GitHub Secrets) ou dotnet user-secrets para desenvolvimento.
- JWT Signing Key: use chave forte (>32 bytes) em produção e roteie via secrets manager.
- Anthropic adapter: exige `Anthropic:ApiKey`. Em ambientes sem chave, configure um adapter noop/mock para evitar falha na inicialização.
- Health endpoints estão expostos sem autenticação — proteja por rede ou auth em produção.
- Evitar async-over-sync: há usos de `CreateConnectionAsync().GetAwaiter().GetResult()` em pontos de inicialização/health that devem ser revisados.

Observabilidade
- Serilog para logs estruturados; configure redaction para campos sensíveis.
- OpenTelemetry (OTLP) configurado; use variáveis `OTEL_EXPORTER_OTLP_ENDPOINT` / `OTEL_EXPORTER_OTLP_HEADERS` para apontar collector.

Estrutura do repositório

```
src/
  BuildingBlocks/Contracts/
  Proposta/    Proposta.Domain | Proposta.Application | Proposta.Infrastructure | Proposta.Api
  Contratacao/ Contratacao.Domain | Contratacao.Application | Contratacao.Infrastructure | Contratacao.Api
  Analise/     Analise.Domain | Analise.Application | Analise.Infrastructure | Analise.Api
tests/
  *.UnitTests
  *.IntegrationTests
  Architecture.Tests
```

Contribuição
- Abra uma issue para discutir mudanças.
- Faça fork, branch e PR; garanta que `dotnet build` e testes passem.

Licença
- Ver arquivo LICENSE (se presente) ou contate o mantenedor.

Referências e documentação adicional
- `openspec/changes/add-proposta-contratacao-platform` contém decisões arquiteturais e tarefas relacionadas ao change.
