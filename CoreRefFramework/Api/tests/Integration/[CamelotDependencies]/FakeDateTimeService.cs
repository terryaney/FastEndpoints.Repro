using FluentAssertions.Common;
using KAT.Camelot.Domain.Services;

namespace KAT.Camelot.Testing.Integration;

/// <summary>
/// A replacement for the static DateTime methods (Now, UtcNow, Today) for getting the current
/// date or time. Any entity utilizing this service can have its perception of time adjusted
/// from the outside, allowing tests to simulate the passage of time.
/// </summary>
/// <remarks>https://gist.github.com/jmsb/5716c2f9a2e8c7e163ac952b529b6386</remarks>
/// <remarks>Added FreezeTimeAt()</remarks>
/// <remarks>Changed FreezeTime to always 'reset' to 'Now' instead of only if not already frozen</remarks>
/// <remarks>UnFreezeTime - Why set/resume time from where it was frozen??  Seems I should just clear FrozenDateTime out?</remarks>
/// <remarks>Changed FreezeTime to take 'offset' into account.</remarks>
public class FakeDateTimeService : IDateTimeService
{
	private DateTime? FrozenDateTime { get; set; }
	private TimeSpan Offset { get; set; } = TimeSpan.Zero;

	public FakeDateTimeService()
	{
		RevertAllTimeTravel();
	}

	public DateTime Now => FrozenDateTime != null ? FrozenDateTime.Value : DateTime.Now.Add( Offset );
	public DateTime UtcNow => FrozenDateTime != null ? FrozenDateTime.Value.ToUniversalTime() : DateTime.UtcNow.Add( Offset );
	public DateTime Today => FrozenDateTime != null ? FrozenDateTime.Value.Date : DateTime.Today.Add( Offset ).Date;
	public DateTime UtcToday => FrozenDateTime != null ? FrozenDateTime.Value.ToUniversalTime().Date : DateTime.UtcNow.Add( Offset ).Date;
    public DateTimeOffset OffsetNow => FrozenDateTime != null ? FrozenDateTime.Value.ToDateTimeOffset() : DateTimeOffset.Now.Add( Offset );


	/// <summary>
	/// Move forward or backward in time by the specified amount of time.
	/// </summary>
	/// <param name="adjustment">The amount of time, forward or backward, to shift by.</param>
	public void TimeTravel( TimeSpan adjustment ) => Offset += adjustment;

	/// <summary>
	/// Move to the specific point in time provided.
	/// </summary>
	/// <param name="newDateTime">The point in time to move to.</param>
	public void TimeTravelTo( DateTime newDateTime ) => Offset = newDateTime.Subtract( DateTime.Now );

	/// <summary>
	/// Move to the specific point in time provided.
	/// </summary>
	/// <param name="newDateTimeUtc">The point in time to move to.</param>
	public void TimeTravelToUtc( DateTime newDateTimeUtc ) => Offset = newDateTimeUtc.Subtract( DateTime.UtcNow );

	/// <summary>
	/// Halts the progress of time.
	/// </summary>
	public DateTime FreezeTime() => FreezeTimeAt( DateTime.Now.Add( Offset ) );
	public DateTime FreezeTimeUtc() => FreezeTimeAt( DateTime.UtcNow.Add( Offset ) );
	public DateTime FreezeTimeAt( DateTime frozenDateTime ) => ( FrozenDateTime = frozenDateTime )!.Value;
	public DateTime FreezeTimeAt( TimeSpan adjustment ) => FreezeTimeAt( DateTime.Now.Add( adjustment ) );
	public DateTime FreezeTimeAtUtc( TimeSpan adjustment ) => FreezeTimeAt( DateTime.UtcNow.Add( adjustment ) );

	/// <summary>
	/// Resumes the progress of time.
	/// </summary>
	public void UnfreezeTime()
	{
		if ( FrozenDateTime != null )
		{
			TimeTravelTo( FrozenDateTime.Value );
			FrozenDateTime = null;
		}
	}

	/// <summary>
	/// Undoes all adjustments to the flow of time.
	/// </summary>
	public void RevertAllTimeTravel()
	{
		UnfreezeTime();
		Offset = TimeSpan.Zero;
	}

	/// <summary>
	/// Are we currently time traveling or not?
	/// </summary>
	/// <returns></returns>
	public bool IsCurrentlyTimeTraveling => FrozenDateTime != null || !Offset.Equals( TimeSpan.Zero );
}
