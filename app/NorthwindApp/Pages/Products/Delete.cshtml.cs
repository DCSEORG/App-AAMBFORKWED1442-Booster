using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NorthwindApp.Models;
using NorthwindApp.Services;

namespace NorthwindApp.Pages.Products;

public class DeleteModel : PageModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(IDatabaseService databaseService, ILogger<DeleteModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public Product? Product { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var (product, _) = await _databaseService.GetProductByIdAsync((short)id);

        if (product == null)
        {
            return NotFound();
        }

        Product = product;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var (success, error) = await _databaseService.DeleteProductAsync((short)id);

        if (error != null)
        {
            var (product, _) = await _databaseService.GetProductByIdAsync((short)id);
            Product = product;
            ModelState.AddModelError(string.Empty, error.Message);
            return Page();
        }

        return RedirectToPage("/Products");
    }
}
