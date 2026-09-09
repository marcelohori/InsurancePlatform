## Purpose

Apoia o analista na decisão de aprovar ou rejeitar uma proposta, gerando automaticamente uma recomendação de risco baseada em IA a partir dos dados da proposta, sem substituir a decisão humana final.

## ADDED Requirements

### Requirement: Análise automática ao criar proposta
O sistema SHALL iniciar automaticamente uma análise de risco ao ser notificado da criação de uma nova proposta, sem exigir ação manual do analista para disparar a análise.

#### Scenario: Nova proposta notificada
- **WHEN** o sistema é notificado de que uma nova proposta foi criada
- **THEN** o sistema inicia o processamento da análise de risco para essa proposta

#### Scenario: Notificação duplicada da mesma proposta
- **WHEN** o sistema recebe mais de uma notificação referente à mesma proposta
- **THEN** o sistema processa a análise apenas uma vez para essa proposta

### Requirement: Geração de recomendação por IA
O sistema SHALL gerar, para cada proposta analisada, um score de risco, uma recomendação (`Aprovar` ou `Rejeitar`) e uma justificativa em linguagem natural, com base nos dados da proposta.

#### Scenario: Análise concluída com sucesso
- **WHEN** a análise de uma proposta é concluída com sucesso
- **THEN** o sistema armazena o score de risco, a recomendação e a justificativa associados à proposta

### Requirement: Caráter consultivo da recomendação
O sistema SHALL tratar a recomendação gerada como consultiva. O sistema NÃO SHALL alterar o status da proposta automaticamente com base na recomendação.

#### Scenario: Recomendação não altera status da proposta
- **WHEN** uma análise é concluída com a recomendação `Rejeitar`
- **THEN** o status da proposta permanece inalterado até que um analista humano tome a decisão

### Requirement: Consulta da análise por proposta
O sistema SHALL permitir consultar a análise de risco associada a uma proposta pelo identificador da proposta.

#### Scenario: Análise já concluída
- **WHEN** é consultada a análise de uma proposta cuja análise já foi concluída
- **THEN** o sistema retorna o score de risco, a recomendação e a justificativa

#### Scenario: Análise ainda em processamento
- **WHEN** é consultada a análise de uma proposta cuja análise ainda está em processamento
- **THEN** o sistema retorna o status `EmProcessamento`, sem score nem recomendação

#### Scenario: Proposta sem análise iniciada
- **WHEN** é consultada a análise de uma proposta para a qual nenhuma análise foi registrada
- **THEN** o sistema retorna erro 404 no formato Problem Details

### Requirement: Resiliência na dependência de IA externa
O sistema SHALL aplicar tentativas de repetição, tempo limite e interrupção de circuito (circuit breaker) nas chamadas ao provedor de IA externo. Quando o provedor estiver indisponível após as tentativas configuradas, o sistema SHALL marcar a análise como `Falha` em vez de deixar a solicitação pendente indefinidamente.

#### Scenario: Indisponibilidade do provedor de IA
- **WHEN** o provedor de IA externo está indisponível após as tentativas de repetição configuradas
- **THEN** o sistema registra a análise da proposta com status `Falha` e permite reprocessamento posterior

#### Scenario: Falha não bloqueia o fluxo de proposta
- **WHEN** a análise de uma proposta falha
- **THEN** a proposta continua disponível para decisão manual do analista sem a recomendação de IA

### Requirement: Autenticação e autorização
O sistema SHALL exigir um token JWT válido para consultar análises de risco.

#### Scenario: Requisição sem autenticação
- **WHEN** uma requisição de consulta de análise é feita sem um token JWT válido
- **THEN** o sistema rejeita a requisição com erro 401

### Requirement: Contrato de erro e versionamento padronizados
O sistema SHALL expor sua API sob um caminho versionado e SHALL retornar erros no formato Problem Details (RFC 7807) de forma consistente em todos os endpoints.

#### Scenario: Erro de validação padronizado
- **WHEN** qualquer operação falha por dado inválido ou recurso não encontrado
- **THEN** o corpo da resposta de erro segue o formato Problem Details, incluindo tipo, título e detalhe do problema
