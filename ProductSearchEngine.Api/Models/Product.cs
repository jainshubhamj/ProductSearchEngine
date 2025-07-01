using OpenSearch.Client;

namespace ProductSearchEngine.Api.Models
{
    [OpenSearchType(IdProperty = nameof(Id))]
    public class Product
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Text(Analyzer = "standard")]
        public string Title { get; set; } = string.Empty;
        
        [Text(Analyzer = "standard")]
        public string Description { get; set; } = string.Empty;
        
        [Keyword]
        public string Category { get; set; } = string.Empty;
        
        [Number]
        public decimal Price { get; set; }
        
        [Keyword]
        public string Brand { get; set; } = string.Empty;
        
        [Keyword]
        public string Sku { get; set; } = string.Empty;
        
        [Object]
        public Dictionary<string, string> Attributes { get; set; } = new();
        
        [Completion]
        public CompletionField Suggest { get; set; } = new();
        
        [Date]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Future AI field - vector embeddings
        [DenseVector(Dimensions = 768)]
        public float[]? EmbeddingVector { get; set; }
        
        [Keyword]
        public List<string> Tags { get; set; } = new();
    }
}