using Microsoft.AspNetCore.Mvc.RazorPages;
using NorthwindApp.Models;
using NorthwindApp.Services;

namespace NorthwindApp.Pages;

public class IndexModel : PageModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IDatabaseService databaseService, ILogger<IndexModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public DashboardStats? Stats { get; set; }
    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public ErrorInfo? Error { get; set; }

    public async Task OnGetAsync()
    {
        var (stats, statsError) = await _databaseService.GetDashboardStatsAsync();
        Stats = stats;
        
        var (products, productsError) = await _databaseService.GetAllProductsAsync();
        Products = products;
        
        var (categories, categoriesError) = await _databaseService.GetAllCategoriesAsync();
        Categories = categories;
        
        // Show error if any operation failed
        Error = statsError ?? productsError ?? categoriesError;
    }
}
