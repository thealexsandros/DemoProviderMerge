using Route = TestTask.Route;

namespace DemoProviderMerge.Main.RouteComparers;

internal class RouteByIdComparer : IEqualityComparer<Route?>
{
    static public IEqualityComparer<Route?> Instance { get; } = new RouteByIdComparer();

    private RouteByIdComparer()
    {
    }

    public bool Equals(Route? x, Route? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (ReferenceEquals(x, null))
        {
            return false;
        }

        if (ReferenceEquals(y, null))
        {
            return false;
        }

        if (x.GetType() != y.GetType())
        {
            return false;
        }

        return
            x.Id.Equals(y.Id);
    }

    public int GetHashCode(Route obj)
    {
        return obj.Id.GetHashCode();
    }
}