using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NorthwindApp.Models;
using NorthwindApp.Services;

namespace NorthwindApp.Pages.Products;

public class CreateModel : PageModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IDatabaseService databaseService, ILogger<CreateModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    [BindProperty]
    public Product Product { get; set; } = new();

    public SelectList? Categories { get; set; }
    public SelectList? Suppliers { get; set; }

    public async Task OnGetAsync()
    {
        await LoadSelectLists();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectLists();
            return Page();
        }

        var (productId, error) = await _databaseService.CreateProductAsync(Product);

        if (error != null)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            await LoadSelectLists();
            return Page();
        }

        return RedirectToPage("/Products/Details", new { id = productId });
    }

    private async Task LoadSelectLists()
    {
        var (categories, _) = await _databaseService.GetAllCategoriesAsync();
        Categories = new SelectList(categories, "CategoryID", "CategoryName");

        var (suppliers, _) = await _databaseService.GetAllSuppliersAsync();
        Suppliers = new SelectList(suppliers, "SupplierID", "CompanyName");
    }
}
