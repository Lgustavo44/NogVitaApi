<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="docs/assets/logo-dark.svg">
    <img src="docs/assets/logo.svg" alt="NogVita" width="360">
  </picture>
</p>

# NogVita

Plataforma de acompanhamento nutricional que conecta **pacientes**, **nutricionistas** e **administradores**.

O paciente encontra um nutricionista e solicita acompanhamento. O nutricionista monta planos alimentares com cálculo nutricional automático, baseado em uma fonte externa de dados nutricionais.

> 🚧 **Projeto de estudo em desenvolvimento.** Criado para praticar .NET, arquitetura e engenharia de software.

## Funcionalidades

### Implementadas

- **Autenticação com JWT:** login, token de acesso de curta duração e endpoint de usuário autenticado
- **Refresh token:** renovação com rotação, detecção de reuso e logout
- **Papéis combináveis:** um mesmo usuário pode ser paciente, nutricionista e/ou administrador
- **Administrador inicial** criado automaticamente a partir da configuração
- **Validação de entrada:** requisições inválidas respondem 400 com os erros por campo
- **CPF validado:** normalização e verificação matemática dos dígitos verificadores
- **Política de senha** baseada na recomendação do NIST (SP 800-63B-4)
- **Cadastro de paciente:** conta e perfil em uma única etapa; a conta fica inativa até o e-mail ser confirmado
- **Confirmação de e-mail:** link de uso único, válido por 24 horas, com reenvio que revoga o link anterior
- **Perfil do paciente:** consulta e atualização dos próprios dados, com idade calculada
- **Painel do administrador:** listagens paginadas de usuários, pacientes e nutricionistas, com busca e filtros
- **Ativação e desativação de contas:** desativar revoga todas as sessões do usuário
- **Pré-cadastro de nutricionistas pelo administrador**, com convite enviado por e-mail (link de uso único, válido por 72 horas)
- **Aceite do convite:** um usuário novo define a senha (o que também confirma o e-mail, já que o link chegou nele); um usuário que já é paciente só ativa o novo papel, sem trocar a senha
- **E-mails transacionais** com layout próprio (convite, confirmação e aviso de conta existente), enviados por SMTP em desenvolvimento e pela API HTTP da Brevo em staging

### Em desenvolvimento

- Solicitações de acompanhamento
- Planos alimentares com cálculo nutricional
- Registro de peso

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/v1/auth/login` | Autentica e devolve os tokens (`403` se o e-mail não foi confirmado) |
| `POST` | `/api/v1/auth/refresh` | Renova os tokens (com rotação) |
| `POST` | `/api/v1/auth/logout` | Encerra a sessão |
| `GET` | `/api/v1/auth/me` | Dados do usuário autenticado |
| `GET` | `/health` | Saúde da API e do banco |
| `POST` | `/api/v1/auth/register/patient` | Cadastra um paciente e envia o link de confirmação |
| `POST` | `/api/v1/auth/email-confirmation/confirm` | Confirma o e-mail e ativa a conta |
| `POST` | `/api/v1/auth/email-confirmation/resend` | Reenvia o link de confirmação (invalida os anteriores) |
| `GET` | `/api/v1/patients/me` | Perfil do paciente autenticado |
| `PUT` | `/api/v1/patients/me` | Atualiza o perfil do paciente autenticado |
| `GET` | `/api/v1/admin/users` | Lista usuários (paginado, com busca e filtro) |
| `GET` | `/api/v1/admin/users/{id}` | Detalhes de um usuário |
| `POST` | `/api/v1/admin/users/{id}/deactivate` | Desativa a conta e revoga as sessões |
| `POST` | `/api/v1/admin/users/{id}/activate` | Reativa a conta |
| `GET` | `/api/v1/admin/patients` | Lista pacientes (paginado) |
| `GET` | `/api/v1/admin/patients/{id}` | Detalhes de um paciente |
| `GET` | `/api/v1/admin/nutritionists` | Lista nutricionistas (paginado) |
| `GET` | `/api/v1/admin/nutritionists/{id}` | Detalhes de um nutricionista |
| `POST` | `/api/v1/admin/nutritionists` | Pré-cadastra um nutricionista e envia o convite |
| `POST` | `/api/v1/admin/nutritionists/{id}/invitation` | Reenvia o convite (invalida os anteriores) |
| `POST` | `/api/v1/auth/invitations/accept` | Aceita o convite |

> 📘 Formatos de requisição e resposta, erros e fluxos estão no **[guia de consumo da API](docs/API.md)**.

### Confirmação de e-mail

O cadastro de paciente cria a conta **inativa** e envia um link de confirmação por e-mail, válido por 24 horas. A conta só é ativada quando o link é usado.

| Método | Rota | Resposta |
|---|---|---|
| `POST` | `/api/v1/auth/register/patient` | `202`, sempre com a mesma mensagem genérica |
| `POST` | `/api/v1/auth/email-confirmation/confirm` | `204` confirmado, `400` link inválido ou expirado |
| `POST` | `/api/v1/auth/email-confirmation/resend` | `202`, sempre com a mesma mensagem genérica |

O que acontece em cada caso de cadastro (a resposta é sempre a mesma `202`):

| Situação | Resultado |
|---|---|
| E-mail e CPF novos | Cria a conta inativa e envia o link de confirmação |
| E-mail de uma conta **pendente** | Gera um link novo (revogando o anterior) e reenvia; a senha não muda |
| E-mail de uma conta **confirmada** | Avisa o dono do e-mail que já existe uma conta |
| CPF de outra conta, com outro e-mail | Não cria nada; avisa o dono do CPF no e-mail cadastrado dele e registra um log, só com o Id |

Decisões de segurança:

- As respostas do cadastro e do reenvio são sempre iguais e não revelam se um e-mail ou CPF já está cadastrado.
- Se o e-mail ou o CPF já tem uma conta, o aviso vai para o e-mail **do dono da conta**, e nunca para o endereço digitado no formulário.
- Os tokens são guardados apenas como hash SHA-256, são de uso único, e cada reenvio revoga o link anterior.
- O login só informa "confirme seu e-mail" (`403`) depois de a senha estar correta.
- Um cadastro repetido nunca altera a senha da conta pendente.
- Os usuários que já existiam antes da confirmação de e-mail foram migrados como confirmados.

## Ambiente de staging

A API de testes está publicada em:

- **API:** `https://nogvitaapi.onrender.com`
- **Swagger:** `https://nogvitaapi.onrender.com/swagger`

