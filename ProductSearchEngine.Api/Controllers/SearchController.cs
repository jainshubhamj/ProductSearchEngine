using Microsoft.AspNetCore.Mvc;
using ProductSearchEngine.Api.Models;
using ProductSearchEngine.Api.Services;

namespace ProductSearchEngine.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly ILogger<SearchController> _logger;

        public SearchController(ISearchService searchService, ILogger<SearchController> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Search([FromBody] SearchRequest request)
        {
            var response = await _searchService.SearchProductsAsync(request);
            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> SearchGet(
            [FromQuery] string? q = "",
            [FromQuery] string? categories = null,
            [FromQuery] string? brands = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string sortBy = "relevance",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var request = new SearchRequest
            {
                Query = q ?? "",
                Categories = categories?.Split(',').ToList(),
                Brands = brands?.Split(',').ToList(),
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sortBy,
                Page = page,
                PageSize = pageSize
            };

            var response = await _searchService.SearchProductsAsync(request);
            return Ok(response);
        }

        [HttpPost("suggestions")]
        public async Task<IActionResult> GetSuggestions([FromBody] SuggestionRequest request)
        {
            var response = await _searchService.GetSuggestionsAsync(request);
            return Ok(response);
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestionsGet([FromQuery] string prefix, [FromQuery] int size = 10)
        {
            var request = new SuggestionRequest { Prefix = prefix, Size = size };
            var response = await _searchService.GetSuggestionsAsync(request);
            return Ok(response);
        }
    }
}