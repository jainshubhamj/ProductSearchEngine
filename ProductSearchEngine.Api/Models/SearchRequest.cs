namespace ProductSearchEngine.Api.Models
{
    public class SearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public List<string>? Categories { get; set; }
        public List<string>? Brands { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string SortBy { get; set; } = "relevance"; // relevance, price_asc, price_desc
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool IncludeFacets { get; set; } = true;
    }
    
    public class SuggestionRequest
    {
        public string Prefix { get; set; } = string.Empty;
        public int Size { get; set; } = 10;
    }
}