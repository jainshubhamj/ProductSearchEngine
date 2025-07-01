using OpenSearch.Client;
using ProductSearchEngine.Api.Models;

namespace ProductSearchEngine.Api.Services
{
    public class ProductService : IProductService
    {
        private readonly IOpenSearchClient _client;
        private readonly ILogger<ProductService> _logger;
        private const string IndexName = "products";

        public ProductService(IOpenSearchClient client, ILogger<ProductService> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<bool> IndexProductAsync(Product product)
        {
            // Prepare suggestion field
            product.Suggest = new CompletionField
            {
                Input = new[] { product.Title, product.Brand, product.Category }
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray(),
                Contexts = new Dictionary<string, IEnumerable<string>>
                {
                    ["category"] = new[] { product.Category }
                }
            };

            var response = await _client.IndexAsync(product, i => i
                .Index(IndexName)
                .Id(product.Id)
                .Refresh(Refresh.WaitFor)
            );

            if (!response.IsValid)
            {
                _logger.LogError("Failed to index product {ProductId}: {Error}", product.Id, response.DebugInformation);
                return false;
            }

            return true;
        }

        public async Task<bool> IndexProductsAsync(IEnumerable<Product> products)
        {
            var productList = products.ToList();
            
            // Prepare suggestion fields for all products
            foreach (var product in productList)
            {
                product.Suggest = new CompletionField
                {
                    Input = new[] { product.Title, product.Brand, product.Category }
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToArray(),
                    Contexts = new Dictionary<string, IEnumerable<string>>
                    {
                        ["category"] = new[] { product.Category }
                    }
                };
            }

            var bulkResponse = await _client.BulkAsync(b => b
                .Index(IndexName)
                .IndexMany(productList)
                .Refresh(Refresh.WaitFor)
            );

            if (!bulkResponse.IsValid)
            {
                _logger.LogError("Bulk indexing failed: {Error}", bulkResponse.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully indexed {Count} products", productList.Count);
            return true;
        }

        public async Task<Product?> GetProductAsync(string id)
        {
            var response = await _client.GetAsync<Product>(id, g => g.Index(IndexName));
            
            return response.IsValid ? response.Source : null;
        }

        public async Task<bool> DeleteProductAsync(string id)
        {
            var response = await _client.DeleteAsync<Product>(id, d => d.Index(IndexName));
            
            return response.IsValid;
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            return await IndexProductAsync(product);
        }
    }
}