# Política de versionamento da API

## Versão na URL

A versão principal faz parte do caminho de todas as rotas de negócio: `/api/v1/...`. O número identifica o contrato público da API, não a versão do build ou do pacote. A API não usa versão por query string, header ou negociação de conteúdo, e não depende de um pacote de versionamento. O endpoint operacional `GET /health` fica fora do prefixo.

Hoje, a única versão publicada é a `v1`.

## Mudanças compatíveis e incompatíveis

Mudanças que preservam o contrato permanecem na versão atual. Exemplos: adicionar uma rota ou aceitar um campo opcional novo na requisição. Os clientes devem ignorar campos desconhecidos nas respostas; novos valores de enumeração também precisam ser tratados com tolerância pelo cliente.

Uma mudança incompatível exige uma nova versão principal no caminho, por exemplo, `/api/v2/...`. São incompatíveis, entre outras: remover ou renomear uma rota ou campo, mudar o tipo ou o significado de um campo, tornar obrigatório um campo antes opcional, remover um valor de enumeração aceito ou alterar de forma incompatível a autenticação e os códigos de resposta.

Não se incrementa o caminho por correções internas ou mudanças que preservem o contrato. A descrição `info.version` do OpenAPI acompanha a versão principal documentada.

## Convivência e descontinuação

Quando uma nova versão for criada, a anterior deve continuar disponível durante a migração dos consumidores. Uma versão não é removida automaticamente: a descontinuação exige comunicação explícita, prazo de migração e atualização deste documento, do guia de consumo e do README. Não há prazo de descontinuação definido para a `v1`.

## Documentação do contrato

O documento OpenAPI e a tela Swagger são gerados pela API. Cada action deve ter um comentário XML em português e declarar com `[ProducesResponseType]` todas as respostas de sucesso e de erro específicas; respostas realmente comuns ficam no nível do controller. Os testes de integração conferem o documento gerado. O guia de consumo está em [`../API.md`](../API.md), e a tela interativa do ambiente de staging está em `https://nogvitaapi.onrender.com/swagger`.

## Registro de mudanças

Toda mudança que afete quem consome a API entra no [`CHANGELOG.md`](../../CHANGELOG.md), na seção **Não lançado**. Uma mudança incompatível não entra só como uma linha a mais: ela exige a próxima versão principal.
