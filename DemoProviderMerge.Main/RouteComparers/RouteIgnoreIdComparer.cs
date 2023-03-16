using Route = TestTask.Route;

namespace DemoProviderMerge.Main.RouteComparers;

internal class RouteIgnoreIdComparer : IEqualityComparer<Route?>
{
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

        bool equals =
            string.Equals(x.Origin, y.Origin, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Destination, y.Destination, StringComparison.OrdinalIgnoreCase) &&
            x.OriginDateTime == y.OriginDateTime &&
            x.DestinationDateTime == y.DestinationDateTime &&
            x.Price == y.Price &&
            x.TimeLimit == y.TimeLimit;

        return equals;
    }

    public int GetHashCode(Route obj)
    {
        HashCode hashCode = new ();

        hashCode.Add(obj.Origin, StringComparer.OrdinalIgnoreCase);
        hashCode.Add(obj.Destination, StringComparer.OrdinalIgnoreCase);
        hashCode.Add(obj.OriginDateTime);
        hashCode.Add(obj.DestinationDateTime);
        hashCode.Add(obj.Price);
        hashCode.Add(obj.TimeLimit);

        return hashCode.ToHashCode();
    }
}