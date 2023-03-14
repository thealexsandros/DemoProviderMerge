using System.ComponentModel.DataAnnotations;

namespace DemoProviderMerge.Main.Cache;

public class CacheOptions
{
    [Range(100, int.MaxValue)]
    public int ClearObsoletePricesIntervalMilliseconds { get; init; }
}