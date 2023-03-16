using System.Collections.Concurrent;

using DemoProviderMerge.Main.RouteComparers;

using Microsoft.Extensions.Options;

using TestTask;

using Route = TestTask.Route;

namespace DemoProviderMerge.Main.Cache;

public partial class RouteCache : BackgroundService, IRouteCache
{
    private readonly IOptions<CacheOptions> _cacheOptions;

    private record RouteKey
    {
        public string Origin { get; init; }

        public string Destination { get; init; }

        public DateTime DepartureDate { get; init; }
    }

    /// <remarks>
    /// Assuming that number of routes for RouteKey is in range 1..100
    /// (in real-life transportation the number of routes from A to B starting at same date is more or less in the range mentioned above).
    /// 
    /// ConcurrentDictionary is used for concurrent indexed access.
    /// 
    /// Route as key and value are used to maintain same guid for multiple route addition attempts with GetOrAdd.
    /// </remarks>
    private readonly ConcurrentDictionary<RouteKey, ConcurrentDictionary<Route, Route>> _routeKeyToRoutesIndex =
        new (new RouteKeyComparer());

    public RouteCache(IOptions<CacheOptions> cacheOptions)
    {
        _cacheOptions = cacheOptions;
    }

    public Route GetOrAdd(Route route)
    {
        RouteKey routeKey = new ()
        {
            Origin = route.Origin,
            Destination = route.Destination,
            DepartureDate = route.OriginDateTime.Date
        };

        ConcurrentDictionary<Route, Route> routes =
            _routeKeyToRoutesIndex.GetOrAdd(routeKey, new ConcurrentDictionary<Route, Route>(new RouteIgnoreIdComparer()));

        Route result = routes.GetOrAdd(route, route);

        return result;
    }

    public Route[] FindCachedRoutes(SearchRequest request)
    {
        RouteKey searchKey = new ()
        {
            Origin = request.Origin,
            Destination = request.Destination,
            DepartureDate = request.OriginDateTime
        };

        bool gotRoutes = _routeKeyToRoutesIndex.TryGetValue(searchKey, out ConcurrentDictionary<Route, Route>? searchKeyRoutes);
        if (gotRoutes)
        {
            IEnumerable<Route> routes = searchKeyRoutes.Keys;
            if (request.Filters != null)
            {
#warning //TODO Assuming origin date in parameters is specified without time.
                if (request.Filters.DestinationDateTime != null)
                {
                    routes = routes.Where(x => x.DestinationDateTime.Date == request.Filters.DestinationDateTime);
                }

                if (request.Filters.MaxPrice != null)
                {
#warning //TODO Maybe < instead of <=.
                    routes = routes.Where(x => x.Price <= request.Filters.MaxPrice);
                }

                if (request.Filters.MinTimeLimit != null)
                {
#warning //TODO Maybe > instead of >=.
                    routes = routes.Where(x => x.TimeLimit >= request.Filters.MinTimeLimit);
                }
            }

            Route[] routesArray = routes
                .OrderBy(x => x.OriginDateTime)
                .ThenBy(x => x.DestinationDateTime)
                .ThenBy(x => x.Price)
                .ThenBy(x => x.TimeLimit)
                .ToArray();

            return routesArray;
        }

        return Array.Empty<Route>();
    }
}
