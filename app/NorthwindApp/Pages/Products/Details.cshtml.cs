using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NorthwindApp.Models;
using NorthwindApp.Services;

namespace NorthwindApp.Pages.Products;

public class DetailsModel : PageModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(IDatabaseService databaseService, ILogger<DetailsModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public Product? Product { get; set; }
    public ErrorInfo? Error { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var (product, error) = await _databaseService.GetProductByIdAsync((short)id);
        Product = product;
        Error = error;

        if (Product == null && Error == null)
        {
            return NotFound();
        }

        return Page();
    }
}
