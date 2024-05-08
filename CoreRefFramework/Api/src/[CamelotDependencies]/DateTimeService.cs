using KAT.Camelot.Domain.Services;

namespace KAT.Camelot.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Today => DateTime.Today;
    public DateTime UtcToday => DateTime.UtcNow.Date;
    public DateTimeOffset OffsetNow => DateTimeOffset.Now;
}
