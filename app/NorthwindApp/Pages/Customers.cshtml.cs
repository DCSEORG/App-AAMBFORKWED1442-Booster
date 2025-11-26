using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NorthwindApp.Models;
using NorthwindApp.Services;

namespace NorthwindApp.Pages;

public class CustomersModel : PageModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<CustomersModel> _logger;

    public CustomersModel(IDatabaseService databaseService, ILogger<CustomersModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public List<Customer> Customers { get; set; } = new();
    public ErrorInfo? Error { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var (customers, error) = await _databaseService.SearchCustomersAsync(SearchTerm);
            Customers = customers;
            Error = error;
        }
        else
        {
            var (customers, error) = await _databaseService.GetAllCustomersAsync();
            Customers = customers;
            Error = error;
        }
    }
}
