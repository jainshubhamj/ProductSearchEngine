namespace ProductSearchEngine.Api.Models
{
    public class SearchResponse
    {
        public List<Product> Products { get; set; } = new();
        public long TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public Dictionary<string, List<FacetItem>> Facets { get; set; } = new();
        public long ExecutionTimeMs { get; set; }
    }
    
    public class FacetItem
    {
        public string Value { get; set; } = string.Empty;
        public long Count { get; set; }
    }
    
    public class SuggestionResponse
    {
        public List<string> Suggestions { get; set; } = new();
    }
}