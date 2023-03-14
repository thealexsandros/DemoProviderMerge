using TestTask;

namespace DemoProviderMerge.Main.Providers;

public interface IProviderOneClient
{
    Task<ProviderOneSearchResponse> SearchAsync(
        ProviderOneSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}