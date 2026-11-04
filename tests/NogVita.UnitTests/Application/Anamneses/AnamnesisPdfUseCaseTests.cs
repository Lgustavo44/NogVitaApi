using NogVita.Application.Anamneses;
using NogVita.Application.Common;
using NogVita.Application.Common.Pagination;
using NogVita.Application.Documents;
using NogVita.Domain.Anamneses;
using NogVita.Domain.Documents;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Anamneses;

public class AnamnesisPdfUseCaseTests
{
    private readonly CareScenario _care = new();
    private readonly StubQueries _queries = new();
    private readonly FakeIssuedDocumentRepository _documents = new();
    private readonly Question _goal = Question.Create("Qual é o seu objetivo?", QuestionType.Text, true, null);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private AnamnesisPdfUseCase UseCase() =>
        new(
            _queries,
            new NutritionistAnamnesisQueriesUseCase(_care.CareAccess(), _queries),
            new StubRenderer(),
            new DocumentIssuer(_care.Users, _documents, new FakeUnitOfWork(), new FrontendSettings { BaseUrl = "https://nogvita.vercel.app" }, TimeProvider.System));

    private AnamnesisResponse Anamnesis(AnamnesisStatus status) =>
        new(Guid.NewGuid(), "Anamnese inicial", status, _care.Patient.Id, _care.Patient.Name, _care.Nutritionist.Id, _care.Nutritionist.Name,
            new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc),
            status == AnamnesisStatus.Submitted ? new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc) : null,
            [_goal],
            status == AnamnesisStatus.Submitted ? [new Answer(_goal.Id, Text: "Emagrecer")] : null);

    // Res. CFN 760/2023, art. 10: como o plano, o PDF da anamnese é um documento com código verificador.
    [Fact]
    public async Task Submitted_Anamnesis_Should_Become_A_Registered_Document()
    {
        _queries.Detail = Anamnesis(AnamnesisStatus.Submitted);

        var result = await UseCase().RenderForNutritionistAsync(_care.Nutritionist.Id, _queries.Detail.Id, _ct);

        Assert.Equal(AnamnesisPdfStatus.Done, result.Status);
        Assert.Equal("nogvita-anamnese-2026-11-02.pdf", result.File!.FileName);
        var document = Assert.Single(_documents.Documents);
        Assert.Equal(DocumentKind.Anamnesis, document.Kind);
        Assert.Equal(_queries.Detail.Id, document.SourceId);
    }

    [Fact]
    public async Task Pending_Anamnesis_Has_Nothing_To_Print()
    {
        _queries.Detail = Anamnesis(AnamnesisStatus.Pending);

        var result = await UseCase().RenderForPatientAsync(_care.Patient.Id, _queries.Detail.Id, _ct);

        Assert.Equal(AnamnesisPdfStatus.NotSubmitted, result.Status);
        Assert.Empty(_documents.Documents);
    }

    // D-V2-30: sem o vínculo, o nutricionista não vê a anamnese, nem em PDF.
    [Fact]
    public async Task Nutritionist_Without_The_Follow_Up_Should_Not_Get_The_Pdf()
    {
        _queries.Detail = Anamnesis(AnamnesisStatus.Submitted);
        _care.Relationships.Relationships.Clear();

        var result = await UseCase().RenderForNutritionistAsync(_care.Nutritionist.Id, _queries.Detail.Id, _ct);

        Assert.Equal(AnamnesisPdfStatus.NotFound, result.Status);
        Assert.Empty(_documents.Documents);
    }

    private sealed class StubRenderer : IAnamnesisPdfRenderer
    {
        public byte[] Render(AnamnesisResponse anamnesis, DocumentStamp stamp) => [0x25, 0x50, 0x44, 0x46];
    }

    private sealed class StubQueries : IAnamnesisQueries
    {
        public AnamnesisResponse? Detail { get; set; }

        public Task<AnamnesisResponse?> GetByNutritionistAsync(Guid nutritionistId, Guid anamnesisId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Detail);

        public Task<AnamnesisResponse?> GetByPatientAsync(Guid patientId, Guid anamnesisId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Detail);

        public Task<PagedResponse<QuestionnaireSummaryResponse>> ListQuestionnairesAsync(Guid nutritionistId, AnamnesisListRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<QuestionnaireResponse?> GetQuestionnaireAsync(Guid nutritionistId, Guid questionnaireId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PagedResponse<AnamnesisSummaryResponse>> ListByNutritionistAndPatientAsync(Guid nutritionistId, Guid patientId, AnamnesisListRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PagedResponse<AnamnesisSummaryResponse>> ListByPatientAsync(Guid patientId, AnamnesisListRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
