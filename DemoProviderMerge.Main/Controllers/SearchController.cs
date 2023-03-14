using Microsoft.AspNetCore.Mvc;

using TestTask;

namespace DemoProviderMerge.Main.Controllers;

[ApiController]
[Route("api/v1/search")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpPost]
    public async Task<IActionResult> SearchAsync([FromBody] SearchRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        SearchResponse response = await _searchService.SearchAsync(request, cancellationToken);

        return Ok(response);
    }

    [HttpGet("api/v1/ping")]
    public async Task<IActionResult> IsAvailableAsync(CancellationToken cancellationToken)
    {
        bool isAvailable = await _searchService.IsAvailableAsync(cancellationToken);

        return Ok(isAvailable);
    }
}