## Purpose

Formaliza a contratação de um seguro a partir de uma proposta aprovada, garantindo que apenas propostas válidas gerem apólices e que a criação seja segura contra duplicidade em caso de repetição de requisição.

## ADDED Requirements

### Requirement: Criar contratação
O sistema SHALL permitir criar uma contratação a partir do identificador de uma proposta, gerando um número de apólice, data de contratação, vigência (início e fim) e valor de prêmio. O sistema SHALL criar a contratação apenas se a proposta associada existir e estiver com status `Aprovada`. A data de fim de vigência SHALL ser estritamente posterior à data de início. Quando a vigência não for informada na criação, o sistema SHALL aplicar automaticamente uma vigência padrão de 1 ano a partir da data de contratação.

#### Scenario: Contratação de proposta aprovada
- **WHEN** é solicitada a criação de uma contratação referenciando uma proposta com status `Aprovada`
- **THEN** o sistema cria a contratação com status `Ativa`, gera um número de apólice e retorna seu identificador

#### Scenario: Vigência com data de fim anterior à data de início
- **WHEN** é solicitada a criação de uma contratação informando uma data de fim de vigência igual ou anterior à data de início
- **THEN** o sistema rejeita a criação com um erro de validação (Problem Details) e nenhuma contratação é criada

#### Scenario: Vigência padrão aplicada
- **WHEN** é solicitada a criação de uma contratação sem informar as datas de vigência
- **THEN** o sistema cria a contratação com vigência de 1 ano a partir da data de contratação

#### Scenario: Contratação de proposta não aprovada
- **WHEN** é solicitada a criação de uma contratação referenciando uma proposta com status `EmAnalise` ou `Rejeitada`
- **THEN** o sistema rejeita a criação com um erro de negócio (Problem Details) e nenhuma contratação é criada

#### Scenario: Contratação de proposta inexistente
- **WHEN** é solicitada a criação de uma contratação referenciando um identificador de proposta que não existe
- **THEN** o sistema rejeita a criação com erro 404/422 (Problem Details) e nenhuma contratação é criada

### Requirement: Verificação síncrona do status da proposta
Ao criar uma contratação, o sistema SHALL verificar o status da proposta associada em tempo real, consultando o serviço de propostas via chamada HTTP síncrona antes de confirmar a contratação.

#### Scenario: Verificação bem-sucedida
- **WHEN** a proposta informada existe e está com status `Aprovada` no momento da verificação
- **THEN** o sistema prossegue com a criação da contratação

#### Scenario: Serviço de propostas indisponível
- **WHEN** o serviço de propostas está indisponível ou não responde dentro do tempo limite configurado
- **THEN** o sistema rejeita a criação da contratação com um erro de indisponibilidade (Problem Details, 503/502), sem criar a contratação e sem repetir chamadas indefinidamente

### Requirement: Idempotência na criação de contratação
O sistema SHALL aceitar uma chave de idempotência na criação de contratação. Requisições repetidas com a mesma chave de idempotência SHALL retornar o mesmo resultado da primeira requisição, sem criar uma segunda contratação.

#### Scenario: Repetição com a mesma chave de idempotência
- **WHEN** uma requisição de criação de contratação é repetida com a mesma chave de idempotência de uma requisição já processada com sucesso
- **THEN** o sistema retorna a contratação originalmente criada, sem gerar uma nova apólice

#### Scenario: Requisições com chaves diferentes
- **WHEN** duas requisições de criação de contratação são enviadas para a mesma proposta com chaves de idempotência diferentes
- **THEN** o sistema trata cada uma como uma solicitação distinta

### Requirement: Listar contratações
O sistema SHALL permitir listar as contratações cadastradas, com suporte a paginação.

#### Scenario: Listagem com resultados
- **WHEN** existem contratações cadastradas
- **THEN** o sistema retorna a página solicitada de contratações com seus dados e status atuais

### Requirement: Obter contratação por id
O sistema SHALL permitir consultar uma contratação específica pelo seu identificador.

#### Scenario: Contratação existente
- **WHEN** é consultado o identificador de uma contratação existente
- **THEN** o sistema retorna os dados completos da contratação, incluindo referência à proposta de origem

#### Scenario: Contratação inexistente
- **WHEN** é consultado um identificador que não corresponde a nenhuma contratação
- **THEN** o sistema retorna erro 404 no formato Problem Details

### Requirement: Atualizar contratação
O sistema SHALL permitir atualizar uma contratação existente, incluindo a transição de status de `Ativa` para `Cancelada`.

#### Scenario: Cancelamento de contratação ativa
- **WHEN** uma contratação com status `Ativa` é atualizada para status `Cancelada`
- **THEN** o sistema persiste a mudança de status

### Requirement: Deletar contratação
O sistema SHALL permitir excluir uma contratação existente.

#### Scenario: Exclusão de contratação
- **WHEN** é solicitada a exclusão de uma contratação existente
- **THEN** o sistema remove a contratação e ela deixa de aparecer em listagens e consultas

### Requirement: Publicação confiável do evento de contratação efetuada
Sempre que uma contratação for criada com sucesso, o sistema SHALL publicar um evento `ContratacaoEfetuadaEvent` de forma confiável, sem perda em caso de indisponibilidade momentânea do broker de mensagens.

#### Scenario: Publicação após contratação
- **WHEN** uma contratação é criada com sucesso
- **THEN** o sistema publica `ContratacaoEfetuadaEvent` com o identificador da contratação e da proposta de origem

### Requirement: Autenticação e autorização
O sistema SHALL exigir um token JWT válido para todas as operações.

#### Scenario: Requisição sem autenticação
- **WHEN** uma requisição é feita sem um token JWT válido
- **THEN** o sistema rejeita a requisição com erro 401

### Requirement: Contrato de erro e versionamento padronizados
O sistema SHALL expor sua API sob um caminho versionado e SHALL retornar erros no formato Problem Details (RFC 7807) de forma consistente em todos os endpoints.

#### Scenario: Erro de validação padronizado
- **WHEN** qualquer operação falha por dado inválido, conflito ou recurso não encontrado
- **THEN** o corpo da resposta de erro segue o formato Problem Details, incluindo tipo, título e detalhe do problema
