using DemoProviderMerge.Main.Cache;
using DemoProviderMerge.Main.Providers;

using TestTask;

using Route = TestTask.Route;

namespace DemoProviderMerge.Main.Services;

public class SearchService : ISearchService
{
    private readonly IProviderOneClient _providerOneClient;

    private readonly IProviderTwoClient _providerTwoClient;

    private readonly IRouteCache _routeCache;

    private readonly IEqualityComparer<Route?> _routeComparer;

    private readonly ILogger _logger;

    public SearchService(
        IProviderOneClient providerOneClient,
        IProviderTwoClient providerTwoClient,
        IRouteCache routeCache,
        IEqualityComparer<Route?> routeComparer,
        ILogger<SearchService> logger)
    {
        _providerOneClient = providerOneClient;
        _providerTwoClient = providerTwoClient;
        _routeCache = routeCache;
        _routeComparer = routeComparer;
        _logger = logger;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken token)
    {
        bool isAvailable = false;
        try
        {
#warning //TODO Assuming service is unavailable if at least one of providers is unavailable.
            IEnumerable<bool> results = await Task.WhenAll(
                _providerOneClient.IsAvailableAsync(token),
                _providerTwoClient.IsAvailableAsync(token)
            );

            isAvailable = results.All(x => x);
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());

            return false;
        }

        return isAvailable;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken token)
    {
        Route[] routes = request.Filters?.OnlyCached == true
            ? GetRoutesFromCache(request)
            : await GetRoutesFromProvidersAsync(request, token);

        SearchResponse searchResponse = CreateSearchResponse(routes);

        return searchResponse;
    }

    private SearchResponse CreateSearchResponse(Route[] routes)
    {
        if (routes.Length > 0)
        {
            return new ()
            {
                Routes = routes,
                MaxMinutesRoute = routes.Max(x => (int)(x.DestinationDateTime - x.OriginDateTime).TotalMinutes),
                MinMinutesRoute = routes.Min(x => (int)(x.DestinationDateTime - x.OriginDateTime).TotalMinutes),
                MaxPrice = routes.Max(x => x.Price),
                MinPrice = routes.Min(x => x.Price)
            };
        }

#warning //TODO Maybe throw exception.
        return new() { Routes = routes };
    }

    private async Task<Route[]> GetRoutesFromProvidersAsync(SearchRequest request, CancellationToken token)
    {
        IReadOnlyCollection<Route>[] routesFromProviders = await Task.WhenAll(
            GetRoutesFromProviderOneAsync(request, token),
            GetRoutesFromProviderTwoAsync(request, token)
        );

        Route[] routes = routesFromProviders
            .SelectMany(x => x)
            .Distinct(_routeComparer)
            .Select(x => _routeCache.GetOrAdd(x))
#warning //TODO Maybe > instead of >=.
#warning //TODO Assuming TimeLimit is in same timezone.
            .Where(x => x.TimeLimit >= DateTime.Now)
            .OrderBy(x => x.Price)
            .ThenByDescending(x => x.DestinationDateTime - x.OriginDateTime)
            .ToArray();

        return routes;
    }

    private async Task<IReadOnlyCollection<Route>> GetRoutesFromProviderOneAsync(SearchRequest request, CancellationToken token)
    {
        ProviderOneSearchRequest providerSearchRequest = new()
        {
            From = request.Origin,
            To = request.Destination,
            DateFrom = request.OriginDateTime,
            DateTo = request.Filters?.DestinationDateTime,
            MaxPrice = request.Filters?.MaxPrice
        };

        ProviderOneSearchResponse providerSearchResponse =
            await _providerOneClient.SearchAsync(providerSearchRequest, token);

        IEnumerable<ProviderOneRoute> routes = providerSearchResponse.Routes;
        if (request.Filters?.MinTimeLimit != null)
        {
            DateTime minTimeLimit = (DateTime)request.Filters.MinTimeLimit;

#warning //TODO Maybe > instead of >=.
            routes = routes.Where(x => x.TimeLimit >= minTimeLimit);
        }

        Route[] result = routes.Select(
                x => new Route
                {
                    Id = Guid.NewGuid(),
                    Origin = x.From,
                    Destination = x.To,
                    OriginDateTime = x.DateFrom,
                    DestinationDateTime = x.DateTo,
                    Price = x.Price,
                    TimeLimit = x.TimeLimit
                }
            )
            .ToArray();

        return result;
    }

    private async Task<IReadOnlyCollection<Route>> GetRoutesFromProviderTwoAsync(SearchRequest request, CancellationToken token)
    {
        ProviderTwoSearchRequest providerSearchRequest = new()
        {
            Departure = request.Origin,
            Arrival = request.Destination,
            DepartureDate = request.OriginDateTime,
            MinTimeLimit = request.Filters?.MinTimeLimit
        };

        ProviderTwoSearchResponse providerSearchResponse =
            await _providerTwoClient.SearchAsync(providerSearchRequest, token);

        IEnumerable<ProviderTwoRoute> routes = providerSearchResponse.Routes;
        if (request.Filters?.MaxPrice != null)
        {
            decimal maxPrice = (decimal)request.Filters.MaxPrice;

#warning //TODO Maybe < instead of <=.
            routes = routes.Where(x => x.Price <= maxPrice);
        }

#warning //TODO Maybe check departure and arrival for null.
        Route[] result = routes.Select(
                x => new Route
                {
                    Id = Guid.NewGuid(),
                    Origin = x.Departure.Point,
                    Destination = x.Arrival.Point,
                    OriginDateTime = x.Departure.Date,
                    DestinationDateTime = x.Arrival.Date,
                    Price = x.Price,
                    TimeLimit = x.TimeLimit
                }
            )
            .OrderBy(x => x.Price)
            .ThenByDescending(x => x.DestinationDateTime - x.OriginDateTime)
            .ToArray();

        return result;
    }

    private Route[] GetRoutesFromCache(SearchRequest request) =>
        _routeCache.FindCachedRoutes(request);
}