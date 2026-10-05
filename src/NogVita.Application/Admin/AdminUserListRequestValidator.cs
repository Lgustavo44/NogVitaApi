using FluentValidation;
using NogVita.Application.Common.Pagination;

namespace NogVita.Application.Admin;

public sealed class AdminUserListRequestValidator : PageRequestValidator<AdminUserListRequest>
{
    public AdminUserListRequestValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(100).WithMessage("A busca pode ter no máximo 100 caracteres.");
    }
}