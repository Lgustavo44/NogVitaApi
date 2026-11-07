using FluentValidation;
using NogVita.Domain.Foods;

namespace NogVita.Application.Foods;

// Como um pedido diz quanto de um alimento (D-V2-34): ou em gramas, ou numa porção do alimento com a quantidade.
// Usado pelo plano (o nutricionista) e pelo diário (o paciente).
public interface IFoodAmountRequest
{
    decimal? QuantityInGrams { get; }
    Guid? PortionId { get; }
    decimal? PortionQuantity { get; }
}

public static class FoodAmount
{
    public const string ChooseOneMessage = "Informe a quantidade em gramas ou uma porção com a quantidade, não os dois.";

    // A porção é procurada no próprio alimento: uma porção de outro alimento devolve null (vira 404).
    public static FoodQuantity? Resolve(Food food, IFoodAmountRequest request)
    {
        if (request.PortionId is not { } portionId)
        {
            return FoodQuantity.InGrams(request.QuantityInGrams!.Value);
        }

        var portion = food.FindPortion(portionId);
        return portion is null ? null : FoodQuantity.OfPortion(portion, request.PortionQuantity!.Value);
    }

    // As mesmas regras da FoodQuantity, para o 400 vir com o erro no campo (o domínio lançaria exceção).
    public static void AddFoodAmountRules<T>(this AbstractValidator<T> validator) where T : IFoodAmountRequest
    {
        validator.RuleFor(r => r)
            .Must(r => r.QuantityInGrams is not null ^ r.PortionId is not null)
            .WithMessage(ChooseOneMessage)
            .OverridePropertyName(nameof(IFoodAmountRequest.QuantityInGrams));

        validator.RuleFor(r => r.QuantityInGrams)
            .GreaterThan(0).WithMessage("A quantidade precisa ser maior que zero.")
            .LessThanOrEqualTo(FoodQuantity.MaxGrams).WithMessage($"A quantidade pode ser no máximo {FoodQuantity.MaxGrams:0} g.")
            .When(r => r.QuantityInGrams is not null);

        validator.RuleFor(r => r.PortionQuantity)
            .NotNull().WithMessage("Informe quantas porções.")
            .GreaterThan(0).WithMessage("A quantidade de porções precisa ser maior que zero.")
            .LessThanOrEqualTo(FoodQuantity.MaxPortionQuantity).WithMessage($"A quantidade de porções pode ser no máximo {FoodQuantity.MaxPortionQuantity:0}.")
            .When(r => r.PortionId is not null);
    }
}
