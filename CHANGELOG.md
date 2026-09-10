# 📜 Histórico do Projeto

Linha do tempo da evolução do schema e das principais entregas do InsurancePlatformV01, reconstruída a partir das migrations do Entity Framework Core e do histórico de commits do repositório. Para o detalhamento de decisões arquiteturais, veja [ARCHITECTURE.md](ARCHITECTURE.md).

## 2026-09-09

- **Proposta** — `AddCriadoPorDateCreationIndex`: índice composto (`CriadoPor`, `DataCriacao`) para acelerar a listagem paginada de propostas por usuário.
- **Proposta** — `AddIndexesAndConstraints`: índice em `DocumentoSegurado` e check constraints (`valor_cobertura >= 0`, `valor_premio >= 0`).
- **Contratacao** — `AddIndexesAndConstraints`: índice único em `NumeroApolice`, índice em `PropostaId` e check constraint (`valor_premio >= 0`).
- **Analise** — `AddIndexesAndConstraints`: índice único em `PropostaId` (uma análise por proposta) e check constraint (`score_risco` entre 0 e 100).
- **Proposta** — `AddPropostaOwner`: coluna `criado_por` para suportar a regra "usuário só vê/edita as próprias propostas" (exceto `analista`/`admin`).
- Correção do filtro global de `FluentValidation` (`FluentValidationFilter`): o registro de validators via `AddValidatorsFromAssembly` apontava para o assembly da API em vez do assembly `Application` onde os validators residem — os validators nunca eram encontrados pelo DI. Corrigido em Proposta.Api e Contratacao.Api.
- Correção de `CriarPropostaCommandValidator`/`AtualizarPropostaRequestValidator`: a validação de `TipoSeguro` usava uma lista de valores divergente do enum de domínio (`TipoSeguro { Auto, Vida, Residencial, Saude }`); substituída por `Enum.TryParse<TipoSeguro>`.
- Suíte de testes consolidada em **125 testes** (unit + integration + architecture) — 0 falhas.
- Novo endpoint **dev-only** `POST /api/dev/auth/token` no Proposta.Api (`src/Proposta/Proposta.Api/Endpoints/DevAuthEndpoints.cs`), mapeado só em `IsDevelopment()`, para gerar JWTs de teste sem precisar de jwt.io — com 3 testes de integração cobrindo o fluxo.
- Corrigido `.env.example`: o placeholder de `JWT_SIGNING_KEY` era, por coincidência, exatamente o valor que o guard de startup de cada API rejeita de propósito — trocado por um placeholder que não colide com o bloqueio.
- Adicionado `docker-compose.local.yml` (overlay opt-in via `-f`) expondo as portas de Postgres/RabbitMQ ao host, para permitir rodar os serviços com `dotnet run` fora da rede Docker sem alterar o comportamento seguro-por-padrão do `docker-compose.yml` principal.
- Seção "Sem Docker" do README reescrita e validada rodando os três serviços simultaneamente contra a infraestrutura Dockerizada (fluxo completo Proposta → Analise (evento) → Contratacao (HTTP síncrono) testado manualmente com sucesso), incluindo o alerta sobre `RunMigrationsOnStartup=true` exigir um role com permissão de DDL (não o role `*_app`, que só tem DML).
- Swagger UI (`Swashbuckle.AspNetCore.SwaggerUI`) adicionado às três APIs, mapeado apenas em `Development`, servindo a UI sobre o documento já gerado por `Microsoft.AspNetCore.OpenApi` (`/openapi/v1.json`). Inclui um `IOpenApiDocumentTransformer` (`BearerSecuritySchemeTransformer`) que declara o esquema JWT Bearer no documento, habilitando o botão "Authorize" da UI.

## 2026-09-08

- **Contratacao** — `AddIdempotencyRequestHash`: tabela `idempotency_records` (chave, hash da requisição, `ContratacaoId`) para suportar `POST /contratacoes` idempotente via header `Idempotency-Key`.
- `InitialCreate` das três bases (`proposta_db`, `contratacao_db`, `analise_db`): schema inicial de `propostas`, `contratacoes` e `analises`, mais as tabelas de outbox/inbox do MassTransit (padrão *Transactional Outbox*).
- Commit inicial do repositório: solução completa com Arquitetura Hexagonal/Clean Architecture, 3 microsserviços, pipeline de CI/CD (build, testes, CodeQL, build de imagens Docker) e licença MIT.

---

> Este arquivo é gerado a partir de fatos verificáveis no código (timestamps de migration, histórico de `git log`). Para o changelog completo commit-a-commit, veja o histórico do repositório no GitHub.
