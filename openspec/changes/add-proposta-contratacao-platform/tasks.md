## 1. Solução e blocos compartilhados

- [x] 1.1 Criar a solução .NET 10 (`InsurancePlatformV01.sln`) e a estrutura de pastas `src/Proposta`, `src/Contratacao`, `src/Analise`, `src/BuildingBlocks`, `tests/`; verificar que `dotnet sln list` mostra a solução vazia
- [x] 1.2 Criar o projeto `BuildingBlocks.Contracts` com os payloads de evento (`PropostaCriadaEvent`, `ContratacaoEfetuadaEvent`) e os tipos de erro Problem Details compartilhados; verificar que o projeto compila (`dotnet build`)
- [x] 1.3 Configurar convenções comuns (Directory.Build.props: nullable enable, warnings as errors, versão de linguagem, `EnableNetAnalyzers` + `StyleCop.Analyzers`) na raiz da solução; verificar que se aplicam a um projeto de teste criado localmente. Nota: StyleCop fica em severidade `suggestion` (via `.editorconfig`) para não bloquear o build em regras puramente estilísticas (cabeçalho de arquivo, linhas em branco); analisadores .NET nativos continuam como erro
- [x] 1.4 Criar projeto `Architecture.Tests` com regras `NetArchTest`/`ArchUnitNET` garantindo que `*.Domain` não referencia `*.Infrastructure`/`*.Api` e que `*.Application` não referencia pacotes de infraestrutura diretamente; verificar que o projeto falha propositalmente ao violar a regra em um teste de exemplo e passa depois de corrigido

## 2. Proposta.Api

- [x] 2.1 Criar `Proposta.Domain`: entidade `Proposta`, value objects `DocumentoIdentificacao` (CPF/CNPJ com dígito verificador) e `Monetario` (Quantia+Moeda, imutável, não-negativo), enum `StatusProposta` (EmAnalise/Aprovada/Rejeitada), enum `TipoSeguro`, e regras de transição de status (Aprovada/Rejeitada são finais); verificar com testes unitários de domínio cobrindo transições válidas/inválidas e construção inválida dos VOs (documento malformado, valor negativo)
- [x] 2.2 Criar `Proposta.Application`: casos de uso Criar/Listar/ObterPorId/Atualizar/Deletar e portas `IPropostaRepository`, `IEventPublisher`; verificar com testes unitários usando fakes/mocks das portas, cobrindo os cenários da spec `proposta` (validação obrigatória, transição final, bloqueio de exclusão de proposta aprovada)
- [x] 2.3 Criar `Proposta.Infrastructure`: adapter EF Core + Postgres (`proposta_db`) implementando `IPropostaRepository`, com outbox transacional (MassTransit `AddEntityFrameworkOutbox`) implementando `IEventPublisher`; verificar com migração aplicada localmente e teste de integração gravando/lendo uma proposta
- [x] 2.4 Criar `Proposta.Api`: controllers REST versionados (`/api/v1/propostas`), autenticação JWT Bearer com papel `Analista` exigido na transição de status, middleware de Problem Details, health checks (`/health/live`, `/health/ready`), Serilog e OpenTelemetry; verificar chamando os 5 endpoints via `dotnet run` + requisições manuais (curl/HTTP file)
- [x] 2.5 Escrever testes de integração de `Proposta.Api` com Testcontainers (Postgres + RabbitMQ) cobrindo criar/listar/obter/atualizar/deletar e a publicação do evento `PropostaCriadaEvent`; verificar que a suíte passa via `dotnet test`

## 3. Contratacao.Api

