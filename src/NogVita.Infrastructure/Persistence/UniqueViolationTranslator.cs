using Microsoft.EntityFrameworkCore;
using NogVita.Application.Common;
using Npgsql;

namespace NogVita.Infrastructure.Persistence;

// D169: só a Infrastructure sabe que o banco é o Postgres e quais são os nomes dos índices.
// A API recebe uma ConflictException com a mensagem pronta.
internal static class UniqueViolationTranslator
{
    public const string DefaultMessage = "Este registro já existe. Atualize a página e confira.";

    // Para CPF e e-mail, a mesma mensagem genérica do cadastro (D62): nunca revela se um dado já tem conta.
    private const string AccountDataMessage = "Não foi possível concluir o cadastro com esses dados.";

    private static readonly Dictionary<string, string> MessagesByIndex = new(StringComparer.Ordinal)
    {
        ["ux_care_relationship_patient_active"] = "Este paciente já está em acompanhamento.",
        ["ux_nutritionist_request_patient_pending"] = "Você já tem uma solicitação pendente.",
        ["ux_nutrition_plan_patient_active"] = "O paciente já tem um plano ativo.",
        ["ux_meal_food_meal_food"] = "Este alimento já está na refeição. Altere a quantidade dele.",
        ["ux_weight_record_patient_day"] = "Já existe um peso registrado neste dia.",
        ["ux_food_barcode"] = "Este alimento já está cadastrado.",
        ["ux_food_source_reference"] = "Este alimento já está cadastrado.",
        ["ix_tb_nutritionist_profile_crn_region_crn_number"] = "Não foi possível pré-cadastrar o nutricionista com esses dados.",
        ["ix_tb_user_cpf"] = AccountDataMessage,
        ["ix_tb_user_email"] = AccountDataMessage
    };

    public static bool TryTranslate(DbUpdateException exception, out ConflictException? conflict)
    {
        if (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres)
        {
            var message = postgres.ConstraintName is { } index && MessagesByIndex.TryGetValue(index, out var known)
                ? known
                : DefaultMessage;

            conflict = new ConflictException(message, exception);
            return true;
        }

        conflict = null;
        return false;
    }
}
