using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace KAT.Camelot.Domain.Extensions;

public static class JsonExtensions
{
	public static void Save( this JsonObject _, string path ) => ( (JsonNode)_ ).Save( path );
	public static void Save( this JsonArray _, string path ) => ( (JsonNode)new JsonObject().AddProperties( new JsonKeyProperty( "Array", _ ) ) ).Save( path );
	public static void Save( this JsonNode _, string path )
	{
		using var fileStream = File.Create( path );
		JsonSerializer.Serialize( fileStream, _, new JsonSerializerOptions { WriteIndented = true } );
	}

	public static string ToJsonString( this object _ ) => _.ToJsonString( writeIndented: false, ignoreNulls: false );
	public static string ToJsonString( this object _, bool writeIndented = false, bool ignoreNulls = false, bool camelCase = false )
	{
		return JsonSerializer.Serialize( _, new JsonSerializerOptions() { 
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			WriteIndented = writeIndented,
			PropertyNamingPolicy = camelCase ? JsonNamingPolicy.CamelCase : null,
			DefaultIgnoreCondition = ignoreNulls ? JsonIgnoreCondition.WhenWritingNull : JsonIgnoreCondition.Never
		} );
	}	

	public static void SetProperty( this JsonObject optionsContainer, string path, string? value, bool parseValue )
	{
		if ( string.IsNullOrEmpty( value ) ) return;

		var optionJson = optionsContainer;

		var optionNames = path.Split( '.' );

		var propertyValue = GetJsonValue( value, parseValue );

		// Build up a json object...
		// chart.title.text, Hello = { chart: { title: { text: "Hello } } }
		// annotations[0].labels[0], { point: 'series1.69', text: 'Life Exp' } = { annotations: [ { labels: [ { point: 'series1.69', text: 'Life Exp' } ] } ] }
		for ( var k = 0; k < optionNames.Length; k++ )
		{
			var optionName = optionNames[ k ];
			var optionIndex = -1;
			if ( optionName.EndsWith( "]" ) )
			{
				var nameParts = optionName.Split( '[' );
				optionName = nameParts[ 0 ];

				var rowIndex = nameParts[ 1 ][ ..^1 ];

				optionIndex = string.Compare( "empty", rowIndex, true ) == 0 ? -2 : int.Parse( rowIndex );
			}

			var onPropertyValue = k == optionNames.Length - 1;

			// When you are on the last name part, instead of setting it
			// to new {} object, set it appropriately to the value passed in CE
			var newValue = onPropertyValue
				? propertyValue
				: new JsonObject();

			if ( newValue != null )
			{
				var propertyContainer = optionJson[ optionName ];

				if ( optionIndex == -2 )
				{
					// Empty array...
					optionJson.Add( optionName, new JsonArray() );
				}
				else if ( optionIndex > -1 )
				{
					if ( propertyContainer is not JsonArray propertyArray )
					{
						optionJson.Add( optionName, propertyArray = new JsonArray() );
					}
					// If property is an array and index isn't there yet, push a new element
					while ( propertyArray.Count - 1 < optionIndex )
					{
						propertyArray.Add( new JsonObject() );
					}

					if ( onPropertyValue )
					{
						// If on property value and exists, this is an override, so just replace the value
						propertyArray[ optionIndex ] = newValue;
					}
				}
				else
				{
					if ( propertyContainer == null )
					{
						optionJson.Add( optionName, newValue );
					}
					else if ( onPropertyValue )
					{
						// If on property value and exists, this is an override, so just replace the value
						optionJson[ optionName ] = newValue;
					}
				}

				// Reset my local variable to the most recently added/created object
				optionJson = optionIndex > -1
					? ( optionJson[ optionName ]![ optionIndex ] as JsonObject )!
					: ( optionJson[ optionName ] as JsonObject )!;
			}
		}
	}

	static JsonNode? GetJsonValue( string value, bool parse )
	{
		var isJson = value.StartsWith( "json:" );

		var propertyValue =
			parse && int.TryParse( value, out var i ) ? i :
			parse && double.TryParse( value, out var d ) ? d :
			parse && bool.TryParse( value, out var b ) ? b :
			isJson ? JsonNode.Parse( value[ 5.. ] )! :
			value == "null" ? null : value;

		return propertyValue;
	}

	public static JsonArray Sort( this JsonArray container, Func<JsonArray, IOrderedEnumerable<JsonNode?>> action )
	{
		var sorted = action( container ).ToArray();
		container.Clear();
		return new JsonArray( sorted );
	}

	public static JsonArray ToJsonArray( this IEnumerable<JsonNode?>? items ) =>
		items != null ? new JsonArray( items.ToArray() ) : new JsonArray();

	public static JsonArray AddItems<T>( this JsonArray container, IEnumerable<T?> items, bool includeNulls = false )
	{
		if ( items != null )
		{
			foreach ( var i in items )
			{
				if ( includeNulls || i != null )
				{
					container.Add( i );
				}
			}
		}

		return container;
	}

	public static JsonNode AddOrUpdate( this JsonObject node, string propertyName, JsonNode? value )
	{
		if ( node[ propertyName ] == null )
		{
			node.Add( propertyName, value );
		}
		else
		{
			node[ propertyName ] = value;
		}
		return node[ propertyName ]!;
	}

