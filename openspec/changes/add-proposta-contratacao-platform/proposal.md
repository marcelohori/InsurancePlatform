## Why

A seguradora não tem hoje nenhum sistema para gerenciar propostas de seguro e formalizar sua contratação. O processo de análise de proposta (aprovar/rejeitar) também não tem nenhum apoio à decisão, tornando a subscrição lenta e sujeita a inconsistência entre analistas. Este change entrega uma plataforma de microsserviços que cobre o ciclo completo: criação e análise de proposta, contratação vinculada a uma proposta aprovada, e um assistente de IA que recomenda aprovação/rejeição com base nos dados da proposta.

## What Changes

- Novo microsserviço `Proposta.Api`: CRUD completo de propostas de seguro (criar, listar, obter por id, atualizar status/dados, deletar), arquitetura hexagonal/DDD, persistência em `proposta_db` (PostgreSQL).
- Novo microsserviço `Contratacao.Api`: CRUD completo de contratações (criar, listar, obter por id, atualizar, deletar), valida a proposta associada via chamada REST síncrona a `Proposta.Api` antes de permitir a contratação, persistência em `contratacao_db` (PostgreSQL).
- Novo microsserviço `Analise.Api`: assistente de subscrição baseado em IA — consome o evento de proposta criada, chama um LLM para gerar score de risco, recomendação (Aprovar/Rejeitar) e justificativa, e expõe consulta por proposta, persistência em `analise_db` (PostgreSQL).
- Comunicação entre serviços via HTTP REST síncrono (verificação de status de proposta) e mensageria assíncrona via RabbitMQ/MassTransit (eventos de domínio entre os três serviços), com outbox/inbox pattern para entrega confiável.
- Infraestrutura transversal aplicada aos três serviços: resiliência (retry/timeout/circuit breaker) nas chamadas HTTP e ao LLM, autenticação JWT Bearer com papéis, observabilidade (log estruturado + tracing distribuído + health checks), versionamento de API e contrato de erro padronizado (Problem Details), idempotência na criação de contratação.
- Empacotamento via Docker (Dockerfile por serviço + docker-compose orquestrando os 3 serviços, PostgreSQL e RabbitMQ).
- Testes unitários (domínio/aplicação) e de integração (API + banco real via Testcontainers) para os três serviços.

## Capabilities

### New Capabilities
- `proposta`: gestão do ciclo de vida da proposta de seguro (criar, listar, obter, atualizar status, deletar) e publicação do evento de proposta criada.
- `contratacao`: contratação de seguro a partir de uma proposta aprovada (criar, listar, obter, atualizar, deletar), incluindo a verificação síncrona do status da proposta e idempotência na criação.
- `analise-risco-ia`: assistente de subscrição por IA que consome o evento de proposta criada, gera recomendação de aprovação/rejeição com score e justificativa, e disponibiliza essa análise por proposta.

### Modified Capabilities
(nenhuma — projeto greenfield, sem specs existentes)

## Impact

- **Novo código**: três soluções .NET 10 independentes (`Proposta`, `Contratacao`, `Analise`), cada uma com camadas Domain/Application/Infrastructure/Api (hexagonal) e um projeto compartilhado `BuildingBlocks.Contracts` para os payloads de evento.
- **Infraestrutura**: 3 bancos PostgreSQL (um por serviço), RabbitMQ, docker-compose para orquestração local.
- **Dependência externa**: provedor de LLM (ex.: Anthropic API) usado por `Analise.Api` — requer chave de API e tratamento de indisponibilidade via circuit breaker.
- **Sem sistemas legados afetados** — trata-se de uma plataforma nova, sem integração com sistemas pré-existentes.