- [x] 3.1 Criar `Contratacao.Domain`: entidade `Contratacao` (Id, PropostaId, NumeroApolice, DataContratacao, Status Ativa/Cancelada), value objects `Vigencia` (DataInicio+DataFim, exige fim > início, com fábrica de vigência padrão de 1 ano) e `Monetario` (ValorPremio); verificar com testes unitários de domínio (geração de apólice, transição Ativa->Cancelada, construção inválida de `Vigencia` com fim <= início, vigência padrão aplicada)
- [x] 3.2 Criar `Contratacao.Application`: casos de uso Criar/Listar/ObterPorId/Atualizar/Deletar e portas `IContratacaoRepository`, `IPropostaVerificationPort`, `IEventPublisher`, `IIdempotencyStore`; verificar com testes unitários cobrindo criação bloqueada por proposta não aprovada/inexistente e repetição por chave de idempotência
- [x] 3.3 Criar `Contratacao.Infrastructure`: adapter EF Core + Postgres (`contratacao_db`), adapter HTTP para `Proposta.Api` implementando `IPropostaVerificationPort` com Polly (retry + timeout + circuit breaker), outbox para `ContratacaoEfetuadaEvent`; verificar com teste de integração simulando indisponibilidade do serviço de propostas (circuito abre e retorna erro em vez de travar)
- [x] 3.4 Criar `Contratacao.Api`: controllers REST versionados (`/api/v1/contratacoes`), suporte ao header `Idempotency-Key`, autenticação JWT Bearer, middleware de Problem Details, health checks, Serilog e OpenTelemetry propagando trace da chamada a `Proposta.Api`; verificar chamando os 5 endpoints via `dotnet run` + requisições manuais
- [x] 3.5 Escrever testes de integração de `Contratacao.Api` com Testcontainers (Postgres + RabbitMQ) e uma instância real (ou stub HTTP controlado) de `Proposta.Api`, cobrindo o fluxo completo de contratação (aprovada, rejeitada, inexistente, repetição idempotente); verificar que a suíte passa via `dotnet test`

## 4. Analise.Api (assistente de IA)

- [x] 4.1 Criar `Analise.Domain`: entidade `AnaliseRisco` (Id, PropostaId, ScoreRisco, Recomendacao, Justificativa, Status EmProcessamento/Concluida/Falha); verificar com testes unitários de domínio das transições de status
- [x] 4.2 Criar `Analise.Application`: caso de uso de processar análise a partir do evento consumido e caso de uso de consulta por proposta, porta `IRiskAssessmentPort`, `IAnaliseRepository`; verificar com testes unitários usando um fake de `IRiskAssessmentPort` (sucesso e falha)
- [x] 4.3 Criar `Analise.Infrastructure`: adapter EF Core + Postgres (`analise_db`), consumer MassTransit do `PropostaCriadaEvent` com inbox (deduplicação por MessageId), adapter HTTP para a Anthropic Messages API implementando `IRiskAssessmentPort` com Polly (retry + timeout + circuit breaker); verificar com teste de integração consumindo um evento de teste e persistindo o resultado
- [x] 4.4 Criar `Analise.Api`: controller REST versionado (`GET /api/v1/propostas/{id}/analise`), autenticação JWT Bearer, middleware de Problem Details, health checks, Serilog e OpenTelemetry; verificar consultando uma análise já processada e uma inexistente (404)
- [x] 4.5 Escrever testes de integração de `Analise.Api` com Testcontainers (Postgres + RabbitMQ) cobrindo: evento duplicado processado uma única vez, falha do provedor de IA marcando status `Falha` sem travar a fila; verificar que a suíte passa via `dotnet test`

## 5. Empacotamento e orquestração

- [x] 5.1 Criar `Dockerfile` multi-stage para cada um dos três serviços; verificar que `docker build` produz uma imagem funcional para cada um
- [x] 5.2 Criar `docker-compose.yml` com Postgres (3 databases), RabbitMQ, e os três serviços com variáveis de ambiente (connection strings, chave da Anthropic API, chave de assinatura JWT); verificar que `docker compose up` sobe o ambiente e os três `/health/ready` respondem OK
- [x] 5.3 Documentar no README como rodar o ambiente local (compose) e executar a suíte de testes; verificar seguindo o próprio README do zero

## 6. Verificação de ponta a ponta

- [x] 6.1 Executar manualmente o fluxo completo (criar proposta -> aprovar -> IA gera análise -> criar contratação -> consultar contratação) contra o ambiente docker-compose; verificar que cada etapa retorna o status esperado e que o evento de contratação é publicado
- [x] 6.2 Executar o fluxo de rejeição (criar contratação para proposta não aprovada) e o fluxo de indisponibilidade (derrubar `Proposta.Api` e tentar contratar) contra o ambiente docker-compose; verificar que os erros retornados seguem o contrato Problem Details definido nas specs
