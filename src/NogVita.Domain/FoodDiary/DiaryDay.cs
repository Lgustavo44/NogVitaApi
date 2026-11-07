using NogVita.Domain.Common;
using NogVita.Domain.Foods;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Domain.FoodDiary;

public enum MealAdherence
{
    Followed,
    Partial,
    Skipped
}

// O check-in de uma refeição do plano. Planned é o retrato dos nutrientes da refeição no momento do check-in (D-V2-38):
// editar o plano depois não muda o que o diário registrou.
public sealed record MealCheckIn(Guid MealId, string MealName, int Order, MealAdherence Adherence, string? Notes, NutrientAmounts Planned);

// Um alimento comido fora do plano, com o retrato do alimento (como o FoodSnapshot do plano) e a quantidade.
// MealId nulo: "fora das refeições".
public sealed record ExtraFood(
    Guid Id,
    Guid? MealId,
    Guid FoodId,
    string Name,
    string? Brand,
    FoodSource Source,
    decimal? EnergyKcalPer100g,
    decimal? ProteinPer100g,
    decimal? CarbohydratePer100g,
    decimal? FatPer100g,
    decimal? FiberPer100g,
    decimal? SodiumMgPer100g,
    decimal QuantityInGrams,
    string? PortionName,
    decimal? PortionGrams,
    decimal? PortionQuantity)
{
    // Um método, e não uma propriedade, para não ir parar no JSON da coluna.
    public NutrientAmounts ComputeAmounts() => NutritionCalculator.Portion(
        new NutrientsPer100g(EnergyKcalPer100g, ProteinPer100g, CarbohydratePer100g, FatPer100g, FiberPer100g, SodiumMgPer100g),
        QuantityInGrams);
}

// D-V2-37: o diário de um dia do paciente. Um registro por paciente e por dia, criado no primeiro check-in ou extra.
public class DiaryDay : Entity
{
    public const int MaxDaysInPast = 7;
    public const int NotesMaxLength = 300;
    public const int MaxExtraFoods = 30;

    public Guid PatientId { get; private set; }
    public DateOnly Date { get; private set; }
    public IReadOnlyList<MealCheckIn> MealCheckIns { get; private set; } = [];
    public IReadOnlyList<ExtraFood> ExtraFoods { get; private set; } = [];

    private DiaryDay() { } // EF Core

    public DiaryDay(Guid patientId, DateOnly date, DateOnly today)
    {
        EnsureEditable(date, today);
        PatientId = patientId;
        Date = date;
    }

    public static void EnsureEditable(DateOnly date, DateOnly today)
    {
        if (date > today)
        {
            throw new DomainException("O diário não aceita datas no futuro.");
        }

        if (date < today.AddDays(-MaxDaysInPast))
        {
            throw new DomainException($"O diário só pode ser preenchido até {MaxDaysInPast} dias atrás.");
        }
    }

    // Um check-in por refeição: responder de novo substitui o anterior. As listas são trocadas inteiras, para o EF
    // perceber a mudança na coluna jsonb.
    public void CheckIn(Meal meal, NutrientAmounts planned, MealAdherence adherence, string? notes, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(meal);
        EnsureEditable(Date, today);

        if (!Enum.IsDefined(adherence))
        {
            throw new DomainException("Resposta inválida para a refeição.");
        }

        var checkIn = new MealCheckIn(meal.Id, meal.Name, meal.Order, adherence, NormalizeNotes(notes), planned);

        MealCheckIns = MealCheckIns
            .Where(c => c.MealId != meal.Id)
            .Append(checkIn)
            .OrderBy(c => c.Order)
            .ToList();
    }

    public bool ClearCheckIn(Guid mealId, DateOnly today)
    {
        EnsureEditable(Date, today);

        if (MealCheckIns.All(c => c.MealId != mealId))
        {
            return false;
        }

        MealCheckIns = MealCheckIns.Where(c => c.MealId != mealId).ToList();
        return true;
    }

    public ExtraFood AddExtra(Food food, FoodQuantity quantity, Guid? mealId, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(food);
        ArgumentNullException.ThrowIfNull(quantity);
        EnsureEditable(Date, today);

        if (!food.IsActive)
        {
            throw new DomainException("Um alimento inativo não pode ser registrado.");
        }

        if (ExtraFoods.Count >= MaxExtraFoods)
        {
            throw new DomainException($"Um dia pode ter no máximo {MaxExtraFoods} alimentos fora do plano.");
        }

        var nutrients = food.Nutrients;
        var extra = new ExtraFood(
            Guid.CreateVersion7(), mealId, food.Id, food.Name, food.Brand, food.Source,
            nutrients.EnergyKcal, nutrients.Protein, nutrients.Carbohydrate, nutrients.Fat, nutrients.Fiber, nutrients.SodiumMg,
            quantity.Grams, quantity.PortionName, quantity.PortionGrams, quantity.PortionQuantity);

        ExtraFoods = ExtraFoods.Append(extra).ToList();
        return extra;
    }

    public bool RemoveExtra(Guid extraId, DateOnly today)
    {
        EnsureEditable(Date, today);

        if (ExtraFoods.All(e => e.Id != extraId))
        {
            return false;
        }

        ExtraFoods = ExtraFoods.Where(e => e.Id != extraId).ToList();
        return true;
    }

    private static string? NormalizeNotes(string? notes)
    {
        var normalized = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        return normalized is { Length: > NotesMaxLength }
            ? throw new DomainException($"As observações podem ter no máximo {NotesMaxLength} caracteres.")
            : normalized;
    }
}
