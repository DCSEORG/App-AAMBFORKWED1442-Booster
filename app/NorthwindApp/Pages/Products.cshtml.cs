using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NorthwindApp.Models;
using NorthwindApp.Services;

namespace NorthwindApp.Pages;

public class ProductsModel : PageModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<ProductsModel> _logger;

    public ProductsModel(IDatabaseService databaseService, ILogger<ProductsModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public ErrorInfo? Error { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public short? SelectedCategoryId { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public decimal? MinPrice { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public decimal? MaxPrice { get; set; }

    public async Task OnGetAsync()
    {
        // Get categories for filter dropdown
        var (categories, _) = await _databaseService.GetAllCategoriesAsync();
        Categories = categories;

        // Apply filters
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var (products, error) = await _databaseService.SearchProductsAsync(SearchTerm);
            Products = products;
            Error = error;
        }
        else if (SelectedCategoryId.HasValue)
        {
            var (products, error) = await _databaseService.GetProductsByCategoryAsync(SelectedCategoryId.Value);
            Products = products;
            Error = error;
        }
        else if (MinPrice.HasValue && MaxPrice.HasValue)
        {
            var (products, error) = await _databaseService.GetProductsByPriceRangeAsync(MinPrice.Value, MaxPrice.Value);
            Products = products;
            Error = error;
        }
        else
        {
            var (products, error) = await _databaseService.GetAllProductsAsync();
            Products = products;
            Error = error;
        }
    }
}
