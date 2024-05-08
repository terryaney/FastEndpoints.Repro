using System.Data;
using KAT.Camelot.Domain.Services;
using Microsoft.Data.SqlClient;

namespace KAT.Camelot.Testing.Integration;

public class FakeDbConnectionForge : IDbConnectionForge
{
	private readonly string connectionString;

	public FakeDbConnectionForge( string connectionString )
	{
		this.connectionString = connectionString;
	}
	public Task<IDbConnection> CreateAWSConnectionAsync( CancellationToken cancellationToken = default ) => throw new NotImplementedException();
	public Task<IDbConnection> CreateAWSConnectionAsync( string? client, CancellationToken cancellationToken = default ) => throw new NotImplementedException();
	public Task<IDbConnection> CreateDataLockerConnectionAsync( CancellationToken cancellationToken = default ) => CreateConnectionAsync( connectionString, cancellationToken: cancellationToken );
	public Task<IDbConnection> CreateDataLockerConnectionAsync( string? client, CancellationToken cancellationToken = default ) => CreateConnectionAsync( connectionString, cancellationToken: cancellationToken );
	public Task<IDbConnection> CreatexDSConnectionAsync( CancellationToken cancellationToken = default ) => CreateConnectionAsync( connectionString, cancellationToken: cancellationToken );
	public Task<IDbConnection> CreatexDSConnectionAsync( int timeout, CancellationToken cancellationToken = default ) => CreateConnectionAsync( connectionString, cancellationToken: cancellationToken );
	public Task<IDbConnection> CreatexDSConnectionAsync( string? client, CancellationToken cancellationToken = default ) => CreateConnectionAsync( connectionString, cancellationToken: cancellationToken );
	public Task<IDbConnection> CreatexDSConnectionAsync( string? client, int timeout, CancellationToken cancellationToken = default ) => CreateConnectionAsync( connectionString, cancellationToken: cancellationToken );
	public string? GetDataLockerConnectionString() => connectionString;
	public string? GetDataLockerConnectionString( string? client ) => connectionString;
	public string? GetHangfireConnectionString() => throw new NotImplementedException();
	public string? GetHangfireConnectionString( string? client ) => throw new NotImplementedException();

	private static async Task<IDbConnection> CreateConnectionAsync( string? connectionString, int timeout = 4, CancellationToken cancellationToken = default )
	{
		if ( timeout != 15 )
		{
			connectionString = $"Connection Timeout={timeout};" + connectionString;
		}

		var connection = new SqlConnection( connectionString );
		await connection.OpenAsync( cancellationToken );
		return connection;
	}
}