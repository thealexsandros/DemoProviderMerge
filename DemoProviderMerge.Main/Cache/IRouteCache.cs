using TestTask;

using Route = TestTask.Route;

namespace DemoProviderMerge.Main.Cache;

public interface IRouteCache
{
    Route GetOrAdd(Route route);

    Route[] FindCachedRoutes(SearchRequest request);
}