## Purpose

Gerencia o ciclo de vida de uma proposta de seguro, desde a criação pelo cliente até a decisão de aprovação ou rejeição por um analista, servindo de fonte de verdade para os demais serviços da plataforma.

## ADDED Requirements

### Requirement: Criar proposta
O sistema SHALL permitir a criação de uma proposta de seguro com nome do segurado, documento do segurado, tipo de seguro, valor de cobertura e valor de prêmio. Toda proposta criada SHALL iniciar com status `EmAnalise`. O documento do segurado SHALL ser um CPF ou CNPJ com dígitos verificadores válidos, e os valores de cobertura e de prêmio SHALL ser não-negativos.

#### Scenario: Criação bem-sucedida
- **WHEN** um cliente envia os dados obrigatórios de uma proposta, com documento válido e valores não-negativos
- **THEN** o sistema cria a proposta com status `EmAnalise` e retorna seu identificador

#### Scenario: Dados obrigatórios ausentes
- **WHEN** a requisição de criação não informa um campo obrigatório (ex.: tipo de seguro ou valor de cobertura)
- **THEN** o sistema rejeita a criação com um erro de validação (Problem Details) e não cria a proposta

#### Scenario: Documento do segurado inválido
- **WHEN** o documento informado não corresponde a um CPF ou CNPJ com dígitos verificadores válidos
- **THEN** o sistema rejeita a criação com um erro de validação (Problem Details) e não cria a proposta

#### Scenario: Valor monetário negativo
- **WHEN** o valor de cobertura ou o valor de prêmio informado é negativo
- **THEN** o sistema rejeita a criação com um erro de validação (Problem Details) e não cria a proposta

### Requirement: Listar propostas
O sistema SHALL permitir listar as propostas cadastradas, com suporte a paginação.

#### Scenario: Listagem com resultados
- **WHEN** existem propostas cadastradas
- **THEN** o sistema retorna a página solicitada de propostas com seus dados e status atuais

#### Scenario: Listagem sem resultados
- **WHEN** não existe nenhuma proposta cadastrada
- **THEN** o sistema retorna uma lista vazia

### Requirement: Obter proposta por id
O sistema SHALL permitir consultar uma proposta específica pelo seu identificador, incluindo seu status atual.

#### Scenario: Proposta existente
- **WHEN** é consultado o identificador de uma proposta existente
- **THEN** o sistema retorna os dados completos e o status da proposta

#### Scenario: Proposta inexistente
- **WHEN** é consultado um identificador que não corresponde a nenhuma proposta
- **THEN** o sistema retorna erro 404 no formato Problem Details

### Requirement: Atualizar proposta
O sistema SHALL permitir atualizar os dados de uma proposta e transicionar seu status de `EmAnalise` para `Aprovada` ou `Rejeitada`. Uma vez em status `Aprovada` ou `Rejeitada`, o status SHALL ser considerado final e não SHALL ser alterado novamente.

#### Scenario: Aprovação de proposta em análise
- **WHEN** uma proposta com status `EmAnalise` é atualizada para status `Aprovada`
- **THEN** o sistema persiste a mudança e a proposta passa a poder ser referenciada em uma contratação

#### Scenario: Rejeição de proposta em análise
- **WHEN** uma proposta com status `EmAnalise` é atualizada para status `Rejeitada`
- **THEN** o sistema persiste a mudança e a proposta não pode mais ser referenciada em uma contratação

#### Scenario: Tentativa de alterar status final
- **WHEN** é solicitada uma mudança de status para uma proposta já `Aprovada` ou `Rejeitada`
- **THEN** o sistema rejeita a atualização com um erro de conflito (Problem Details)

### Requirement: Deletar proposta
O sistema SHALL permitir excluir uma proposta que ainda esteja com status `EmAnalise` ou `Rejeitada`. O sistema SHALL impedir a exclusão de uma proposta com status `Aprovada`.

#### Scenario: Exclusão permitida
- **WHEN** é solicitada a exclusão de uma proposta com status `EmAnalise` ou `Rejeitada`
- **THEN** o sistema remove a proposta e ela deixa de aparecer em listagens e consultas

#### Scenario: Exclusão bloqueada por proposta aprovada
- **WHEN** é solicitada a exclusão de uma proposta com status `Aprovada`
- **THEN** o sistema rejeita a exclusão com um erro de conflito (Problem Details)

### Requirement: Publicação confiável do evento de proposta criada
Sempre que uma proposta for criada, o sistema SHALL publicar um evento `PropostaCriadaEvent` de forma confiável, garantindo que o evento não seja perdido mesmo em caso de indisponibilidade momentânea do broker de mensagens.

#### Scenario: Publicação após criação
- **WHEN** uma proposta é criada com sucesso
- **THEN** o sistema publica `PropostaCriadaEvent` com o identificador e os dados relevantes da proposta

#### Scenario: Broker indisponível no momento da criação
- **WHEN** uma proposta é criada com sucesso mas o broker de mensagens está temporariamente indisponível
- **THEN** o sistema garante que a criação da proposta não é perdida nem revertida, e o evento é publicado assim que o broker voltar a ficar disponível

### Requirement: Autenticação e autorização
O sistema SHALL exigir um token JWT válido para todas as operações. A transição de status (aprovar/rejeitar) SHALL exigir o papel `Analista`.

#### Scenario: Requisição sem autenticação
- **WHEN** uma requisição é feita sem um token JWT válido
- **THEN** o sistema rejeita a requisição com erro 401

#### Scenario: Atualização de status sem papel de analista
- **WHEN** um usuário sem o papel `Analista` tenta aprovar ou rejeitar uma proposta
- **THEN** o sistema rejeita a requisição com erro 403

### Requirement: Contrato de erro e versionamento padronizados
O sistema SHALL expor sua API sob um caminho versionado e SHALL retornar erros no formato Problem Details (RFC 7807) de forma consistente em todos os endpoints.

#### Scenario: Erro de validação padronizado
- **WHEN** qualquer operação falha por dado inválido, conflito ou recurso não encontrado
- **THEN** o corpo da resposta de erro segue o formato Problem Details, incluindo tipo, título e detalhe do problema
