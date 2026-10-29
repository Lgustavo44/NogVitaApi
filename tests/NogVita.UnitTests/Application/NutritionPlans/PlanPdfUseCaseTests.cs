using Microsoft.Extensions.Logging.Abstractions;
using NogVita.Application.Common.Pagination;
using NogVita.Application.FollowUps;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.NutritionPlans;

public class PlanPdfUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly StubQueries _queries = new();
    private readonly User _patient;

    public PlanPdfUseCaseTests()
    {
        _patient = new User("Paciente Teste", "paciente@email.com", Cpf.Create("11144477735"));
        _userRepository.Add(_patient);
    }

    private PlanPdfUseCase CreateUseCase() =>
        new(
            _queries,
            new NutritionistPlanQueriesUseCase(
                new CareAccessService(_userRepository, new FakeCareRelationshipRepository()), _queries),
            _userRepository,
            new StubRenderer(),
            _emailSender,
            TimeProvider.System,
            NullLogger<PlanPdfUseCase>.Instance);

    private static NutritionPlanDetailResponse CreateDetail() =>
        new(
            new NutritionPlanResponse(Guid.NewGuid(), Guid.NewGuid(), "Paciente", Guid.NewGuid(), "Nutri", "Hipertrofia", null,
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

    private sealed class StubRenderer : INutritionPlanPdfRenderer
    {
        public byte[] Render(NutritionPlanDetailResponse plan, DateOnly generatedOn) => [0x25, 0x50, 0x44, 0x46];
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
