namespace NogVita.UnitTests;

// Fase 23: os testes que fazem trabalho pesado de verdade (ler 22 mil linhas, gerar um PDF, rodar o PBKDF2).
// Eles continuam rodando por padrão. No ciclo do dia a dia, dá para pular com:
//   dotnet test --project tests/NogVita.UnitTests --filter-not-trait "Category=Slow"
public static class TestCategories
{
    public const string Key = "Category";
    public const string Slow = "Slow";
}
