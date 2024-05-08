using System.Diagnostics;

namespace KAT.Camelot.Domain.Extensions;

public static class LinqExtensions
{
	public static void ForAll<TSource>( this IEnumerable<TSource> items, Action<TSource> action )
	{
		foreach ( var i in items ) action( i );
	}

	public static IEnumerable<TSource> SkipUntil<TSource>( this IEnumerable<TSource> source, Func<TSource, TSource?, bool> predicate, bool includeCurrent = true )
	{
		var previous = default( TSource );

		using var e = source.GetEnumerator();
		while ( e.MoveNext() )
		{
			var element = e.Current;
			if ( predicate( element, previous ) )
			{
				if ( includeCurrent )
				{
					yield return element;
				}

				while ( e.MoveNext() )
				{
					yield return e.Current;
				}

				yield break;
			}
			previous = element;
		}
	}

	public static IEnumerable<T> TakeUntil<T>( this IEnumerable<T> source, Func<T, bool> predicate, bool includeCurrent = true )
	{
		foreach ( var item in source )
		{
			var condition = predicate( item );

			if ( !condition || includeCurrent )
			{
				yield return item;
			}

			if ( condition )
			{
				yield break;
			}
		}
	}

	/// <summary>
	/// Executes a foreach loop on an enumerable sequence, in which iterations may run
	/// in parallel, and returns the results of all iterations in the original order.
	/// </summary>
	/// <remarks>
	/// Original SO question: https://stackoverflow.com/q/60659896/166231
	/// This method is based on this answer: https://stackoverflow.com/a/60664631/166231.
	/// The answer first points to clean new .NET 6 support (documented by Hanselman as well) at https://stackoverflow.com/a/68901782/166231 but points out results are not returned.
	/// The answer then shared solution below at https://stackoverflow.com/a/71129678/166231.
	/// </remarks>
	public static Task<TResult[]> ForEachAsync<TSource, TResult>(
		this IEnumerable<TSource> source,
		ParallelOptions parallelOptions,
		Func<TSource, CancellationToken, ValueTask<TResult>> body
	)
	{
		// TODO: Consider getting exceptions and results on return (also in the TSource[] override): https://stackoverflow.com/questions/30907650/foreachasync-with-result/71129678?noredirect=1#comment138021907_71129678
		ArgumentNullException.ThrowIfNull( source );
		ArgumentNullException.ThrowIfNull( parallelOptions );
		ArgumentNullException.ThrowIfNull( body );

		List<TResult?> results = new();
		
		if ( source.TryGetNonEnumeratedCount( out var count ) ) results.Capacity = count;
		
		IEnumerable<(TSource, int)> withIndexes = source.Select( ( x, i ) => (x, i) );
		
		return Parallel.ForEachAsync( withIndexes, parallelOptions, async ( entry, ct ) =>
		{
			(var item, var index) = entry;

			var result = await body( item, ct ).ConfigureAwait( false );

			lock ( results )
			{
				while ( results.Count <= index ) results.Add( default );
				results[ index ] = result;
			}
		} ).ContinueWith( t =>
			{
				TaskCompletionSource<TResult[]> tcs = new();
				switch ( t.Status )
				{
					case TaskStatus.RanToCompletion:
						lock ( results ) tcs.SetResult( results.ToArray()! ); break;
					case TaskStatus.Faulted:
						tcs.SetException( t.Exception!.InnerExceptions ); break;
					case TaskStatus.Canceled:
						tcs.SetCanceled( new TaskCanceledException( t ).CancellationToken ); break;
					default: throw new UnreachableException();
				}
				return tcs.Task;
			}, 
			default, 
			TaskContinuationOptions.DenyChildAttach | TaskContinuationOptions.ExecuteSynchronously, 
			TaskScheduler.Default 
		).Unwrap();
	}

	/// <summary>
	/// Executes a foreach loop on an enumerable sequence, in which iterations may run
	/// in parallel, and returns the results of all iterations in the original order.
	/// </summary>
	/// <remarks>
	/// Array extension based on info from my question here: https://stackoverflow.com/q/76501926/166231
	/// </remarks>
	public static async Task<TResult[]> ForEachAsync<TSource, TResult>(
		this TSource[] source,
		ParallelOptions parallelOptions,
     	Func<TSource, CancellationToken, ValueTask<TResult>> body
	)
	{
		ArgumentNullException.ThrowIfNull( source );
		ArgumentNullException.ThrowIfNull( parallelOptions );
		ArgumentNullException.ThrowIfNull( body );

		var results = new TResult[ source.Length ];

		await Parallel.ForEachAsync(
			Enumerable.Range( 0, source.Length ),
			parallelOptions,
			async ( i, ct ) =>
			{
				results[ i ] = await body( source[ i ], ct ); // .ConfigureAwait( false );
			} );

		return results;
	}
}