namespace NogVita.Infrastructure.Persistence;

// Funções do Postgres usadas nas consultas do EF, que viram SQL e nunca rodam no C#.
public static class NogVitaDbFunctions
{
    // D191: o public.f_unaccent, a versão IMMUTABLE do unaccent (criada na migration AddFoodNameTrigramIndex).
    // O Postgres só indexa funções imutáveis, e o unaccent original não é. E o índice só é usado se a consulta usar
    // exatamente a mesma expressão do índice: f_unaccent(name). Por isso a busca chama esta função, e não o
    // EF.Functions.Unaccent.
    public static string Unaccent(string value) =>
        throw new NotSupportedException("NogVitaDbFunctions.Unaccent só existe dentro de uma consulta do EF.");
}
