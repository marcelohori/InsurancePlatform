## Context

Projeto greenfield (sem código ou specs pré-existentes). Stack obrigatória definida pelo usuário: .NET 10 / C#, PostgreSQL, arquitetura hexagonal, Clean Architecture, DDD, SOLID, Docker, testes unitários e de integração. Ver `proposal.md` para a motivação de negócio (gestão de propostas e contratação de seguro, apoiada por um assistente de IA de subscrição).

## Diagrama da Arquitetura

Topologia entre os três serviços:

```
                         +-------------------+
                         |     RabbitMQ        |
                         |  (MassTransit)       |
                         +----+--------+--------+
                    PropostaCriadaEvent |  ContratacaoEfetuadaEvent
                              |         |
        +---------------------+        +----------------------+
        |                                                      |
+-------v---------+     REST (GET status,      +---------------v---+
|  Proposta.Api     |<---- Polly retry/CB) -----|  Contratacao.Api   |
|  (Hexagonal/DDD)  |                            |  (Hexagonal/DDD)   |
|  proposta_db        |                          |  contratacao_db      |
+-------^---------+                            +---------------------+
        | PropostaCriadaEvent (consumido)
        |
+-------+---------+
|  Analise.Api       |   --> chama LLM (Anthropic API) com Polly
|  (Hexagonal/DDD)   |       retry/timeout/circuit breaker
|  analise_db          |
+---------------------+

Todos os 3 servicos: JWT Bearer auth, Serilog + OpenTelemetry,
health checks, versionamento /api/v1, ProblemDetails, outbox+inbox.
```

Forma hexagonal dentro de cada serviço (exemplo: `Contratacao.Api`):

```
                     +-----------------------------------+
                     |         Contratacao.Api             |
                     |   (adapter inbound: Controllers)     |
                     +------------------+------------------+
                                        |
                     +------------------v------------------+
                     |      Contratacao.Application          |
                     |  Casos de uso + Portas (interfaces)    |
                     |  IContratacaoRepository                |
                     |  IPropostaVerificationPort             |
                     |  IEventPublisher                       |
                     +----+---------------+---------------+---+
                          |               |               |
              +-----------v---+  +--------v-------+  +----v-----------+
              | EF Core Adapter |  | HttpClient      |  | MassTransit     |
              | (Postgres)      |  | Adapter          |  | Outbox Adapter   |
              | contratacao_db  |  | -> Proposta.Api    |  | -> RabbitMQ      |
              +-----------------+  +-----------------+  +-----------------+
                     Adapters de saida (outbound) - implementam as portas
```

O núcleo (`Domain` + `Application`) nunca depende de `Infrastructure` ou `Api` — a dependência aponta sempre para dentro, característica central do padrão Ports & Adapters.

## Goals / Non-Goals

**Goals:**
- Três microsserviços independentes (`Proposta.Api`, `Contratacao.Api`, `Analise.Api`), cada um hexagonal/DDD por dentro, com fronteiras de bounded context respeitadas (nada de banco compartilhado entre eles).
- Comunicação entre serviços consistente: REST síncrono onde a resposta é necessária no mesmo request (verificação de proposta), eventos assíncronos onde o consumidor pode reagir depois (notificação de proposta criada, notificação de contratação efetuada).
- Entrega confiável de eventos (outbox) e consumo idempotente (inbox), resiliência a falhas de rede/dependência externa, autenticação, observabilidade, versionamento e idempotência de escrita aplicados uniformemente aos três serviços.
- Ambiente local reproduzível via Docker Compose.

**Non-Goals:**
- Autenticação/emissão de token (IdentityServer, OAuth flows completos) — assume-se um emissor de JWT já configurado; os serviços apenas validam o token.
- Interface de usuário (frontend) — escopo é somente as APIs.
- Escalonamento horizontal, deploy em Kubernetes, CI/CD — fica para um change futuro.
- Cobertura de todos os tipos de seguro do mercado — `TipoSeguro` é um enum extensível, não uma tabela de configuração dinâmica.

