using NogVita.Application.Common.Pagination;
using NogVita.Application.FollowUps;
using NogVita.Application.WeightRecords;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.WeightRecords;

public class NutritionistWeightQueriesUseCaseTests
{
    private readonly CareScenario _care = new(following: false);
    private readonly StubWeightQueries _queries = new();


    private NutritionistWeightQueriesUseCase CreateUseCase() =>
        new(_care.CareAccess(), _queries);

    [Fact]
    public async Task Should_List_History_While_Following_Patient()
    {
        _care.Follow();

        var page = await CreateUseCase().ListForPatientAsync(
            _care.Nutritionist.Id, _care.Patient.Id, new WeightHistoryRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(page);
        Assert.Equal(_care.Patient.Id, _queries.RequestedPatientId);
    }

    [Fact]
    public async Task Should_Not_Query_History_Of_Patient_Not_Followed()
    {
        var page = await CreateUseCase().ListForPatientAsync(
            _care.Nutritionist.Id, _care.Patient.Id, new WeightHistoryRequest(), TestContext.Current.CancellationToken);

        Assert.Null(page);
        Assert.Null(_queries.RequestedPatientId);
    }

    [Fact]
    public async Task Should_Not_Query_History_Of_Patient_Blocked_By_Admin()
    {
        _care.Follow();
        _care.Patient.Deactivate(DateTime.UtcNow);

        var page = await CreateUseCase().ListForPatientAsync(
            _care.Nutritionist.Id, _care.Patient.Id, new WeightHistoryRequest(), TestContext.Current.CancellationToken);

        Assert.Null(page);
        Assert.Null(_queries.RequestedPatientId);
    }

    private sealed class StubWeightQueries : IWeightRecordQueries
    {
        public Guid? RequestedPatientId { get; private set; }

        public Task<PagedResponse<WeightRecordResponse>> ListByPatientAsync(
            Guid patientId, WeightHistoryRequest request, CancellationToken cancellationToken = default)
        {
            RequestedPatientId = patientId;
            return Task.FromResult(new PagedResponse<WeightRecordResponse>([], request.Page, request.PageSize, 0));
        }

        public Task<LatestWeightResponse?> GetLatestAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
