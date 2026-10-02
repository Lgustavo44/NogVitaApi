# NogVita

Plataforma de acompanhamento nutricional que conecta **pacientes**, **nutricionistas** e **administradores**.

O paciente encontra um nutricionista e solicita acompanhamento. O nutricionista monta planos alimentares com cálculo nutricional automático, baseado em uma fonte externa de dados nutricionais.

> 🚧 **Projeto de estudo em desenvolvimento.** Criado para praticar .NET, arquitetura e engenharia de software. Ainda não há funcionalidades de negócio implementadas.

## Stack

**Em uso:** C# · .NET 10 · ASP.NET Core Web API · xUnit

**Planejado:** Entity Framework Core · PostgreSQL · JWT · FluentValidation · Docker

## Arquitetura

Clean Architecture em quatro camadas:

```
src/
├── NogVita.Api              → exposição HTTP
├── NogVita.Application      → casos de uso
├── NogVita.Domain           → entidades e regras de negócio
└── NogVita.Infrastructure   → banco de dados e integrações externas

tests/
├── NogVita.UnitTests
└── NogVita.IntegrationTests
```

