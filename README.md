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

### Em desenvolvimento

- Cadastro de pacientes e validação de CPF
- Gestão de nutricionistas pelo administrador, com convite
- Solicitações de acompanhamento
- Planos alimentares com cálculo nutricional
- Registro de peso

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/v1/auth/login` | Autentica e devolve os tokens |
| `POST` | `/api/v1/auth/refresh` | Renova os tokens (com rotação) |
| `POST` | `/api/v1/auth/logout` | Encerra a sessão |
| `GET` | `/api/v1/auth/me` | Dados do usuário autenticado |
| `GET` | `/health` | Saúde da API e do banco |

## Stack

**Em uso:** C# · .NET 10 · ASP.NET Core Web API · Entity Framework Core · PostgreSQL · JWT · xUnit · Docker (banco de dados) · OpenAPI / Swagger UI

**Planejado:** FluentValidation · Docker Compose (API + banco) · testes de integração

## Arquitetura

Clean Architecture em quatro camadas:

```
src/
├── NogVita.Api              → exposição HTTP
├── NogVita.Application      → casos de uso
├── NogVita.Domain           → entidades e regras de negócio
└── NogVita.Infrastructure   → banco de dados, segurança e integrações externas

tests/
├── NogVita.UnitTests
└── NogVita.IntegrationTests
```

**Regra de dependência:** o Domain não depende de nenhuma outra camada nem de pacotes externos. A Application define contratos (repositórios, hash de senha, geração de tokens), e a Infrastructure os implementa.

## Segurança

- Senhas armazenadas com PBKDF2 (HMAC-SHA512, 100 mil iterações)
- Refresh tokens armazenados apenas como hash SHA-256
- Mesma resposta para qualquer falha de login, evitando enumeração de usuários
- Segredos fora do código (User Secrets em desenvolvimento)
- Nenhum dado pessoal nos tokens nem nos logs

## Como rodar (desenvolvimento)

**Pré-requisitos:** .NET SDK 10 e Docker.

1. Suba o PostgreSQL:

   ```bash
   docker run -d --name nogvita-postgres -e POSTGRES_USER=nogvita -e POSTGRES_PASSWORD=<senha> -e POSTGRES_DB=nogvita -p 127.0.0.1:5432:5432 -v nogvita-pgdata:/var/lib/postgresql/data postgres:17
   ```

2. Configure os segredos com `dotnet user-secrets` no projeto `src/NogVita.Api`:
   - `ConnectionStrings:NogVita`
   - `Jwt:SecretKey` (32 bytes aleatórios, em base64)
   - `AdminSeed:Name`, `AdminSeed:Email`, `AdminSeed:Cpf`, `AdminSeed:Password`

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
