using NogVita.Application.Common.Pagination;
using NogVita.Application.FollowUps;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.FollowUps;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.NutritionPlans;

public class NutritionistPlanQueriesUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeCareRelationshipRepository _relationshipRepository = new();
    private readonly StubPlanQueries _queries = new();
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly User _nutritionist;

    public NutritionistPlanQueriesUseCaseTests()
    {
        _nutritionist = new User("Nutri Teste", "nutri@email.com", Cpf.Create("12345678909"));
        _nutritionist.SetPasswordHash("hash");
        _nutritionist.ConfirmEmail(DateTime.UtcNow);
        _nutritionist.CreateNutritionistProfile(1, "123456");
        _nutritionist.NutritionistProfile!.Activate();
        _userRepository.Add(_nutritionist);
    }

    private NutritionistPlanQueriesUseCase CreateUseCase() =>
        new(new CareAccessService(_userRepository, _relationshipRepository), _queries);

    [Fact]
    public async Task Should_Hide_Plan_When_Relationship_Ended()
    {
        var relationship = new NutritionistRequest(_patientId, _nutritionist.Id, null).Accept(DateTime.UtcNow);
        _relationshipRepository.Add(relationship);
        _queries.Plan = CreateResponse();
        relationship.End(_patientId, DateTime.UtcNow);

        var plan = await CreateUseCase().GetAsync(_nutritionist.Id, _queries.Plan.Id, TestContext.Current.CancellationToken);

        Assert.Null(plan);
    }

    [Fact]
    public async Task Should_Return_Plan_While_Following_Patient()
    {
        _relationshipRepository.Add(new NutritionistRequest(_patientId, _nutritionist.Id, null).Accept(DateTime.UtcNow));
        _queries.Plan = CreateResponse();

        var plan = await CreateUseCase().GetAsync(_nutritionist.Id, _queries.Plan.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(plan);
    }

    private NutritionPlanResponse CreateResponse() =>
        new(Guid.NewGuid(), _patientId, "Paciente", _nutritionist.Id, "Nutri", "Plano", null,
            new DateOnly(2026, 10, 1), null, NutritionPlanStatus.Active, DateTime.UtcNow);

    private sealed class StubPlanQueries : INutritionPlanQueries
    {
        public NutritionPlanResponse? Plan { get; set; }

        public Task<NutritionPlanDetailResponse?> GetByNutritionistAsync(Guid nutritionistId, Guid planId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Plan is null ? null : new NutritionPlanDetailResponse(Plan, []));

        public Task<PagedResponse<NutritionPlanResponse>> ListByNutritionistAndPatientAsync(Guid nutritionistId, Guid patientId, NutritionPlanListRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PagedResponse<NutritionPlanResponse>> ListByPatientAsync(Guid patientId, NutritionPlanListRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<NutritionPlanDetailResponse?> GetByPatientAsync(Guid patientId, Guid planId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<NutritionPlanDetailResponse?> GetCurrentByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}