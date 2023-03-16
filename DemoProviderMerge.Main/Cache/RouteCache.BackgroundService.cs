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
                RemoveExpiredRoutes();
            }
        }
    }

    private void RemoveExpiredRoutes()
    {
        foreach (RouteKey routeKey in _routeKeyToRoutesIndex.Keys.ToArray())
        {
            if (_routeKeyToRoutesIndex.TryGetValue(routeKey, out ConcurrentDictionary<Route, Route>? routesByKey))
            {
                IReadOnlyCollection<Route> keysToRemove = routesByKey.Keys
#warning //TODO Assuming TimeLimit is in same timezone.
                    .Where(x => x.TimeLimit < DateTime.Now)
                    .ToArray();

                foreach (Route key in keysToRemove)
                {
                    routesByKey.TryRemove(key, out _);
                }
            }
        }
    }
}