> O ambiente usa o plano gratuito da Render: depois de 15 minutos sem uso, ele "dorme", e a primeira requisição pode levar cerca de 1 minuto. Use **apenas dados de teste**.

### Para o frontend

- **Origem liberada no CORS:** `http://localhost:5173` (o padrão do Vite). Acesse sempre por `localhost`, e não por `127.0.0.1`.
- **Autenticação:** envie o token de acesso no header `Authorization: Bearer <accessToken>`. O token de acesso dura 15 minutos; use o `POST /api/v1/auth/refresh` com o `refreshToken` para obter um par novo. Faça **uma renovação por vez**: duas renovações simultâneas com o mesmo refresh token encerram todas as sessões (detecção de reuso).
- **Erros:** todas as respostas de erro seguem o formato `ProblemDetails` (`title`, `status` e, em erros de validação, `errors` por campo).
- **Enums:** trafegam como texto (ex.: `"goal": "MuscleGain"`).
- **Cadastro de paciente:** responde `202` **sem tokens**. Mostre "verifique seu e-mail", e não faça login automático.
- **Confirmação de e-mail:** o link abre `/confirmar-email#token=...` no front. A tela lê o token do fragmento da URL e o envia no corpo do `POST /api/v1/auth/email-confirmation/confirm`.
- **Login com e-mail não confirmado:** um `403` significa "e-mail não confirmado". Mostre a opção de reenviar o link (`POST /api/v1/auth/email-confirmation/resend`).
- **Convite de nutricionista:** o e-mail leva para `/nutri/convite#token=...`. O Vue Router precisa usar `createWebHistory()` (e não o modo hash). A tela lê o token do fragmento da URL e o envia no corpo do `POST /api/v1/auth/invitations/accept`.

O guia completo, com exemplos de cada endpoint, está em [docs/API.md](docs/API.md).

## Stack

**Em uso:** C# · .NET 10 · ASP.NET Core Web API · Entity Framework Core · PostgreSQL · JWT · FluentValidation · xUnit · Docker (banco de dados) · OpenAPI / Swagger UI

**Planejado:** Docker Compose (API + banco) · testes de integração

## Arquitetura

Clean Architecture em quatro camadas:

```
src/
├── NogVita.Api              → exposição HTTP
├── NogVita.Application      → casos de uso e validação de entrada
├── NogVita.Domain           → entidades, value objects e regras de negócio
└── NogVita.Infrastructure   → banco de dados, segurança e integrações externas

tests/
├── NogVita.UnitTests
└── NogVita.IntegrationTests
```

