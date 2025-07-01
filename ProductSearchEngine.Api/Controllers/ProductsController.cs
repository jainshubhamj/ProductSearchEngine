using Microsoft.AspNetCore.Mvc;
using ProductSearchEngine.Api.Models;
using ProductSearchEngine.Api.Services;

namespace ProductSearchEngine.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] Product product)
        {
            if (string.IsNullOrWhiteSpace(product.Title))
            {
                return BadRequest("Product title is required");
            }

            var success = await _productService.IndexProductAsync(product);
            
            if (!success)
            {
                return StatusCode(500, "Failed to create product");
            }

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> CreateProducts([FromBody] List<Product> products)
        {
            if (!products.Any())
            {
                return BadRequest("No products provided");
            }

            var success = await _productService.IndexProductsAsync(products);
            
            if (!success)
            {
                return StatusCode(500, "Failed to create products");
            }

            return Ok(new { Message = $"Successfully indexed {products.Count} products" });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(string id)
        {
            var product = await _productService.GetProductAsync(id);
            
            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(string id, [FromBody] Product product)
        {
            product.Id = id;
            var success = await _productService.UpdateProductAsync(product);
            
            if (!success)
            {
                return StatusCode(500, "Failed to update product");
            }

            return Ok(product);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            var success = await _productService.DeleteProductAsync(id);
            
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}