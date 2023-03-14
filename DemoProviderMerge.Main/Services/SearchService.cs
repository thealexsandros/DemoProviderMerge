using System.Text;

using DemoProviderMerge.Main.Cache;
using DemoProviderMerge.Main.Providers;
using DemoProviderMerge.Main.RouteComparers;
using DemoProviderMerge.Main.Validation;

using TestTask;

using Route = TestTask.Route;

namespace DemoProviderMerge.Main.Services;

public class SearchService : ISearchService
{
    private readonly IProviderOneClient _providerOneClient;

    private readonly IProviderTwoClient _providerTwoClient;

    private readonly IRouteCache _routeCache;

    public SearchService(
        IProviderOneClient providerOneClient,
        IProviderTwoClient providerTwoClient,
        IRouteCache routeCache)
    {
        _providerOneClient = providerOneClient;
        _providerTwoClient = providerTwoClient;
        _routeCache = routeCache;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken token)
    {
#warning //TODO Assuming service is unavailable if at least one of providers is unavailable.
        IEnumerable<bool> results = await Task.WhenAll(
            _providerOneClient.IsAvailableAsync(token),
            _providerTwoClient.IsAvailableAsync(token)
        );

        return results.All(x => x);
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken token)
    {
        if (await IsAvailableAsync(token))
        {
            ValidationResult validationResult = ValidateSearchRequest(request);
            if (validationResult.IsValid)
            {
                Route[] routes = request.Filters?.OnlyCached == true
                    ? GetRoutesFromCache(request)
                    : await GetRoutesFromProvidersAsync(request, token);

                SearchResponse searchResponse = CreateSearchResponse(routes);

                return searchResponse;
            }

            throw new Exception($"Request is invalid: {validationResult.ErrorMessage}");
        }

        throw new Exception("Search service is unavailable");
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
            .Distinct(RouteIgnoreIdComparer.Instance)
            .Select(x => _routeCache.GetOrAdd(x))
#warning //TODO Maybe > instead of >=.
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

    private ValidationResult ValidateSearchRequest(SearchRequest? request)
    {
#warning //TODO validation with attributes
        StringBuilder errorBuilder = new();

        if (request == null)
        {
            errorBuilder.AppendLine($"{nameof(SearchRequest)} should be not null.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Origin))
            {
                errorBuilder.AppendLine($"{nameof(request.Origin)} should not be empty.");
            }

            if (string.IsNullOrWhiteSpace(request.Destination))
            {
                errorBuilder.AppendLine($"{nameof(request.Destination)} should not be empty.");
            }

#warning //TODO Assuming origin date in parameters is specified without time.
            request.OriginDateTime.ValidateDate(nameof(request.OriginDateTime), errorBuilder);

            if (request.Filters != null)
            {
                ValidateSearchRequestFilters(request.Filters, errorBuilder, nameof(request.Filters));
            }
        }

        if (errorBuilder.Length > 0)
        {
            return new()
            {
                IsValid = false,
                ErrorMessage = errorBuilder.ToString()
            };
        }

        return new() { IsValid = true };
    }

    private void ValidateSearchRequestFilters(SearchFilters searchFilters, StringBuilder errorBuilder, string propertyNamePrefix)
    {
        if (searchFilters.MaxPrice < 0m)
        {
            errorBuilder.AppendLine($"{propertyNamePrefix}.{nameof(searchFilters.MaxPrice)} should be greater than or equal to zero.");
        }

        searchFilters.MinTimeLimit.ValidateNullableDateTime($"{propertyNamePrefix}.{nameof(searchFilters.MinTimeLimit)}", errorBuilder);

#warning //TODO Assuming destination date is specified without time.
        searchFilters.DestinationDateTime.ValidateNullableDate($"{propertyNamePrefix}.{nameof(searchFilters.DestinationDateTime)}", errorBuilder);
    }
}