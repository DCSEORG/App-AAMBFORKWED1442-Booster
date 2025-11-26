using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NorthwindApp.Models;
using NorthwindApp.Services;

namespace NorthwindApp.Pages;

public class OrdersModel : PageModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<OrdersModel> _logger;

    public OrdersModel(IDatabaseService databaseService, ILogger<OrdersModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public List<Order> Orders { get; set; } = new();
    public ErrorInfo? Error { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? CustomerId { get; set; }

    public async Task OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(CustomerId))
        {
            var (orders, error) = await _databaseService.GetOrdersByCustomerAsync(CustomerId);
            Orders = orders;
            Error = error;
        }
        else
        {
            var (orders, error) = await _databaseService.GetAllOrdersAsync();
            Orders = orders;
            Error = error;
        }
    }
}
