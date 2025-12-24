using E_Commerce.API.DTOs;
using E_Commerce.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.API.Controllers;

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

    /// <summary>
    /// Get all products
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProducts([FromQuery] bool activeOnly = false)
    {
        _logger.LogInformation("GET /api/products - activeOnly: {ActiveOnly}", activeOnly);

        var products = activeOnly
            ? await _productService.GetActiveProductsAsync()
            : await _productService.GetAllProductsAsync();

        return Ok(products);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetProductById(Guid id)
    {
        _logger.LogInformation("GET /api/products/{Id}", id);

        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
        {
            _logger.LogWarning("Product not found: {Id}", id);
            return NotFound(new { message = $"Product with ID {id} not found" });
        }

        return Ok(product);
    }

    /// <summary>
    /// Get product by SKU
    /// </summary>
    [HttpGet("sku/{sku}")]
    public async Task<ActionResult<ProductDto>> GetProductBySku(string sku)
    {
        _logger.LogInformation("GET /api/products/sku/{SKU}", sku);

        var product = await _productService.GetProductBySkuAsync(sku);
        if (product == null)
        {
            _logger.LogWarning("Product not found with SKU: {SKU}", sku);
            return NotFound(new { message = $"Product with SKU '{sku}' not found" });
        }

        return Ok(product);
    }

    /// <summary>
    /// Get low stock products
    /// </summary>
    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetLowStockProducts([FromQuery] int threshold = 10)
    {
        _logger.LogInformation("GET /api/products/low-stock - threshold: {Threshold}", threshold);

        var products = await _productService.GetLowStockProductsAsync(threshold);
        return Ok(products);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto createDto)
    {
        _logger.LogInformation("POST /api/products - SKU: {SKU}", createDto.SKU);

        try
        {
            var product = await _productService.CreateProductAsync(createDto);
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, [FromBody] UpdateProductDto updateDto)
    {
        _logger.LogInformation("PUT /api/products/{Id}", id);

        var product = await _productService.UpdateProductAsync(id, updateDto);
        if (product == null)
        {
            _logger.LogWarning("Product not found: {Id}", id);
            return NotFound(new { message = $"Product with ID {id} not found" });
        }

        return Ok(product);
    }

    /// <summary>
    /// Delete a product (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteProduct(Guid id)
    {
        _logger.LogInformation("DELETE /api/products/{Id}", id);

        var deleted = await _productService.DeleteProductAsync(id);
        if (!deleted)
        {
            _logger.LogWarning("Product not found: {Id}", id);
            return NotFound(new { message = $"Product with ID {id} not found" });
        }

        return NoContent();
    }

    /// <summary>
    /// Update product stock
    /// </summary>
    [HttpPatch("{id:guid}/stock")]
    public async Task<ActionResult<ProductDto>> UpdateStock(Guid id, [FromBody] UpdateStockDto stockDto)
    {
        _logger.LogInformation("PATCH /api/products/{Id}/stock - Quantity: {Quantity}", id, stockDto.Quantity);

        try
        {
            var product = await _productService.UpdateStockAsync(id, stockDto.Quantity);
            if (product == null)
            {
                _logger.LogWarning("Product not found: {Id}", id);
                return NotFound(new { message = $"Product with ID {id} not found" });
            }

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock");
            return BadRequest(new { message = ex.Message });
        }
    }
}
