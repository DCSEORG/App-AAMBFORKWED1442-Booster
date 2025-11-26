using Microsoft.AspNetCore.Mvc;
using NorthwindApp.Models;
using NorthwindApp.Services;

namespace NorthwindApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IDatabaseService _databaseService;

    public ProductsController(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    /// <summary>
    /// Get all products
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        var (products, error) = await _databaseService.GetAllProductsAsync();
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error.Message);
        }
        return Ok(products);
    }

    /// <summary>
    /// Get a product by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(short id)
    {
        var (product, error) = await _databaseService.GetProductByIdAsync(id);
        if (error != null)
        {
            return NotFound(new { message = error.Message });
        }
        if (product == null)
        {
            return NotFound(new { message = "Product not found" });
        }
        return Ok(product);
    }

    /// <summary>
    /// Get products by category
    /// </summary>
    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<IEnumerable<Product>>> GetByCategory(short categoryId)
    {
        var (products, error) = await _databaseService.GetProductsByCategoryAsync(categoryId);
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error.Message);
        }
        return Ok(products);
    }

    /// <summary>
    /// Get products by price range
    /// </summary>
    [HttpGet("pricerange")]
    public async Task<ActionResult<IEnumerable<Product>>> GetByPriceRange([FromQuery] decimal minPrice, [FromQuery] decimal maxPrice)
    {
        var (products, error) = await _databaseService.GetProductsByPriceRangeAsync(minPrice, maxPrice);
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error.Message);
        }
        return Ok(products);
    }

    /// <summary>
    /// Search products
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Product>>> Search([FromQuery] string term)
    {
        var (products, error) = await _databaseService.SearchProductsAsync(term);
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error.Message);
        }
        return Ok(products);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] Product product)
    {
        var (productId, error) = await _databaseService.CreateProductAsync(product);
        if (error != null)
        {
            return BadRequest(new { message = error.Message });
        }
        product.ProductID = productId!.Value;
        return CreatedAtAction(nameof(GetById), new { id = product.ProductID }, product);
    }

    /// <summary>
    /// Update a product
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(short id, [FromBody] Product product)
    {
        product.ProductID = id;
        var (success, error) = await _databaseService.UpdateProductAsync(product);
        if (error != null)
        {
            return BadRequest(new { message = error.Message });
        }
        return NoContent();
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(short id)
    {
        var (success, error) = await _databaseService.DeleteProductAsync(id);
        if (error != null)
        {
            return BadRequest(new { message = error.Message });
        }
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IDatabaseService _databaseService;

    public CategoriesController(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetAll()
    {
        var (categories, error) = await _databaseService.GetAllCategoriesAsync();
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error.Message);
        }
        return Ok(categories);
    }

    /// <summary>
    /// Get a category by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetById(short id)
    {
        var (category, error) = await _databaseService.GetCategoryByIdAsync(id);
        if (error != null)
        {
            return NotFound(new { message = error.Message });
        }
        if (category == null)
        {
            return NotFound(new { message = "Category not found" });
        }
        return Ok(category);
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Category>> Create([FromBody] Category category)
    {
        var (categoryId, error) = await _databaseService.CreateCategoryAsync(category);
        if (error != null)
        {
            return BadRequest(new { message = error.Message });
        }
        category.CategoryID = categoryId!.Value;
        return CreatedAtAction(nameof(GetById), new { id = category.CategoryID }, category);
    }

    /// <summary>
    /// Update a category
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(short id, [FromBody] Category category)
    {
        category.CategoryID = id;
        var (success, error) = await _databaseService.UpdateCategoryAsync(category);
        if (error != null)
        {
            return BadRequest(new { message = error.Message });
        }
        return NoContent();
    }

    /// <summary>
    /// Delete a category
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(short id)
    {
        var (success, error) = await _databaseService.DeleteCategoryAsync(id);
        if (error != null)
        {
            return BadRequest(new { message = error.Message });
        }
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SuppliersController : ControllerBase
{
    private readonly IDatabaseService _databaseService;

    public SuppliersController(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    /// <summary>
    /// Get all suppliers
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Supplier>>> GetAll()
    {
        var (suppliers, error) = await _databaseService.GetAllSuppliersAsync();
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error.Message);
        }
        return Ok(suppliers);
    }

    /// <summary>
    /// Get a supplier by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Supplier>> GetById(short id)
    {
        var (supplier, error) = await _databaseService.GetSupplierByIdAsync(id);
        if (error != null)
        {
            return NotFound(new { message = error.Message });
        }
        if (supplier == null)
        {
            return NotFound(new { message = "Supplier not found" });
        }
        return Ok(supplier);
    }

    /// <summary>
    /// Create a new supplier
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Supplier>> Create([FromBody] Supplier supplier)
    {
        var (supplierId, error) = await _databaseService.CreateSupplierAsync(supplier);
        if (error != null)
        {
            return BadRequest(new { message = error.Message });
        }
        supplier.SupplierID = supplierId!.Value;
        return CreatedAtAction(nameof(GetById), new { id = supplier.SupplierID }, supplier);
    }

    /// <summary>
    /// Update a supplier
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(short id, [FromBody] Supplier supplier)
    {
        supplier.SupplierID = id;
        var (success, error) = await _databaseService.UpdateSupplierAsync(supplier);
        if (error != null)
        {
            return BadRequest(new { message = error.Message });
        }
        return NoContent();
    }

    /// <summary>
    /// Delete a supplier
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(short id)
    {
        var (success, error) = await _databaseService.DeleteSupplierAsync(id);
        if (error != null)
        {
            return BadRequest(new { message = error.Message });
        }
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private readonly IDatabaseService _databaseService;

    public CustomersController(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    /// <summary>
    /// Get all customers
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> GetAll()
    {
        var (customers, error) = await _databaseService.GetAllCustomersAsync();
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error.Message);
        }
        return Ok(customers);
    }

    /// <summary>
    /// Get a customer by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetById(string id)
    {
        var (customer, error) = await _databaseService.GetCustomerByIdAsync(id);
        if (error != null)
        {
            return NotFound(new { message = error.Message });
        }
        if (customer == null)
        {
            return NotFound(new { message = "Customer not found" });
        }
        return Ok(customer);
    }

    /// <summary>
    /// Search customers
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Customer>>> Search([FromQuery] string term)
    {
        var (customers, error) = await _databaseService.SearchCustomersAsync(term);
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error.Message);
        }
        return Ok(customers);
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Customer>> Create([FromBody] Customer customer)
    {
        var (customerId, error) = await _databaseService.CreateCustomerAsync(customer);
        if (error != null)
        {
            return BadRequest(new { message = error.Message });
        }
        return CreatedAtAction(nameof(GetById), new { id = customer.CustomerID }, customer);
    }

    /// <summary>
    /// Update a customer
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, [FromBody] Customer customer)
    {
        customer.CustomerID = id;
        var (success, error) = await _databaseService.UpdateCustomerAsync(customer);
        if (error != null)
        {
            return BadRequest(new { message = error.Message });
        }
        return NoContent();
    }

    /// <summary>
    /// Delete a customer
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var (success, error) = await _databaseService.DeleteCustomerAsync(id);
        if (error != null)
        {
            return BadRequest(new { message = error.Message });
        }
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IDatabaseService _databaseService;

    public OrdersController(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    /// <summary>
    /// Get all orders
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetAll()
    {
        var (orders, error) = await _databaseService.GetAllOrdersAsync();
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error.Message);
        }
        return Ok(orders);
    }

    /// <summary>
    /// Get an order by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetById(short id)
    {
        var (order, error) = await _databaseService.GetOrderByIdAsync(id);
        if (error != null)
        {
            return NotFound(new { message = error.Message });
        }
        if (order == null)
        {
            return NotFound(new { message = "Order not found" });
        }
        return Ok(order);
    }

    /// <summary>
    /// Get orders by customer
    /// </summary>
    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<Order>>> GetByCustomer(string customerId)
    {
        var (orders, error) = await _databaseService.GetOrdersByCustomerAsync(customerId);
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error.Message);
        }
        return Ok(orders);
    }

    /// <summary>
    /// Get order details
    /// </summary>
    [HttpGet("{id}/details")]
    public async Task<ActionResult<IEnumerable<OrderDetail>>> GetDetails(short id)
    {
        var (details, error) = await _databaseService.GetOrderDetailsAsync(id);
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error.Message);
        }
        return Ok(details);
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Order>> Create([FromBody] Order order)
    {
        var (orderId, error) = await _databaseService.CreateOrderAsync(order);
        if (error != null)
        {
            return BadRequest(new { message = error.Message });
        }
        order.OrderID = orderId!.Value;
        return CreatedAtAction(nameof(GetById), new { id = order.OrderID }, order);
    }

    /// <summary>
    /// Delete an order
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(short id)
    {
        var (success, error) = await _databaseService.DeleteOrderAsync(id);
        if (error != null)
        {
            return BadRequest(new { message = error.Message });
        }
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDatabaseService _databaseService;

    public DashboardController(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStats>> GetStats()
    {
        var (stats, error) = await _databaseService.GetDashboardStatsAsync();
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error.Message);
        }
        return Ok(stats);
    }

    /// <summary>
    /// Get low stock products
    /// </summary>
    [HttpGet("lowstock")]
    public async Task<ActionResult<IEnumerable<Product>>> GetLowStock([FromQuery] short threshold = 10)
    {
        var (products, error) = await _databaseService.GetLowStockProductsAsync(threshold);
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error.Message);
        }
        return Ok(products);
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EmployeesController : ControllerBase
{
    private readonly IDatabaseService _databaseService;

    public EmployeesController(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    /// <summary>
    /// Get all employees
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetAll()
    {
        var (employees, error) = await _databaseService.GetAllEmployeesAsync();
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error.Message);
        }
        return Ok(employees);
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ShippersController : ControllerBase
{
    private readonly IDatabaseService _databaseService;

    public ShippersController(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    /// <summary>
    /// Get all shippers
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Shipper>>> GetAll()
    {
        var (shippers, error) = await _databaseService.GetAllShippersAsync();
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error.Message);
        }
        return Ok(shippers);
    }
}
