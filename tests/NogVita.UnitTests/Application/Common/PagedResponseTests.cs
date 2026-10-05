using NogVita.Application.Common.Pagination;

namespace NogVita.UnitTests.Application.Common;

public class PagedResponseTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(100, 20, 5)]
    [InlineData(101, 20, 6)]
    public void Should_Calculate_Total_Pages(int totalItems, int pageSize, int expectedPages)
    {
        var response = new PagedResponse<string>([], 1, pageSize, totalItems);

        Assert.Equal(expectedPages, response.TotalPages);
    }

    [Theory]
    [InlineData(1, 20, 0)]
    [InlineData(2, 20, 20)]
    [InlineData(3, 50, 100)]
    public void Should_Calculate_Skip(int page, int pageSize, int expectedSkip)
    {
        var request = new PageRequest { Page = page, PageSize = pageSize };

        Assert.Equal(expectedSkip, request.Skip);
    }
}