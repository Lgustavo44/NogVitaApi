using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.Foods;

namespace NogVita.Infrastructure.Persistence.Configurations;

internal static class NutrientsMapping
{
    public static void Configure(ComplexPropertyBuilder<NutrientsPer100g> nutrients)
    {
        nutrients.Ignore(n => n.IsComplete);

        nutrients.Property(n => n.EnergyKcal).HasPrecision(9, 4);
        nutrients.Property(n => n.Protein).HasPrecision(9, 4);
        nutrients.Property(n => n.Carbohydrate).HasPrecision(9, 4);
        nutrients.Property(n => n.Fat).HasPrecision(9, 4);
        nutrients.Property(n => n.Fiber).HasPrecision(9, 4);
        nutrients.Property(n => n.SodiumMg).HasPrecision(9, 4);
    }
}
