using System.Diagnostics.CodeAnalysis;
using System.Text;
using KAT.Camelot.Domain.Extensions;
using NJsonSchema.Generation;

namespace KAT.Camelot.Api;

[ExcludeFromCodeCoverage( Justification = "Only used to generate Swagger documentation" )]
public class ContractsNameGenerator : ISchemaNameGenerator
{
	public string? Generate( Type type )
	{
		var isGeneric = type.IsGenericType;
		var fullNameWithoutGenericArgs = isGeneric
			? type.FullName![ ..type.FullName!.IndexOf( '`' ) ]
			: type.FullName;

		var nameParts = fullNameWithoutGenericArgs!.Split( '.' );
		var contractPrefixes = new[] { "Requests", "Responses" };

		var shortName = nameParts.Any( p => contractPrefixes.Contains( p ) )
			? string.Join( "_", nameParts.SkipUntil( ( c, p ) => contractPrefixes.Contains( c ) ) )
			: fullNameWithoutGenericArgs.Replace( ".", "" )!;

		return isGeneric
			? shortName + GenericArgString( type )
			: shortName;

		static string? GenericArgString( Type type )
		{
			if ( type.IsGenericType )
			{
				var sb = new StringBuilder();
				var args = type.GetGenericArguments();
				for ( var i = 0; i < args.Length; i++ )
				{
					var arg = args[ i ];
					if ( i == 0 ) sb.Append( "Of" );
					sb.Append( TypeNameWithoutGenericArgs( arg ) );
					sb.Append( GenericArgString( arg ) );
					if ( i < args.Length - 1 ) sb.Append( "And" );
				}
				return sb.ToString();
			}
			return type.Name;

			static string TypeNameWithoutGenericArgs( Type type )
			{
				var index = type.Name.IndexOf( '`' );
				index = index == -1 ? 0 : index;
				return type.Name![ ..index ];
			}
		}
	}
}