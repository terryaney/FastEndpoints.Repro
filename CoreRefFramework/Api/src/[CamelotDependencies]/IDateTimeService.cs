namespace KAT.Camelot.Domain.Services;

// See BTR.Camelot.Api.xDS.IntegrationTests.Fakes from original to see why we have this
public interface IDateTimeService
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
    DateTime Today { get; }
    DateTime UtcToday { get; }
    DateTimeOffset OffsetNow { get; }
}