## Decisions

### 1. Um microsserviço por bounded context, banco por serviço
Cada serviço (`Proposta`, `Contratacao`, `Analise`) tem seu próprio banco PostgreSQL (`proposta_db`, `contratacao_db`, `analise_db`) na mesma instância Postgres do docker-compose, mas sem cruzar schemas. Alternativa considerada: banco único compartilhado — descartada por acoplar os serviços a um schema comum e violar o isolamento de bounded context do DDD, tornando cada serviço não deployável/evoluível de forma independente.

### 2. Hexagonal por dentro de cada serviço
Estrutura por serviço:
```
<Servico>.Domain          entidades, value objects, enums, eventos de dominio
<Servico>.Application     casos de uso + portas (interfaces): IRepository, IEventPublisher, portas outbound especificas
<Servico>.Infrastructure  adapters: EF Core (Postgres), MassTransit (RabbitMQ), HttpClient (chamadas REST), cliente LLM
<Servico>.Api             adapter inbound (controllers), composition root (DI, Program.cs), Dockerfile
```
`Contratacao.Application` define uma porta `IPropostaVerificationPort`; `Contratacao.Infrastructure` implementa essa porta com um `HttpClient` para `Proposta.Api`. `Analise.Application` define `IRiskAssessmentPort`; `Analise.Infrastructure` implementa com o cliente do provedor de IA. Isso mantém a camada de aplicação sem dependência de framework HTTP ou SDK de IA.

### 3. REST síncrono para verificação, evento assíncrono para notificação
`POST /contratacoes` chama `GET /api/v1/propostas/{id}` em `Proposta.Api` de forma síncrona porque a resposta (aprovada ou não) é necessária antes de decidir se a contratação pode ser criada — não há como adiar essa decisão para depois. Já `PropostaCriadaEvent` e `ContratacaoEfetuadaEvent` são assíncronos porque seus consumidores (análise de risco, futura auditoria) não bloqueiam o fluxo principal do serviço que os publica.

### 4. RabbitMQ + MassTransit para mensageria
Alternativa considerada: Kafka — descartado por ser overkill para o volume e o padrão pub/sub simples necessário aqui; RabbitMQ com MassTransit é o padrão idiomático em .NET, com suporte nativo a outbox (`AddEntityFrameworkOutbox`) e inbox (idempotent consumers), o que resolve os requisitos de entrega confiável sem código de infraestrutura manual.

### 5. Outbox no publisher, inbox no consumer
Cada serviço que publica um evento grava o evento na tabela de outbox na mesma transação EF Core da escrita de domínio (ex.: criar proposta + gravar evento no outbox = uma transação). Um processo de despacho do MassTransit publica os eventos pendentes no RabbitMQ. `Analise.Api`, ao consumir `PropostaCriadaEvent`, usa o inbox pattern do MassTransit (`AddEntityFrameworkOutbox` com deduplicação por `MessageId`) para garantir que reentregas do broker não disparem duas análises para a mesma proposta.

### 6. Resiliência via Polly + `HttpClientFactory`
As duas dependências de rede síncronas e sujeitas a falha — `Contratacao.Api -> Proposta.Api` e `Analise.Api -> provedor de IA` — usam `HttpClientFactory` nomeado com políticas Polly: retry com backoff exponencial (3 tentativas), timeout por tentativa, e circuit breaker (abre após N falhas consecutivas). Quando o circuito está aberto ou as tentativas se esgotam:
- Em `Contratacao.Api`: a criação da contratação é rejeitada com Problem Details 503, sem criar registro parcial.
- Em `Analise.Api`: a análise é marcada como `Falha` (não fica pendente indefinidamente), e a proposta segue disponível para decisão manual.

