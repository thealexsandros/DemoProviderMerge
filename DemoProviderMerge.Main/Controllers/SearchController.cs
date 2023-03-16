using System.Text;

using Microsoft.AspNetCore.Mvc;

using DemoProviderMerge.Main.Validation;

using TestTask;

namespace DemoProviderMerge.Main.Controllers;

[ApiController]
[Route("api/v1/search")]
public class SearchController : ControllerBase 
{
    private readonly ISearchService _searchService;

    private readonly ILogger _logger;

    public SearchController(ISearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SearchAsync([FromBody] SearchRequest request, CancellationToken cancellationToken)
    {
        if (await _searchService.IsAvailableAsync(cancellationToken))
        {
            ValidationResult validationResult = ValidateSearchRequest(request);
            if (validationResult.IsValid)
            {
                try
                {
                    SearchResponse response = await _searchService.SearchAsync(request, cancellationToken);

                    return Ok(response);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString());

                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }

            return BadRequest(validationResult.ErrorMessage);
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable);

    }

    [HttpGet("api/v1/ping")]
    public async Task<IActionResult> IsAvailableAsync(CancellationToken cancellationToken)
    {
        bool isAvailable = await _searchService.IsAvailableAsync(cancellationToken);

        if (isAvailable)
        {
            return Ok();
        }
        else
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private ValidationResult ValidateSearchRequest(SearchRequest? request)
    {
#warning //TODO validation with attributes
        StringBuilder errorBuilder = new ();

        if (request == null)
        {
            errorBuilder.AppendLine($"{nameof(SearchRequest)} should be not null.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Origin))
            {
                errorBuilder.AppendLine($"{nameof(request.Origin)} should not be empty.");
            }

            if (string.IsNullOrWhiteSpace(request.Destination))
            {
                errorBuilder.AppendLine($"{nameof(request.Destination)} should not be empty.");
            }

#warning //TODO Assuming origin date in parameters is specified without time.
            request.OriginDateTime.ValidateDate(nameof(request.OriginDateTime), errorBuilder);

            if (request.Filters != null)
            {
                ValidateSearchRequestFilters(request.Filters, errorBuilder, nameof(request.Filters));
            }
        }

        if (errorBuilder.Length > 0)
        {
            return new()
            {
                IsValid = false,
                ErrorMessage = errorBuilder.ToString()
            };
        }

        return new() { IsValid = true };
    }

    private void ValidateSearchRequestFilters(SearchFilters searchFilters, StringBuilder errorBuilder, string propertyNamePrefix)
    {
        if (searchFilters.MaxPrice < 0m)
        {
            errorBuilder.AppendLine($"{propertyNamePrefix}.{nameof(searchFilters.MaxPrice)} should be greater than or equal to zero.");
        }

        searchFilters.MinTimeLimit.ValidateNullableDateTime($"{propertyNamePrefix}.{nameof(searchFilters.MinTimeLimit)}", errorBuilder);

#warning //TODO Assuming destination date is specified without time.
        searchFilters.DestinationDateTime.ValidateNullableDate($"{propertyNamePrefix}.{nameof(searchFilters.DestinationDateTime)}", errorBuilder);
    }
}