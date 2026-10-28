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
    private readonly CareScenario _care = new(following: false);
    private readonly StubPlanQueries _queries = new();

    private NutritionistPlanQueriesUseCase CreateUseCase() =>
        new(_care.CareAccess(), _queries);

    [Fact]
    public async Task Should_Hide_Plan_When_Relationship_Ended()
    {
        var relationship = TestUsers.Follow(_care.Patient.Id, _care.Nutritionist.Id);
        _care.Relationships.Add(relationship);
        _queries.Plan = CreateResponse();
        relationship.End(_care.Patient.Id, DateTime.UtcNow);

        var plan = await CreateUseCase().GetAsync(_care.Nutritionist.Id, _queries.Plan.Id, TestContext.Current.CancellationToken);

        Assert.Null(plan);
    }

    [Fact]
    public async Task Should_Return_Plan_While_Following_Patient()
    {
        _care.Follow();
        _queries.Plan = CreateResponse();

        var plan = await CreateUseCase().GetAsync(_care.Nutritionist.Id, _queries.Plan.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(plan);
    }

    private NutritionPlanResponse CreateResponse() =>
        new(Guid.NewGuid(), _care.Patient.Id, "Paciente", _care.Nutritionist.Id, "Nutri", "Plano", null,
            new DateOnly(2026, 10, 1), null, NutritionPlanStatus.Active,
            TargetEnergyKcal: null, TargetProteinGrams: null, TargetCarbohydrateGrams: null, TargetFatGrams: null,
            CreatedAt: DateTime.UtcNow);

    [Fact]
    public async Task Should_List_Plans_While_Following_Patient()
    {
        _care.Follow();
        _queries.Plan = CreateResponse();

        var page = await CreateUseCase().ListForPatientAsync(
            _care.Nutritionist.Id, _care.Patient.Id, new NutritionPlanListRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(page);
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task Should_Not_List_Plans_Of_Patient_Not_Followed()
    {
        _queries.Plan = CreateResponse();

        var page = await CreateUseCase().ListForPatientAsync(
            _care.Nutritionist.Id, _care.Patient.Id, new NutritionPlanListRequest(), TestContext.Current.CancellationToken);

        Assert.Null(page);
    }

    [Fact]
    public async Task Should_Return_Null_For_Unknown_Plan()
    {
        _care.Follow();

        Assert.Null(await CreateUseCase().GetAsync(_care.Nutritionist.Id, Guid.NewGuid(), TestContext.Current.CancellationToken));
    }

    private sealed class StubPlanQueries : INutritionPlanQueries
    {
        public NutritionPlanResponse? Plan { get; set; }

        public Task<NutritionPlanDetailResponse?> GetByNutritionistAsync(Guid nutritionistId, Guid planId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Plan is null ? null : new NutritionPlanDetailResponse(Plan, []));

        public Task<PagedResponse<NutritionPlanResponse>> ListByNutritionistAndPatientAsync(Guid nutritionistId, Guid patientId, NutritionPlanListRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResponse<NutritionPlanResponse>(Plan is null ? [] : [Plan], request.Page, request.PageSize, Plan is null ? 0 : 1));

        public Task<PagedResponse<NutritionPlanResponse>> ListByPatientAsync(Guid patientId, NutritionPlanListRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<NutritionPlanDetailResponse?> GetByPatientAsync(Guid patientId, Guid planId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<NutritionPlanDetailResponse?> GetCurrentByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}