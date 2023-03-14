using System.ComponentModel.DataAnnotations;

namespace DemoProviderMerge.Main.Providers;

public class ProviderOneOptions
{
    [Required]
    [MinLength(1)]
    [Url]
    public string Uri { get; init; }
}