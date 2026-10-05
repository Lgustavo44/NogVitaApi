using FluentValidation;

namespace NogVita.Application.Common.Pagination;

public abstract class PageRequestValidator<T> : AbstractValidator<T> where T : PageRequest
{
    protected PageRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("A página precisa ser maior ou igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PageRequest.MaxPageSize)
            .WithMessage($"O tamanho da página precisa estar entre 1 e {PageRequest.MaxPageSize}.");
    }
}