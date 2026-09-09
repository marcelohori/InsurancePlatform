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

## 2026-09-08

- **Contratacao** — `AddIdempotencyRequestHash`: tabela `idempotency_records` (chave, hash da requisição, `ContratacaoId`) para suportar `POST /contratacoes` idempotente via header `Idempotency-Key`.
- `InitialCreate` das três bases (`proposta_db`, `contratacao_db`, `analise_db`): schema inicial de `propostas`, `contratacoes` e `analises`, mais as tabelas de outbox/inbox do MassTransit (padrão *Transactional Outbox*).
- Commit inicial do repositório: solução completa com Arquitetura Hexagonal/Clean Architecture, 3 microsserviços, pipeline de CI/CD (build, testes, CodeQL, build de imagens Docker) e licença MIT.

---

> Este arquivo é gerado a partir de fatos verificáveis no código (timestamps de migration, histórico de `git log`). Para o changelog completo commit-a-commit, veja o histórico do repositório no GitHub.
