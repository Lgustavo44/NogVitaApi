# Guia de consumo da API NogVita

Este guia é para quem vai **consumir** a API (frontend, apps, testes manuais). Ele descreve o contrato: URLs, autenticação, formatos de requisição e resposta, erros e os fluxos principais.

> O Swagger continua sendo a referência interativa: dá para testar cada endpoint direto pelo navegador.

## Sumário

- [Ambientes](#ambientes)
- [Convenções gerais](#convenções-gerais)
- [Autenticação](#autenticação)
- [Erros](#erros)
- [Endpoints](#endpoints)
  - [Auth](#auth)
  - [Confirmação de e-mail](#confirmação-de-e-mail)
  - [Paciente](#paciente)
  - [Convite de nutricionista](#convite-de-nutricionista)
  - [Administração](#administração)
- [Fluxos](#fluxos)
- [Checklist do frontend](#checklist-do-frontend)

## Ambientes

| Ambiente | API | Swagger |
|---|---|---|
| Staging | `https://nogvitaapi.onrender.com` | `https://nogvitaapi.onrender.com/swagger` |
| Local | `https://localhost:7194` | `https://localhost:7194/swagger` |

**Sobre o staging:**

- Roda no plano gratuito da Render: depois de 15 minutos sem uso, o serviço "dorme", e a primeira requisição pode levar **cerca de 1 minuto**. Mostre um estado de carregamento e não use timeouts curtos.
- Use **apenas dados de teste**. Para CPFs, use um gerador de CPF válido.
- `GET /health` responde `Healthy` quando a API e o banco estão no ar. É útil para "acordar" o serviço.

**CORS:** a única origem liberada é `http://localhost:5173` (o padrão do Vite). Acesse sempre por `localhost`, e não por `127.0.0.1`, que o navegador trata como outra origem.

## Convenções gerais

- **Prefixo:** todas as rotas de negócio ficam em `/api/v1`.
- **JSON:** os campos usam `camelCase` (`accessToken`, `birthDate`). Envie `Content-Type: application/json`.
- **Enums:** trafegam como **texto**, e não como número.

  | Enum | Valores |
  |---|---|
  | `biologicalSex` | `Male`, `Female` |
  | `goal` | `WeightLoss`, `Maintenance`, `MuscleGain` |
  | papéis (`roles`) | `Patient`, `Nutritionist`, `Admin` |

- **Datas:** datas sem hora (`birthDate`) usam `YYYY-MM-DD`. Datas com hora (`createdAt`, `accessTokenExpiresAtUtc`) estão em **UTC**, no formato ISO 8601. Converta para o fuso local só na exibição.
- **IDs:** são GUIDs (`"3fa85f64-5717-4562-b3fc-2c963f66afa6"`).
- **CPF:** pode ser enviado com ou sem máscara (`123.456.789-09` ou `12345678909`). Nas respostas, ele vem formatado.
- **Limite de tentativas:** as rotas `/api/v1/auth/*` aceitam **10 requisições por minuto por IP**. Acima disso, a resposta é `429 Too Many Requests`.

## Autenticação

A API usa **JWT** com dois tokens:

| Token | Duração | Para que serve |
|---|---|---|
| `accessToken` | 15 minutos | Vai em toda requisição protegida |
| `refreshToken` | 7 dias | Só serve para obter um par novo de tokens |

Login, cadastro e renovação devolvem o mesmo formato (`AuthResponse`):

```json
{
  "accessToken": "eyJhbGciOi...",
  "accessTokenExpiresAtUtc": "2026-10-08T15:30:00Z",
  "refreshToken": "q8Zx...",
  "refreshTokenExpiresAtUtc": "2026-10-15T15:15:00Z"
}
```

Envie o token de acesso no header:

```
Authorization: Bearer <accessToken>
```

**Regras importantes:**

1. **Rotação:** cada `POST /auth/refresh` devolve um par **novo**, e o refresh token usado deixa de valer. Sempre salve o novo `refreshToken`.
2. **Uma renovação por vez:** se o mesmo refresh token for usado duas vezes (por exemplo, duas requisições renovando ao mesmo tempo), a API entende isso como roubo de token e **encerra todas as sessões** do usuário. No frontend, garanta que só exista uma renovação em andamento e que as outras requisições esperem por ela (veja [Renovação automática](#renovação-automática-do-token)).
3. **Papéis combináveis:** um mesmo usuário pode ser `Patient`, `Nutritionist` e `Admin` ao mesmo tempo. Use `GET /auth/me` para saber quais papéis ele tem e decidir quais telas mostrar.
4. **Conta desativada:** quando um administrador desativa uma conta, todas as sessões dela são revogadas. A próxima renovação falha com `401`.

## Erros

As respostas de erro seguem o formato **ProblemDetails** (RFC 9457):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Não foi possível concluir o cadastro com esses dados.",
  "status": 409
}
```

Em **erros de validação** (`400`), vem também o objeto `errors`, com as mensagens por campo:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["O e-mail informado não é válido."],
    "Password": ["A senha precisa ter pelo menos 15 caracteres."]
  }
}
```

> As chaves de `errors` vêm em **PascalCase** (`Email`, `BirthDate`). Ao mapear para os campos do formulário, compare sem diferenciar maiúsculas de minúsculas.

As mensagens (`title` e `errors`) já estão em português e podem ser mostradas ao usuário.

| Status | Significado | O que fazer |
|---|---|---|
| `400` | Dados inválidos | Mostrar os erros por campo (`errors`) ou o `title` |
| `401` | Sem token, token expirado ou credenciais inválidas | Tentar renovar o token; se falhar, mandar para o login |
| `403` | Usuário autenticado, mas sem o papel necessário; **no login**, e-mail ainda não confirmado | Mostrar "sem permissão"; no login, oferecer o reenvio do link |
| `404` | Recurso não encontrado | Mostrar o `title` |
| `409` | Conflito (dados já em uso, regra de negócio) | Mostrar o `title` |
| `429` | Muitas tentativas | Pedir para o usuário esperar um minuto |

> `401` gerado pela falta ou expiração do token e `403` de falta de papel vêm **sem corpo**. Trate esses casos pelo status, e não pelo `title`. A exceção é o `403` do login (e-mail não confirmado), que vem com `title`.

## Endpoints

Legenda de acesso: 🌐 público · 🔒 qualquer usuário autenticado · 🧑 `Patient` · 🛡️ `Admin`

### Auth

#### `POST /api/v1/auth/login` 🌐

```json
{ "email": "maria@exemplo.com", "password": "uma senha bem longa aqui" }
```

- `200` → `AuthResponse`
- `401` → "E-mail ou senha inválidos." (a mesma mensagem para qualquer falha, de propósito, inclusive conta desativada)
- `403` → "Confirme seu e-mail antes de entrar." Só aparece quando a senha está **correta**: mostre a opção de reenviar o link (veja [Confirmação de e-mail](#confirmação-de-e-mail)).

#### `POST /api/v1/auth/register/patient` 🌐

Cria a conta e o perfil de paciente em uma etapa. A conta nasce **inativa**, e a API envia um link de confirmação para o e-mail informado. **Não devolve tokens**: o usuário só consegue entrar depois de confirmar o e-mail.

```json
{
  "name": "Maria Silva",
  "email": "maria@exemplo.com",
  "cpf": "123.456.789-09",
  "password": "uma senha bem longa aqui",
  "birthDate": "1995-04-20",
  "biologicalSex": "Female",
  "heightInCm": 165,
  "goal": "WeightLoss"
}
```

| Campo | Regras |
|---|---|
| `name` | Obrigatório, até 150 caracteres |
| `email` | Obrigatório, e-mail válido, até 254 caracteres |
| `cpf` | Obrigatório, com dígitos verificadores válidos |
| `password` | De 15 a 128 caracteres, sem regras de composição; senhas muito comuns são recusadas |
| `birthDate` | Não pode estar no futuro |
| `biologicalSex` | `Male` ou `Female` |
| `heightInCm` | Maior que zero |
| `goal` | `WeightLoss`, `Maintenance` ou `MuscleGain` |

- `202` → sempre a mesma resposta, mesmo que o e-mail ou o CPF já estejam cadastrados:

  ```json
  { "message": "Se os dados estiverem corretos, enviamos um e-mail para confirmar o cadastro." }
  ```

  Mostre uma tela de "verifique seu e-mail" (pode exibir o `message`), com a opção de reenviar o link.
- `400` → erros de validação

> A resposta igual para todos os casos é proposital: ela não revela se um e-mail ou CPF tem conta. Quando já existe uma conta, quem recebe o aviso é o **dono** dela, no e-mail cadastrado. Por isso o front não tem como (nem precisa) saber o que aconteceu.

### Confirmação de e-mail

O link enviado no cadastro tem o formato:

```
http://localhost:5173/confirmar-email#token=<token>
```

Como no convite, o token vai no **fragmento** (`#`). A tela `/confirmar-email` lê o token de `window.location.hash` (ou `route.hash`) e o envia ao `confirm`. O link vale por **24 horas** e só pode ser usado uma vez.

#### `POST /api/v1/auth/email-confirmation/confirm` 🌐

```json
{ "token": "<token do link>" }
```

- `204` → e-mail confirmado e conta ativada. Mostre "e-mail confirmado" e mande para o login.
- `400` → "Link de confirmação inválido ou expirado." Acontece com link vencido, já usado ou substituído por um reenvio. Ofereça o reenvio.

> Navegadores e antivírus às vezes abrem os links dos e-mails sozinhos, para verificar. Isso não confirma nada, porque a confirmação só acontece no `POST`, que a tela faz. Mesmo assim, chame o `confirm` **uma vez só** ao abrir a tela (cuidado com efeitos rodando duas vezes no modo de desenvolvimento): uma segunda chamada responde `400`, já que o token foi usado.

#### `POST /api/v1/auth/email-confirmation/resend` 🌐

```json
{ "email": "maria@exemplo.com" }
```

- `202` → sempre a mesma resposta:

  ```json
  { "message": "Se houver um cadastro pendente com este e-mail, enviamos um novo link de confirmação." }
  ```

  Só há envio quando existe uma conta **pendente** com esse e-mail. Cada reenvio **invalida os links anteriores**, então só o e-mail mais recente funciona. Vale avisar isso na tela.
- `400` → e-mail vazio ou inválido
- `429` → muitas tentativas (as rotas de `/auth` aceitam 10 por minuto por IP). Desabilite o botão de reenvio por alguns segundos depois de cada clique.

#### `POST /api/v1/auth/refresh` 🌐

```json
{ "refreshToken": "q8Zx..." }
```

- `200` → novo `AuthResponse` (substitua os dois tokens salvos)
- `401` → "Sessão inválida ou expirada." → mande para o login

#### `POST /api/v1/auth/logout` 🌐

```json
{ "refreshToken": "q8Zx..." }
```

- `204` → sessão encerrada. Apague os tokens locais mesmo se a chamada falhar.

#### `GET /api/v1/auth/me` 🔒

```json
{ "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "roles": ["Patient"] }
```

### Paciente

#### `GET /api/v1/patients/me` 🧑

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Maria Silva",
  "email": "maria@exemplo.com",
  "cpf": "123.456.789-09",
  "birthDate": "1995-04-20",
  "age": 31,
  "biologicalSex": "Female",
  "heightInCm": 165,
  "goal": "WeightLoss"
}
```

- `404` → "Perfil de paciente não encontrado."

#### `PUT /api/v1/patients/me` 🧑

Atualiza os dados do perfil. Nome, e-mail e CPF **não** são alterados por aqui.

```json
{
  "birthDate": "1995-04-20",
  "biologicalSex": "Female",
  "heightInCm": 166,
  "goal": "Maintenance"
}
```

- `200` → o perfil atualizado (mesmo formato do `GET`)
- `400` → erros de validação

### Convite de nutricionista

Nutricionistas não se cadastram sozinhos: o administrador faz o pré-cadastro, e a pessoa recebe um e-mail com um link no formato:

```
http://localhost:5173/nutri/convite#token=<token>
```

O token vai no **fragmento** (`#`), que o navegador nunca envia a servidores. A tela do convite precisa:

1. Ler o token de `window.location.hash` (ou `route.hash` no Vue Router).
2. Enviar o token no corpo do `POST` abaixo.

> O Vue Router precisa usar `createWebHistory()`, e não o modo hash (`createWebHashHistory()`). No modo hash, o `#token=...` conflitaria com as rotas.

#### `POST /api/v1/auth/invitations/accept` 🌐

```json
{ "token": "<token do link>", "password": "uma senha bem longa aqui" }
```

- `password` é obrigatória para quem **ainda não tem conta**. Quem já é paciente só ativa o papel de nutricionista e continua com a senha atual: nesse caso, envie `password` como `null`. Uma senha enviada por essa pessoa é ignorada.
- `204` → convite aceito. Mande para o login. Para quem definiu a senha agora, aceitar o convite também confirma o e-mail, já que o link chegou nele. Não é preciso confirmar de novo.
- `400` → "Convite inválido ou expirado." (o link vale por 72 horas e só pode ser usado uma vez)
- `400` → "Defina uma senha para ativar sua conta." → mostre o campo de senha e envie de novo

**Sugestão de tela:** comece só com o botão "Aceitar convite", sem senha. Se a resposta for "Defina uma senha...", mostre o campo de senha. Assim, quem já tem conta não precisa digitar nada.

### Administração

Todas as rotas desta seção exigem o papel `Admin` 🛡️.

#### Listagens paginadas

| Rota | Lista |
|---|---|
| `GET /api/v1/admin/users` | Todos os usuários |
| `GET /api/v1/admin/patients` | Pacientes |
| `GET /api/v1/admin/nutritionists` | Nutricionistas |

Parâmetros de query (todos opcionais):

| Parâmetro | Padrão | Descrição |
|---|---|---|
| `page` | `1` | Página, a partir de 1 |
| `pageSize` | `20` | Itens por página, de 1 a 100 |
| `search` | — | Busca no nome ou no e-mail, sem diferenciar maiúsculas (até 100 caracteres) |
| `isActive` | — | `true` ou `false` para filtrar pela situação da conta |

Exemplo: `GET /api/v1/admin/users?page=2&pageSize=10&search=maria&isActive=true`

O resultado vem do mais novo para o mais antigo, neste formato:

```json
{
  "items": [ ],
  "page": 2,
  "pageSize": 10,
  "totalItems": 37,
  "totalPages": 4
}
```

Formato de cada item em `items`:

- **users:** `id`, `name`, `email`, `isActive`, `isAdmin`, `isPatient`, `isNutritionist`, `createdAt`
- **patients:** `id`, `name`, `email`, `isActive`, `birthDate`, `goal`, `createdAt`
- **nutritionists:** `id`, `name`, `email`, `isActive`, `crnRegion`, `crnNumber`, `isProfileActive`, `createdAt`

> Em nutricionistas, `isActive` é a situação da **conta**, e `isProfileActive` indica se o **convite já foi aceito**. Nutricionista com `isProfileActive: false` é um convite pendente (dá para reenviar).

#### Detalhes

`GET /api/v1/admin/users/{id}`, `GET /api/v1/admin/patients/{id}` e `GET /api/v1/admin/nutritionists/{id}` devolvem o mesmo formato:

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Maria Silva",
  "email": "maria@exemplo.com",
  "cpf": "123.456.789-09",
  "isActive": true,
  "isAdmin": false,
  "patient": { "birthDate": "1995-04-20", "biologicalSex": "Female", "heightInCm": 165, "goal": "WeightLoss" },
  "nutritionist": null,
  "createdAt": "2026-10-01T12:00:00Z",
  "updatedAt": null
}
```

`patient` e `nutritionist` vêm `null` quando o usuário não tem esse papel. `nutritionist` tem `crnRegion`, `crnNumber` e `isActive`.

- `404` → usuário não encontrado, ou o usuário não tem o papel da rota (por exemplo, `/admin/patients/{id}` de quem não é paciente)

#### `POST /api/v1/admin/users/{id}/deactivate`

Desativa a conta e encerra todas as sessões dela.

- `204` → desativada
- `404` → usuário não encontrado
- `409` → "Você não pode desativar a sua própria conta."

#### `POST /api/v1/admin/users/{id}/activate`

- `204` → reativada
- `404` → usuário não encontrado
- `400` → "Senha não definida." (nutricionista que ainda não aceitou o convite: reenvie o convite em vez de ativar)

#### `POST /api/v1/admin/nutritionists`

Pré-cadastra um nutricionista e envia o convite por e-mail. Se o e-mail e o CPF já forem de um paciente, a mesma conta ganha o papel de nutricionista.

```json
{
  "name": "Ana Souza",
  "email": "ana@exemplo.com",
  "cpf": "987.654.321-00",
  "crnRegion": 3,
  "crnNumber": "12345"
}
```

| Campo | Regras |
|---|---|
| `name` | Obrigatório, até 150 caracteres |
| `email` | Obrigatório, e-mail válido, até 254 caracteres |
| `cpf` | Obrigatório, com dígitos verificadores válidos |
| `crnRegion` | De 1 a 11 |
| `crnNumber` | Obrigatório, até 20 caracteres |

- `201` → `{ "userId": "...", "invitationEmailSent": true }`
- `400` → erros de validação
- `409` → "Não foi possível pré-cadastrar o nutricionista com esses dados." (CRN já usado, já é nutricionista, ou e-mail e CPF pertencem a pessoas diferentes)

> Se `invitationEmailSent` vier `false`, o cadastro foi feito, mas o e-mail falhou. Mostre um aviso e ofereça o botão de reenviar o convite.

#### `POST /api/v1/admin/nutritionists/{id}/invitation`

Gera um convite novo e invalida os anteriores.

- `200` → `{ "invitationEmailSent": true }` (ou `false`, se o e-mail falhou)
- `404` → "Nutricionista não encontrado."
- `409` → "O nutricionista já aceitou o convite."

## Fluxos

### Login e escolha da área

```
POST /auth/login
  ├─ 200 → salva os tokens
  │        GET /auth/me → roles: ["Patient", "Nutritionist", ...]
  │                       ├─ um papel  → vai direto para a área dele
  │                       └─ vários    → mostra a escolha de área
  ├─ 401 → "E-mail ou senha inválidos."
  └─ 403 → "Confirme seu e-mail" + botão "Reenviar link" (POST /auth/email-confirmation/resend)
```

### Cadastro de paciente e confirmação de e-mail

```
Tela de cadastro
  POST /auth/register/patient
    ├─ 400 → mostra os erros nos campos
    └─ 202 → tela "Verifique seu e-mail" (com botão "Reenviar link")

E-mail → /confirmar-email#token=...
  POST /auth/email-confirmation/confirm { token }
    ├─ 204 → "E-mail confirmado!" → login
    └─ 400 → "Link inválido ou expirado" → campo de e-mail + "Reenviar link"
```

### Renovação automática do token

1. Antes de cada requisição, ou ao receber `401`, verifique se o `accessToken` venceu (`accessTokenExpiresAtUtc`).
2. Se venceu, chame `POST /auth/refresh` **uma única vez**: guarde a promise da renovação em andamento e faça as outras requisições esperarem por ela.
3. Salve o novo par e repita a requisição original **uma vez**.
4. Se a renovação responder `401`, apague os tokens e mande para o login.

Esboço com `fetch`:

```js
let refreshing = null

async function refreshTokens() {
  if (!refreshing) {
    refreshing = fetch(`${API}/api/v1/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: auth.refreshToken }),
    })
      .then(async (res) => {
        if (!res.ok) throw new Error('session-expired')
        auth.save(await res.json())
      })
      .finally(() => { refreshing = null })
  }
  return refreshing
}

export async function api(path, options = {}, retried = false) {
  const res = await fetch(`${API}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(auth.accessToken && { Authorization: `Bearer ${auth.accessToken}` }),
      ...options.headers,
    },
  })

  if (res.status === 401 && auth.refreshToken && !retried) {
    try {
      await refreshTokens()
    } catch {
      auth.clear()
      router.push('/login')
      throw new Error('Sessão expirada')
    }
    return api(path, options, true)
  }

  return res
}
```

> Se o app abrir em várias abas, cada aba pode tentar renovar o token ao mesmo tempo, o que dispara a detecção de reuso. Sincronize entre abas (por exemplo, com `BroadcastChannel` ou com o evento `storage`) ou deixe só uma aba renovar.

### Convite de nutricionista

```
Admin: POST /admin/nutritionists  →  e-mail com /nutri/convite#token=...
Nutri: abre o link → tela lê o token do hash
       POST /auth/invitations/accept { token, password? }
       ├─ 204 → vai para o login
       └─ 400 "Defina uma senha..." → pede a senha e envia de novo
```

## Checklist do frontend

- [ ] Rodar em `http://localhost:5173` (e não em `127.0.0.1`)
- [ ] Vue Router com `createWebHistory()`
- [ ] Rota `/nutri/convite` lendo o token do hash
- [ ] Rota `/confirmar-email` lendo o token do hash e chamando o `confirm` uma única vez
- [ ] Cadastro sem login automático: tela "verifique seu e-mail" depois do `202`
- [ ] Login tratando o `403` com a opção de reenviar o link
- [ ] Enviar `Authorization: Bearer <accessToken>` nas rotas protegidas
- [ ] Renovar o token uma vez por vez e salvar o novo `refreshToken`
- [ ] Mostrar os erros de `errors` nos campos do formulário
- [ ] Estado de carregamento para a primeira requisição no staging (até cerca de 1 minuto)
- [ ] Enums como texto (`"MuscleGain"`), e não como número
