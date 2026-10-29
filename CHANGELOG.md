# Changelog

As mudanças da API que importam para quem a consome (o front, um app, um teste manual). O formato segue o [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/), e a política de versionamento está em [docs/dev/versionamento.md](docs/dev/versionamento.md).

Enquanto só existir a `v1`, as entradas ficam em **Não lançado**. Uma mudança incompatível não entra aqui como uma linha a mais: ela exige a `v2`.

## [Não lançado]

### Adicionado

- `GET /health/live` (o processo responde, sem o banco) e `GET /health/ready` (a API consegue atender, com o banco). O `GET /health` continua, igual ao `ready`.
- `GET /api/v1/admin/foods`: a busca de alimentos do administrador, incluindo os desativados (`isActive: false`).
- `DELETE /api/v1/admin/nutritionists/{id}`: cancela um pré-cadastro cujo convite nunca foi aceito, liberando o CPF, o e-mail e o CRN.
- O documento OpenAPI descreve, em português, cada endpoint e todas as respostas possíveis, com os tipos (`/openapi/v1.json` e a tela do Swagger no staging).

### Alterado

- A busca de alimentos ordena por relevância: primeiro os nomes que começam com a primeira palavra buscada, depois os da TACO, depois os de nome mais curto, e só então a ordem alfabética. Antes, era só alfabética.
- A busca de alimentos ficou mais rápida no banco (um índice de trigramas: de cerca de 23 ms para 1,5 ms por consulta). O formato da resposta não mudou.
- Desativar um nutricionista encerra os acompanhamentos dele (os pacientes recebem um e-mail), finaliza ou cancela os planos em aberto e recusa as solicitações pendentes.
- Um paciente desativado sai da lista do nutricionista, e o detalhe, os planos e o peso dele respondem `404` enquanto durar o bloqueio.
- O bloqueio pelo administrador é explícito: confirmar o e-mail, redefinir a senha ou aceitar um convite não reativa uma conta bloqueada.
- Uma requisição repetida ao mesmo tempo (o clique duplo) que esbarra num dado único responde `409`, com uma mensagem por caso, e não mais `500`.
- Uma regra de negócio recusada fora das checagens dos casos de uso responde `422`, com a mensagem do domínio.
- Um erro inesperado responde `500` com uma mensagem genérica e o `traceId`, sem nenhum detalhe interno.
- Toda resposta traz cabeçalhos de segurança (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, e nas rotas `/api` também `Content-Security-Policy` e `Cache-Control: no-store`), e não traz mais o `Server`.
- `GET /api/v1/auth/me` passou a ter um tipo documentado (`MeResponse`). O JSON não mudou: `{ "userId": "...", "roles": [...] }`.
- As respostas `{ "id": ... }`, `{ "message": ... }` e `{ "invitationEmailSent": ... }` passaram a ter tipos documentados. O JSON não mudou.
- Staging: o front passou a ser `https://nogvita.vercel.app` (Angular). É a única origem liberada no CORS (sai o `nogvita-green.vercel.app` e o `localhost:5173`), e os links dos e-mails (confirmação, redefinição de senha e convite) apontam para ele.
- O CORS aceita só os métodos `GET`, `POST`, `PUT` e `DELETE` e os cabeçalhos `Authorization` e `Content-Type`.
- Um texto com o caractere nulo (`\u0000`), no corpo, na query ou na rota, responde `400` com o erro no campo ("O texto contém um caractere inválido."), e não mais `500`.
- Os erros de validação da quantidade de um alimento na refeição, da meta diária e do `PUT` do plano vêm em português (antes, vinham com a mensagem padrão, em inglês).

### Segurança

- Fora do desenvolvimento, a API manda o HSTS (30 dias) e redireciona o HTTP puro para o HTTPS com `308`, que mantém o método.
- O login responde no mesmo tempo para um e-mail cadastrado ou não, e não revela quem tem conta.
- A API só aceita requisições para os próprios endereços (`AllowedHosts`); outro `Host` recebe `400`.
