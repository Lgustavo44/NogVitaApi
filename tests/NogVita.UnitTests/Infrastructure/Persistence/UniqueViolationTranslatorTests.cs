using Microsoft.EntityFrameworkCore;
using NogVita.Infrastructure.Persistence;
using Npgsql;

namespace NogVita.UnitTests.Infrastructure.Persistence;

public class UniqueViolationTranslatorTests
{
    private static DbUpdateException UniqueViolation(string? index) =>
        new("Falha ao salvar.", new PostgresException(
            "duplicate key value violates unique constraint", "ERROR", "ERROR",
            PostgresErrorCodes.UniqueViolation, constraintName: index));

    [Fact]
    public void Should_Translate_Known_Index_To_Its_Message()
    {
        Assert.True(UniqueViolationTranslator.TryTranslate(UniqueViolation("ux_weight_record_patient_day"), out var conflict));
        Assert.Equal("Já existe um peso registrado neste dia.", conflict!.Message);
    }

    [Fact]
    public void Should_Use_Default_Message_For_Unknown_Index()
    {
        Assert.True(UniqueViolationTranslator.TryTranslate(UniqueViolation("ux_indice_novo"), out var conflict));
        Assert.Equal(UniqueViolationTranslator.DefaultMessage, conflict!.Message);
    }

    [Fact]
    public void Should_Not_Reveal_Which_Account_Data_Already_Exists()
    {
        UniqueViolationTranslator.TryTranslate(UniqueViolation("ix_tb_user_cpf"), out var cpfConflict);
        UniqueViolationTranslator.TryTranslate(UniqueViolation("ix_tb_user_email"), out var emailConflict);

        // A mesma mensagem nos dois casos, e sem citar CPF nem e-mail.
        Assert.Equal(cpfConflict!.Message, emailConflict!.Message);
        Assert.DoesNotContain("CPF", cpfConflict.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("e-mail", cpfConflict.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Should_Keep_Original_Exception_For_The_Log()
    {
        var original = UniqueViolation("ux_food_barcode");

        UniqueViolationTranslator.TryTranslate(original, out var conflict);

        Assert.Same(original, conflict!.InnerException);
    }

    [Fact]
    public void Should_Ignore_Other_Database_Errors()
    {
        var foreignKey = new DbUpdateException("Falha ao salvar.", new PostgresException(
            "violates foreign key constraint", "ERROR", "ERROR", PostgresErrorCodes.ForeignKeyViolation));

        Assert.False(UniqueViolationTranslator.TryTranslate(foreignKey, out var conflict));
        Assert.Null(conflict);
    }
}