### 7. Idempotência de escrita via header `Idempotency-Key`
`POST /contratacoes` aceita `Idempotency-Key`. O adapter de persistência grava a chave junto com o resultado da primeira execução bem-sucedida (mesma transação da criação da contratação); uma repetição com a mesma chave retorna a resposta já registrada sem executar a lógica de criação novamente. Isso protege contra duplicidade de apólice quando o cliente reenvia a requisição após timeout de rede.

### 8. Autenticação: JWT Bearer validado por cada serviço
Os três serviços validam o mesmo JWT (emitido por uma autoridade externa ao escopo deste change) via `AddAuthentication().AddJwtBearer(...)`, com claims de papel (`Analista`, `Cliente`). Chamadas serviço-a-serviço (`Contratacao -> Proposta`) propagam o token do chamador original (token forwarding) em vez de usar um token de serviço separado — mais simples para o escopo atual, sem exigir um fluxo client-credentials adicional.

### 9. Observabilidade: Serilog + OpenTelemetry + health checks
Log estruturado com Serilog (sink de console em JSON, pronto para agregação futura). Tracing distribuído com OpenTelemetry, exportando via OTLP (permite plugar Jaeger/Grafana Tempo depois sem mudar código); os spans propagam automaticamente entre `Contratacao -> Proposta` (REST) e entre publisher/consumer (MassTransit já instrumenta isso). Cada serviço expõe `/health/live` (processo no ar) e `/health/ready` (banco e broker acessíveis).

### 10. Versionamento e contrato de erro
`Asp.Versioning` com versionamento por segmento de URL (`/api/v1/...`). Middleware de exceção único mapeia exceções de domínio/aplicação para `ProblemDetails` (RFC 7807), padronizado nos três serviços via um pacote/convenção compartilhada em `BuildingBlocks.Contracts` (apenas tipos de payload e constantes de erro — não lógica de negócio, para não acoplar os bounded contexts).

### 11. Provedor de IA: Anthropic API (Claude), chamada HTTP direta
`Analise.Infrastructure` implementa `IRiskAssessmentPort` com uma chamada HTTP direta à Anthropic Messages API (sem SDK pesado), enviando os dados da proposta em um prompt estruturado e parseando uma resposta em formato controlado (score numérico + recomendação + justificativa). Alternativa considerada: treinar um modelo de scoring próprio — descartado por estar fora do escopo e do prazo deste change; um LLM com prompt bem definido é suficiente para uma recomendação consultiva.

### 12. Testes
- **Unitários**: `Domain` e `Application` de cada serviço, sem infraestrutura real (xUnit + FluentAssertions/NSubstitute para os ports).
- **Integração**: `<Servico>.IntegrationTests` sobem Postgres e RabbitMQ reais via Testcontainers, testam o serviço de ponta a ponta via `WebApplicationFactory`. Para `Contratacao.Api`, o teste de integração usa um `HttpClient` apontando para um `Proposta.Api` real também subido via Testcontainers (ou um stub HTTP controlado) para validar a integração REST síncrona sem depender de mocks internos.

### 13. Value objects de domínio (reforço DDD)
As entidades `Proposta` e `Contratacao` não usam tipos primitivos soltos para conceitos com regras próprias — esses conceitos são modelados como value objects imutáveis dentro de `Proposta.Domain` e `Contratacao.Domain` (ou em um `SharedKernel.Domain` mínimo se os três serviços precisarem do mesmo VO, avaliado na implementação para não acoplar bounded contexts desnecessariamente):

- **`DocumentoIdentificacao` (CPF/CNPJ)** — usado em `Proposta.NomeSegurado`/documento. Imutável; normaliza a string removendo máscara; valida 11 dígitos (CPF) ou 14 dígitos (CNPJ) e os dígitos verificadores oficiais no construtor; lança erro de domínio na criação se inválido — nunca existe uma instância inválida.
- **`Monetario` (Money)** — usado em `ValorCobertura`, `ValorPremio`. Composto por `Quantia` (decimal) + `Moeda` (ISO 4217, padrão `BRL`); construtor rejeita quantia negativa; operações aritméticas retornam nova instância (imutabilidade); soma entre moedas diferentes lança erro de domínio.
- **`Vigencia` (PeriodoTempo)** — usado em `Contratacao` (início/fim de vigência). Composto por `DataInicio` + `DataFim`; construtor exige `DataFim` estritamente posterior a `DataInicio`; quando a vigência não é informada na criação da contratação, `Contratacao.Application` aplica o padrão de 1 ano a partir da data de contratação antes de construir o VO.

