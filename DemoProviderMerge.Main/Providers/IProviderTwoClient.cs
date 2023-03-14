using TestTask;

namespace DemoProviderMerge.Main.Providers;

public interface IProviderTwoClient
{
    Task<ProviderTwoSearchResponse> SearchAsync(
        ProviderTwoSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}