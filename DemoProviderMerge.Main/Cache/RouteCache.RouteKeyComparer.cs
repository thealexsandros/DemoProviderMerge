namespace DemoProviderMerge.Main.Cache;

public partial class RouteCache
{
    private class RouteKeyComparer : IEqualityComparer<RouteKey?>
    {
        static public IEqualityComparer<RouteKey?> Instance { get; } = new RouteKeyComparer();

        private RouteKeyComparer()
        {
        }

        public bool Equals(RouteKey? x, RouteKey? y)
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
                string.Equals(x.Origin, y.Origin, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Destination, y.Destination, StringComparison.OrdinalIgnoreCase) &&
                x.DepartureDate == y.DepartureDate;
        }

        public int GetHashCode(RouteKey obj)
        {
            return HashCode.Combine(obj.Origin, obj.Destination, obj.DepartureDate);
        }
    }
}