Alternativa considerada: manter esses campos como tipos primitivos e validar apenas na camada de aplicação — descartada porque permite a existência de um objeto `Proposta` em memória com CPF ou valor inválido em algum ponto do fluxo, violando o princípio DDD de que um value object nunca é inválido depois de construído.

### 14. Enforcement de SOLID, Clean Code e Design Patterns
Sem verificação automatizada, essas práticas dependem só de revisão manual e tendem a degradar com o tempo. Este design adota:

- **Architecture tests** (`NetArchTest.Rules` ou `ArchUnitNET`, um projeto de teste dedicado por solução): regras automatizadas que falham o build se `Domain` referenciar `Infrastructure`/`Api`, se `Application` referenciar diretamente um pacote de infraestrutura (EF Core, MassTransit, `HttpClient`) em vez de uma porta, ou se uma dependência circular for introduzida entre bounded contexts. Isso torna o Hexagonal e o DIP (SOLID) verificáveis em CI, não apenas uma convenção.
- **Analyzers de Clean Code**: analisadores nativos do .NET (`EnableNetAnalyzers`) mais `StyleCop.Analyzers` habilitados como erro de build (`TreatWarningsAsErrors`), cobrindo nomenclatura, complexidade ciclomática e tamanho de método.
- **Catálogo de Design Patterns usados** (nomeados explicitamente para rastreabilidade): **Repository** (`IPropostaRepository`, `IContratacaoRepository`, `IAnaliseRepository`), **Adapter/Ports & Adapters** (toda a `Infrastructure` implementando portas de `Application`), **Outbox/Inbox** (entrega confiável de eventos), **Circuit Breaker + Retry** (Polly, resiliência), **Strategy** (`IRiskAssessmentPort` permite trocar o provedor de IA sem alterar `Application`), **Factory estático** (métodos `Criar`/`FromExisting` nos value objects e agregados, garantindo que invariantes sejam checadas na construção).

## Risks / Trade-offs

- **[Risco] Token forwarding entre serviços acopla a validade da chamada interna ao token do usuário final** (se expirar no meio do fluxo, a chamada interna falha) → Mitigação: TTL de token generoso o suficiente para o fluxo síncrono (chamada única, resposta em milissegundos); revisitar com client-credentials se surgir um fluxo assíncrono que precise de token de serviço.
- **[Risco] Dependência de um provedor de IA externo introduz latência e custo variável** → Mitigação: análise é assíncrona (não bloqueia a criação da proposta) e tem circuit breaker; falha do provedor não impede o fluxo de negócio principal.
- **[Risco] Outbox/inbox adicionam uma tabela e um processo de despacho extra por serviço, aumentando a complexidade operacional** → Mitigação: é o padrão suportado nativamente pelo MassTransit + EF Core, não exige infraestrutura própria além do que já será usado.
- **[Risco] Três bancos + broker no docker-compose aumentam o tempo de subida do ambiente local** → Mitigação: aceitável para o escopo deste change; pode ser otimizado depois com perfis do compose.

## Migration Plan

Projeto novo — não há dado ou sistema legado a migrar. Ordem de implementação recomendada (detalhada em `tasks.md`): `Proposta.Api` primeiro (não depende de nada), depois `Contratacao.Api` (depende do endpoint REST de `Proposta.Api`), depois `Analise.Api` (depende do evento publicado por `Proposta.Api`), e por fim o `docker-compose.yml` integrando os três. Rollback é trivial nesta fase (nenhum dado em produção): remover os serviços do compose.
