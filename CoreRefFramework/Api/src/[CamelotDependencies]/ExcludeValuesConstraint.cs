using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KAT.Camelot.Domain.Web.Http;

public class ExcludeValuesConstraint : IRouteConstraint
{
    private readonly string[] validOptions;
    public ExcludeValuesConstraint( string options )
    {
        validOptions = options.Split( '|' );        
    }

	public bool Match( HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection )
    {
        return !validOptions.Contains(values[routeKey]);
    }
}