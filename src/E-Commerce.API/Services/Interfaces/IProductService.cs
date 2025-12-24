using E_Commerce.API.DTOs;

namespace E_Commerce.API.Services.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();
    Task<IEnumerable<ProductDto>> GetActiveProductsAsync();
    Task<ProductDto?> GetProductByIdAsync(Guid id);
    Task<ProductDto?> GetProductBySkuAsync(string sku);
    Task<ProductDto> CreateProductAsync(CreateProductDto createDto);
    Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductDto updateDto);
    Task<bool> DeleteProductAsync(Guid id);
    Task<ProductDto?> UpdateStockAsync(Guid id, int quantity);
    Task<IEnumerable<ProductDto>> GetLowStockProductsAsync(int threshold = 10);
}
