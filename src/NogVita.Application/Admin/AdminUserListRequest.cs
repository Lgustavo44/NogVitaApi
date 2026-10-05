using NogVita.Application.Common.Pagination;

namespace NogVita.Application.Admin;

public sealed record AdminUserListRequest : PageRequest
{
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
}