using Microsoft.Extensions.Logging.Abstractions;
using NogVita.Application.Common;
using NogVita.Application.Common.Pagination;
using NogVita.Application.Documents;
using NogVita.Application.FollowUps;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.Documents;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.NutritionPlans;

public class PlanPdfUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly StubQueries _queries = new();
    private readonly StubRenderer _renderer = new();
    private readonly FakeIssuedDocumentRepository _documents = new();
    private readonly User _patient;
    private readonly User _nutritionist;

    public PlanPdfUseCaseTests()
    {
        _patient = new User("Paciente Teste", "paciente@email.com", Cpf.Create("11144477735"));
        _userRepository.Add(_patient);

        _nutritionist = new User("Nutri Teste", "nutri@email.com", Cpf.Create("52998224725"));
        _nutritionist.CreateNutritionistProfile(3, "12345");
        _userRepository.Add(_nutritionist);
    }

    private PlanPdfUseCase CreateUseCase() =>
        new(
            _queries,
            new NutritionistPlanQueriesUseCase(
                new CareAccessService(_userRepository, new FakeCareRelationshipRepository()), _queries),
            _userRepository,
            CreateIssuer(),
            _renderer,
            _emailSender,
            NullLogger<PlanPdfUseCase>.Instance);

    private DocumentIssuer CreateIssuer() =>
        new(_userRepository, _documents, new FakeUnitOfWork(), new FrontendSettings { BaseUrl = "https://nogvita.vercel.app/" }, TimeProvider.System);

    private NutritionPlanDetailResponse CreateDetail() =>
        new(
            new NutritionPlanResponse(Guid.NewGuid(), Guid.NewGuid(), "Paciente", _nutritionist.Id, "Nutri Teste", "Hipertrofia", null,
                new DateOnly(2026, 10, 15), null, NutritionPlanStatus.Active, DateTime.UtcNow, null, null, null, null),
            []);

    [Fact]
    public async Task Should_Email_Pdf_To_The_Account_Address()
    {
        _queries.Detail = CreateDetail();

        var result = await CreateUseCase().EmailToPatientAsync(_patient.Id, _queries.Detail.Plan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(EmailPlanPdfResult.Sent, result);
        var email = Assert.Single(_emailSender.SentMessages);
        Assert.Equal("paciente@email.com", email.To);
        var attachment = Assert.Single(email.Attachments!);
        Assert.Equal("nogvita-plano-2026-10-15.pdf", attachment.FileName);
        Assert.Equal("application/pdf", attachment.ContentType);
    }

    [Fact]
    public async Task Should_Not_Email_Plan_Not_Visible_To_Patient()
    {
        _queries.Detail = null;

        var result = await CreateUseCase().EmailToPatientAsync(_patient.Id, Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(EmailPlanPdfResult.NotFound, result);
        Assert.Empty(_emailSender.SentMessages);
    }

    [Fact]
    public async Task Should_Report_Email_Failure()
    {
        _queries.Detail = CreateDetail();
        _emailSender.ShouldFail = true;

        var result = await CreateUseCase().EmailToPatientAsync(_patient.Id, _queries.Detail.Plan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(EmailPlanPdfResult.EmailFailed, result);
    }

    // Res. CFN 760/2023, art. 10: o PDF leva o CRN e um código verificador, e a emissão fica registrada para a conferência.
    [Fact]
    public async Task Should_Stamp_And_Register_Every_Pdf()
    {
        _queries.Detail = CreateDetail();

        var file = await CreateUseCase().RenderForPatientAsync(_patient.Id, _queries.Detail.Plan.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(file);
        var stamp = _renderer.LastStamp!;
        Assert.Equal(3, stamp.CrnRegion);
        Assert.Equal("12345", stamp.CrnNumber);
        Assert.Matches("^[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}$", stamp.VerificationCode);

        var document = Assert.Single(_documents.Documents);
        Assert.Equal(stamp.VerificationCode.Replace("-", ""), document.Code);
        Assert.Equal($"https://nogvita.vercel.app/verificar-documento/{document.Code}", stamp.VerificationUrl);
        Assert.Equal(DocumentKind.NutritionPlan, document.Kind);
        Assert.Equal(_queries.Detail.Plan.Id, document.SourceId);
        Assert.Equal(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(file.Content)), document.Sha256);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_Verify_A_Code_With_Or_Without_Dashes(bool typedLowercaseWithDashes)
    {
        _queries.Detail = CreateDetail();
        await CreateUseCase().RenderForPatientAsync(_patient.Id, _queries.Detail.Plan.Id, TestContext.Current.CancellationToken);
        var code = _renderer.LastStamp!.VerificationCode;

        var result = await CreateIssuer().VerifyAsync(
            typedLowercaseWithDashes ? code.ToLowerInvariant() : code.Replace("-", ""), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(code, result.Code);
        Assert.Equal("Nutri Teste", result.NutritionistName);
        Assert.Equal(3, result.CrnRegion);
    }

    [Theory]
    [InlineData("ABCD-EFGH-JKLM")]
    [InlineData("curto")]
    public async Task Should_Not_Verify_An_Unknown_Code(string code)
    {
        var result = await CreateIssuer().VerifyAsync(code, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    private sealed class StubRenderer : INutritionPlanPdfRenderer
    {
        public DocumentStamp? LastStamp { get; private set; }

        public byte[] Render(NutritionPlanDetailResponse plan, DocumentStamp stamp)
        {
            LastStamp = stamp;
            return [0x25, 0x50, 0x44, 0x46];
        }
    }

    private sealed class StubQueries : INutritionPlanQueries
    {
        public NutritionPlanDetailResponse? Detail { get; set; }

        public Task<NutritionPlanDetailResponse?> GetByPatientAsync(Guid patientId, Guid planId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Detail);

        public Task<NutritionPlanDetailResponse?> GetByNutritionistAsync(Guid nutritionistId, Guid planId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Detail);

        public Task<NutritionPlanDetailResponse?> GetCurrentByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PagedResponse<NutritionPlanResponse>> ListByNutritionistAndPatientAsync(Guid nutritionistId, Guid patientId, NutritionPlanListRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PagedResponse<NutritionPlanResponse>> ListByPatientAsync(Guid patientId, NutritionPlanListRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
