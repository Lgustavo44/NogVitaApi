using FluentValidation;
using NogVita.Application.Abstractions;

namespace NogVita.Application.Patients;

// LGPD, art. 11, I (dados de saúde) e art. 33, VIII (transferência internacional): dois aceites separados,
// porque o art. 33, VIII, pede que a transferência seja distinguida "claramente de outras finalidades".
// Mudou o texto, a versão sobe, e cada paciente precisa aceitar de novo.
// ⚠️ Mantenha a política de privacidade (docs/legal) coerente com este texto.
public static class HealthDataConsent
{
    // Versão 2 (07/11/2026): o texto passou a citar a anamnese, o diário alimentar e os check-ins semanais.
    public const string CurrentVersion = "2";

    public const string HealthDataText =
        "Autorizo o NogVita a tratar os meus dados de saúde (data de nascimento, sexo biológico, altura, objetivo, rotina, " +
        "peso, planos alimentares, respostas da anamnese, diário alimentar, check-ins semanais e, se eu enviar, fotos de evolução). " +
        "Eles são dados pessoais sensíveis (LGPD, art. 11, I) " +
        "e serão usados só para o meu acompanhamento nutricional, vistos apenas por mim e pelo nutricionista que me acompanha. " +
        "Sei que esses dados são necessários para usar o NogVita como paciente, e que posso retirar esta autorização " +
        "a qualquer momento, pedindo a exclusão da conta.";

    public const string InternationalTransferText =
        "Autorizo que esses dados sejam guardados e processados fora do Brasil (LGPD, art. 33, VIII), " +
        "pelos serviços que o NogVita usa: Render e Vercel (Estados Unidos), Supabase (banco de dados e arquivos) e Brevo (União Europeia), " +
        "como descrito na Política de Privacidade.";

    public static readonly HealthDataConsentTermResponse Term = new(CurrentVersion, HealthDataText, InternationalTransferText);
}

public sealed record HealthDataConsentTermResponse(string Version, string HealthDataText, string InternationalTransferText);

public sealed record HealthDataConsentResponse(
    string Version,
    string HealthDataText,
    string InternationalTransferText,
    bool Accepted,
    DateTime? AcceptedAtUtc);

public sealed record AcceptHealthDataConsentRequest(string Version, bool AcceptHealthData, bool AcceptInternationalTransfer);

public static class HealthDataConsentMessages
{
    public const string VersionRequired = "Informe a versão do termo que você leu.";
    public const string HealthDataRequired = "Para usar o NogVita como paciente, autorize o tratamento dos seus dados de saúde.";
    public const string InternationalTransferRequired = "Para usar o NogVita, autorize a transferência dos seus dados para os serviços no exterior.";
    public const string ConsentRequired = "Autorize o tratamento dos seus dados de saúde para continuar. O termo está na sua área de paciente.";
}

public sealed class AcceptHealthDataConsentRequestValidator : AbstractValidator<AcceptHealthDataConsentRequest>
{
    public AcceptHealthDataConsentRequestValidator()
    {
        RuleFor(r => r.Version).NotEmpty().WithMessage(HealthDataConsentMessages.VersionRequired);
        RuleFor(r => r.AcceptHealthData).Equal(true).WithMessage(HealthDataConsentMessages.HealthDataRequired);
        RuleFor(r => r.AcceptInternationalTransfer).Equal(true).WithMessage(HealthDataConsentMessages.InternationalTransferRequired);
    }
}

public enum HealthDataConsentResult
{
    Done,
    ProfileNotFound,
    OutdatedVersion
}

// Para quem já era paciente antes do termo: o cadastro novo já grava o aceite.
public sealed class HealthDataConsentUseCase(IUserRepository userRepository, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<HealthDataConsentResponse?> GetAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var profile = (await userRepository.GetByIdAsync(patientId, cancellationToken))?.PatientProfile;
        if (profile is null)
        {
            return null;
        }

        var accepted = profile.HasHealthDataConsent(HealthDataConsent.CurrentVersion);

        return new HealthDataConsentResponse(
            HealthDataConsent.CurrentVersion,
            HealthDataConsent.HealthDataText,
            HealthDataConsent.InternationalTransferText,
            accepted,
            accepted ? profile.HealthDataConsentAtUtc : null);
    }

    public async Task<HealthDataConsentResult> AcceptAsync(Guid patientId, AcceptHealthDataConsentRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await userRepository.GetByIdAsync(patientId, cancellationToken);
        if (patient?.PatientProfile is null)
        {
            return HealthDataConsentResult.ProfileNotFound;
        }

        // O paciente aceita o texto que leu. Se o termo mudou enquanto a tela estava aberta, ele precisa ler o novo.
        if (request.Version != HealthDataConsent.CurrentVersion)
        {
            return HealthDataConsentResult.OutdatedVersion;
        }

        patient.GrantHealthDataConsent(request.Version, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return HealthDataConsentResult.Done;
    }
}