**Regra de dependência:** o Domain não depende de nenhuma outra camada nem de pacotes externos. A Application define contratos (repositórios, hash de senha, geração de tokens), e a Infrastructure os implementa.

**Duas camadas de validação:**
- **Entrada (FluentValidation):** verifica se a requisição está bem preenchida e devolve todos os erros de uma vez
- **Domínio (entidades e value objects):** garante que nenhum objeto inválido exista, como um CPF com dígitos verificadores errados

**Leitura e escrita separadas (CQRS leve):** as operações que alteram dados passam por casos de uso e pelas regras do domínio. As listagens usam serviços de consulta que projetam direto do banco para o DTO, com paginação e sem carregar entidades inteiras.

## Segurança

- Senhas armazenadas com PBKDF2 (HMAC-SHA512, 100 mil iterações)
- Política de senha do NIST: mínimo de 15 caracteres, sem regras de composição e com lista de bloqueio
- Refresh tokens armazenados apenas como hash SHA-256
- Mesma resposta para qualquer falha de login, evitando enumeração de usuários
- Limite de tamanho em senhas e tokens, protegendo o servidor contra entradas gigantes
- Segredos fora do código (User Secrets em desenvolvimento)
- Nenhum dado pessoal nos tokens nem nos logs
- Autorização por papel (`Patient`, `Nutritionist`, `Admin`) e acesso aos próprios dados sempre pelo token, nunca por Ids enviados pelo cliente
- Conflitos de cadastro com mensagem genérica, sem revelar se um CPF ou e-mail já tem conta (LGPD)
- Tokens de convite armazenados apenas como hash, com validade e uso único
- O link do convite leva o token no fragmento da URL, que nunca é enviado a servidores
- O aceite do convite nunca altera a senha de uma conta existente

## Identidade visual

<img src="docs/assets/icon.svg" alt="Ícone NogVita" width="64" align="left">

O logotipo é só o nome, com o "o" trocado por uma fatia de laranja. A fatia sozinha também funciona como ícone do app.

<br clear="left">

| Cor | Hex | Uso |
|---|---|---|
| ![#F28C28](https://placehold.co/16x16/F28C28/F28C28.png) Laranja | `#F28C28` | Casca e gomos da fatia |
| ![#FFD08A](https://placehold.co/16x16/FFD08A/FFD08A.png) Polpa | `#FFD08A` | Interior da fatia |
| ![#1D2A24](https://placehold.co/16x16/1D2A24/1D2A24.png) Verde-escuro | `#1D2A24` | Texto do logotipo |

Os arquivos ficam em [`docs/assets/`](docs/assets/): `logo.svg` (fundo claro), `logo-dark.svg` (fundo escuro) e `icon.svg` (ícone).

## Como rodar (desenvolvimento)

**Pré-requisitos:** .NET SDK 10 e Docker.

1. Suba o PostgreSQL:

   ```bash
   docker run -d --name nogvita-postgres -e "POSTGRES_USER=nogvita" -e "POSTGRES_PASSWORD=<senha>" -e "POSTGRES_DB=nogvita" -p 127.0.0.1:5432:5432 -v nogvita-pgdata:/var/lib/postgresql/data postgres:17
   ```

   Suba também o servidor de e-mail de desenvolvimento (os e-mails aparecem em `http://localhost:8025`):

   ```bash
   docker run -d --name nogvita-mailpit -p 127.0.0.1:1025:1025 -p 127.0.0.1:8025:8025 axllent/mailpit:v1.31.4
   ```

2. Configure os segredos com `dotnet user-secrets` no projeto `src/NogVita.Api`:
   - `ConnectionStrings:NogVita`
   - `Jwt:SecretKey` (32 bytes aleatórios, em base64)
   - `AdminSeed:Name`, `AdminSeed:Email`, `AdminSeed:Cpf` (CPF válido), `AdminSeed:Password` (mínimo de 15 caracteres)

3. Restaure as ferramentas e aplique as migrations:

   ```bash
   dotnet tool restore
   dotnet ef database update --project src/NogVita.Infrastructure --startup-project src/NogVita.Api
   ```

4. Rode a API:

   ```bash
   dotnet run --project src/NogVita.Api --launch-profile https
   ```

5. Acesse o Swagger em `https://localhost:7194/swagger`
