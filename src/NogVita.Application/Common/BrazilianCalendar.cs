namespace NogVita.Application.Common;

public static class BrazilianCalendar
{
    private static readonly TimeSpan BrasiliaOffset = TimeSpan.FromHours(-3);

    public static DateOnly Today(TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().ToOffset(BrasiliaOffset).DateTime);
}