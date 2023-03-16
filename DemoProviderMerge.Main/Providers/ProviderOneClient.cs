using System.Text.Json;
using System.Text;

using Microsoft.Extensions.Options;

using TestTask;

namespace DemoProviderMerge.Main.Providers;

public class ProviderOneClient : IProviderOneClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly IOptions<ProviderOneOptions> _options;

    public ProviderOneClient(IHttpClientFactory httpClientFactory, IOptions<ProviderOneOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public async Task<ProviderOneSearchResponse> SearchAsync(
        ProviderOneSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        Uri baseUri = new (_options.Value.Uri);
        Uri searchUri = new (baseUri, "api/v1/search");

        StringContent requestContent = new (JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        HttpRequestMessage requestMessage = new (HttpMethod.Post, searchUri) { Content = requestContent };

        HttpResponseMessage responseMessage;
        using (HttpClient httpClient = _httpClientFactory.CreateClient())
        {
            responseMessage = await httpClient.SendAsync(requestMessage, cancellationToken);
        }

        responseMessage.EnsureSuccessStatusCode();

        Stream responseContent = await responseMessage.Content.ReadAsStreamAsync(cancellationToken);

        ProviderOneSearchResponse response =
            await JsonSerializer.DeserializeAsync<ProviderOneSearchResponse>(responseContent, cancellationToken: cancellationToken);

        return response;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        Uri baseUri = new (_options.Value.Uri);
        Uri pingUri = new (baseUri, "api/v1/ping");

        HttpRequestMessage requestMessage = new (HttpMethod.Get, pingUri);

        HttpResponseMessage responseMessage;
        using (HttpClient httpClient = _httpClientFactory.CreateClient())
        {
            responseMessage = await httpClient.SendAsync(requestMessage, cancellationToken);
        }

        return responseMessage.IsSuccessStatusCode;
    }
}