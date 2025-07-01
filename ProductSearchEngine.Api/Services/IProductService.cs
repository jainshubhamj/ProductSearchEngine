using ProductSearchEngine.Api.Models;

namespace ProductSearchEngine.Api.Services
{
    public interface IProductService
    {
        Task<bool> IndexProductAsync(Product product);
        Task<bool> IndexProductsAsync(IEnumerable<Product> products);
        Task<Product?> GetProductAsync(string id);
        Task<bool> DeleteProductAsync(string id);
        Task<bool> UpdateProductAsync(Product product);
    }
}