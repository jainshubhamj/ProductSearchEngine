using ProductSearchEngine.Api.Models;

namespace ProductSearchEngine.Api.Services
{
    public interface ISearchService
    {
        Task<SearchResponse> SearchProductsAsync(SearchRequest request);
        Task<SuggestionResponse> GetSuggestionsAsync(SuggestionRequest request);
    }
}