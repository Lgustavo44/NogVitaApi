using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NogVita.Domain.Foods;

namespace NogVita.Infrastructure.Persistence.Seed;

public sealed class TacoFoodSeeder(NogVitaDbContext context, ILogger<TacoFoodSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var alreadyImported = await context.Foods.AnyAsync(f => f.Source == FoodSource.Taco, cancellationToken);

        if (alreadyImported)
        {
            logger.LogInformation("TACO já importada. Nada a fazer.");
            return;
        }

        var foods = TacoCsvReader.ReadFoods();

        context.Foods.AddRange(foods);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("TACO importada: {Count} alimentos.", foods.Count);
    }
}