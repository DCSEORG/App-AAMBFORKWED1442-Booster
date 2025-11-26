using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NorthwindApp.Models;
using NorthwindApp.Services;

namespace NorthwindApp.Pages.Products;

public class EditModel : PageModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IDatabaseService databaseService, ILogger<EditModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    [BindProperty]
    public Product Product { get; set; } = new();

    public SelectList? Categories { get; set; }
    public SelectList? Suppliers { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var (product, error) = await _databaseService.GetProductByIdAsync((short)id);

        if (product == null)
        {
            return NotFound();
        }

        Product = product;
        await LoadSelectLists();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectLists();
            return Page();
        }

        var (success, error) = await _databaseService.UpdateProductAsync(Product);

        if (error != null)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            await LoadSelectLists();
            return Page();
        }

        return RedirectToPage("/Products/Details", new { id = Product.ProductID });
    }

    private async Task LoadSelectLists()
    {
        var (categories, _) = await _databaseService.GetAllCategoriesAsync();
        Categories = new SelectList(categories, "CategoryID", "CategoryName");

        var (suppliers, _) = await _databaseService.GetAllSuppliersAsync();
        Suppliers = new SelectList(suppliers, "SupplierID", "CompanyName");
    }
}
