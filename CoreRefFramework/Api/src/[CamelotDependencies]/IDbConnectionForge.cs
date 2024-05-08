using System.Data;

namespace KAT.Camelot.Domain.Services;

public interface IDbConnectionForge
{
	Task<IDbConnection> CreateAWSConnectionAsync( CancellationToken cancellationToken = default );
	Task<IDbConnection> CreateAWSConnectionAsync( string? client, CancellationToken cancellationToken = default );

	string? GetDataLockerConnectionString();
	string? GetDataLockerConnectionString( string? client );
	Task<IDbConnection> CreateDataLockerConnectionAsync( CancellationToken cancellationToken = default );
	Task<IDbConnection> CreateDataLockerConnectionAsync( string? client, CancellationToken cancellationToken = default );

	Task<IDbConnection> CreatexDSConnectionAsync( CancellationToken cancellationToken = default );
	Task<IDbConnection> CreatexDSConnectionAsync( int timeout, CancellationToken cancellationToken = default );
	Task<IDbConnection> CreatexDSConnectionAsync( string? client, CancellationToken cancellationToken = default );
	Task<IDbConnection> CreatexDSConnectionAsync( string? client, int timeout, CancellationToken cancellationToken = default );

	string? GetHangfireConnectionString();
	string? GetHangfireConnectionString( string? client );
}