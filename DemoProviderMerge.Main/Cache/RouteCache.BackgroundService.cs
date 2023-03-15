using System.Collections.Concurrent;

using Route = TestTask.Route;

namespace DemoProviderMerge.Main.Cache;

public partial class RouteCache
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = TimeSpan.FromMilliseconds(_cacheOptions.Value.ClearObsoletePricesIntervalMilliseconds);

        using (PeriodicTimer timer = new (interval))
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                ExecuteCore();
            }
        }
    }

    private void ExecuteCore()
    {
        IReadOnlyCollection<Route> keysToRemove = _routeByDataIndex.Keys
            .Where(x => x.TimeLimit < DateTime.Now)
            .ToArray();

        foreach (Route route in keysToRemove)
        {
            _routeByDataIndex.TryRemove(route, out _);

            RouteKey routeKey = new()
            {
                Origin = route.Origin,
                Destination = route.Destination,
                DepartureDate = route.OriginDateTime.Date
            };

            if (_routeKeyToRoutesIndex.TryGetValue(routeKey, out ConcurrentDictionary<Route, Route>? routesByKey))
            {
                routesByKey.TryRemove(route, out _);
            }
        }
    }
}