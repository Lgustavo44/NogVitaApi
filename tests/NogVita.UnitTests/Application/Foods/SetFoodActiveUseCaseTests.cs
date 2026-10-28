using NogVita.Application.Admin;
using NogVita.Application.Foods;
using NogVita.Domain.Audit;
using NogVita.Domain.Foods;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Foods;

public class SetFoodActiveUseCaseTests
{
    private static readonly Guid AdminId = Guid.NewGuid();

    private readonly FakeFoodRepository _foodRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CapturingLogger<AdminAuditLog> _auditLogger = new();

    private SetFoodActiveUseCase CreateUseCase() => new(_foodRepository, _unitOfWork, new AdminAuditLog(_auditLogger));

    private Food AddRice()
    {
        var rice = Food.FromTaco(3, "Arroz, tipo 1, cozido", "Cereais e derivados",
            new NutrientsPer100g(128.26m, 2.52m, 28.06m, 0.23m, 1.56m, 1.22m));
        _foodRepository.Add(rice);
        return rice;
    }

    [Fact]
    public async Task Should_Deactivate_And_Audit()
    {
        var rice = AddRice();

        var found = await CreateUseCase().ExecuteAsync(AdminId, rice.Id, active: false, TestContext.Current.CancellationToken);

        Assert.True(found);
        Assert.False(rice.IsActive);
        AuditAssert.Recorded(_auditLogger, AdminId, AdminAuditAction.FoodDeactivated, rice.Id);
    }

    [Fact]
    public async Task Should_Activate_And_Audit()
    {
        var rice = AddRice();
        rice.Deactivate();

        await CreateUseCase().ExecuteAsync(AdminId, rice.Id, active: true, TestContext.Current.CancellationToken);

        Assert.True(rice.IsActive);
        AuditAssert.Recorded(_auditLogger, AdminId, AdminAuditAction.FoodActivated, rice.Id);
    }

    [Fact]
    public async Task Should_Not_Audit_Unknown_Food()
    {
        var found = await CreateUseCase().ExecuteAsync(AdminId, Guid.NewGuid(), active: false, TestContext.Current.CancellationToken);

        Assert.False(found);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
        AuditAssert.Nothing(_auditLogger);
    }
}