	// Chose 'GetObject' instead of 'AsObject' to avoid conflicts with JsonNode.AsObject
	public static JsonObject GetObject( this JsonNode? node, string propertyName ) => node?[ propertyName ] == null ? new JsonObject() : ( node![ propertyName ] as JsonObject )!;
	public static JsonObject? TryGetObject( this JsonNode? node ) => node as JsonObject;
	public static JsonObject GetObject( this JsonNode? node ) => node == null ? new JsonObject() : ( node as JsonObject )!;
	// Chose 'GetArray' instead of 'ToArray' to avoid conflicts with IEnumerable<T>.ToArray
	public static JsonArray GetArray( this JsonNode? node, string propertyName ) => node?[ propertyName ] == null ? new JsonArray() : ( node![ propertyName ] as JsonArray )!;
	public static JsonArray GetArray( this JsonNode? node ) => node == null ? new JsonArray() : ( node as JsonArray )!;

	public static JsonArray CloneArray( this JsonArray? array, Func<JsonNode?, bool>? predicate = null )
	{
		if ( array == null ) return new JsonArray();

		var result = new JsonArray();
		
		foreach( var node in array.Where( n => predicate == null || predicate( n ) ) )
		{
			result.Add( node!.Clone() );
		}

		return result;
	}

	public static JsonObject AddPropertiesWithNulls( this JsonObject container, params JsonKeyProperty[] content )
	{
		foreach ( var c in content )
		{
			container[ c.Key ] = c.Value;
		}
		return container;
	}
	public static JsonObject AddProperties( this JsonObject container, params JsonKeyProperty[] content )
	{
		foreach ( var c in content )
		{
			if ( c.Value != null )
			{
				container[ c.Key ] = c.Value;
			}
		}
		return container;
	}
	public static JsonObject AddProperties( this JsonObject container, IEnumerable<KeyValuePair<string, JsonNode?>> content )
	{
		foreach ( var c in content )
		{
			if ( c.Value != null )
			{
				container[ c.Key ] = c.Value;
			}
		}
		return container;
	}
	public static JsonObject AddProperties( this JsonObject container, IEnumerable<JsonKeyProperty> content )
	{
		foreach ( var c in content )
		{
			if ( c.Value != null )
			{
				container[ c.Key ] = c.Value;
			}
		}
		return container;
	}

	// https://stackoverflow.com/a/71590703/166231 Copy/Move node extensions
	public static TNode Clone<TNode>( this TNode node ) where TNode : JsonNode => node.Deserialize<TNode>()!;

	public static JsonNode MoveNode( this JsonArray array, int id, JsonObject newParent, string name )
	{
		var node = array[ id ]!;
		array.RemoveAt( id );
		return newParent[ name ] = node;
	}

	public static JsonNode MoveNode( this JsonObject parent, string oldName, JsonObject newParent, string name )
	{
		parent.Remove( oldName, out var node );
		return newParent[ name ] = node!;
	}

	public static JsonNode? DetachNodeOrDefault( this JsonNode parent, string name )
		=> ( parent as JsonObject )?.DetachNodeOrDefault( name );

	public static JsonNode? DetachNodeOrDefault( this JsonObject parent, string name )
	{
		parent.Remove( name, out var node );
		return node;
	}
	public static TNode DetachNode<TNode>( this JsonObject parent, string name ) where TNode : JsonNode
		=> ( DetachNode( parent, name ) as TNode )!;
	
	public static JsonNode DetachNode( this JsonObject parent, string name )
	{
		return 
			parent.DetachNodeOrDefault( name ) ?? 
			throw new NullReferenceException( $"DetachNode unable to find a node named '{name}'." );
	}

	public static JsonNode? ToJsonNode( this object value )
	{
		return value switch
		{
			null => null,
			string s => (JsonNode?)s,
			double d => (JsonNode?)d,
			int i => (JsonNode?)i,
			bool b => (JsonNode?)b,
			DateTime dt => (JsonNode?)dt,
			_ => throw new ArgumentException( $"Unable to convert type '{value.GetType()}' to JsonNode" ),
		};
	}

	public static TNode ThrowOnNull<TNode>( this TNode? value ) where TNode : JsonNode => value ?? throw new JsonException( "Null JSON value" );

	public static JsonNode? SelectNode( this JsonNode context, string path )
	{
		var pathParts = path.Split( '.' );
		var current = context;
		
		foreach( var p in pathParts )
		{
			var tokenParts = p.Split( '[' );

			current = current?[ tokenParts[ 0 ] ];

			if ( current != null && tokenParts.Length == 2 )
			{
				var currentArray = ( current as JsonArray )!;
				var index = int.Parse( tokenParts[ 1 ].Replace( "]", "" ) );
				
				current = currentArray.Count > index
					? currentArray[ index ]
					: null;
			}

			if (current == null) return null;
		}

		return current;
	}
}

// Added so I didn't have to put ! after every .Key reference of System.Text.Json.JsonProperty
public record JsonKeyProperty( string Key, JsonNode? Value );