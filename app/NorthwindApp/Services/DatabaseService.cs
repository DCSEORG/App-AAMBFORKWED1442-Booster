using Microsoft.Data.SqlClient;
using NorthwindApp.Models;
using System.Runtime.CompilerServices;

namespace NorthwindApp.Services;

public interface IDatabaseService
{
    Task<(List<Product> Products, ErrorInfo? Error)> GetAllProductsAsync();
    Task<(Product? Product, ErrorInfo? Error)> GetProductByIdAsync(short productId);
    Task<(List<Product> Products, ErrorInfo? Error)> GetProductsByCategoryAsync(short categoryId);
    Task<(List<Product> Products, ErrorInfo? Error)> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    Task<(List<Product> Products, ErrorInfo? Error)> SearchProductsAsync(string searchTerm);
    Task<(short? ProductId, ErrorInfo? Error)> CreateProductAsync(Product product);
    Task<(bool Success, ErrorInfo? Error)> UpdateProductAsync(Product product);
    Task<(bool Success, ErrorInfo? Error)> DeleteProductAsync(short productId);
    
    Task<(List<Category> Categories, ErrorInfo? Error)> GetAllCategoriesAsync();
    Task<(Category? Category, ErrorInfo? Error)> GetCategoryByIdAsync(short categoryId);
    Task<(short? CategoryId, ErrorInfo? Error)> CreateCategoryAsync(Category category);
    Task<(bool Success, ErrorInfo? Error)> UpdateCategoryAsync(Category category);
    Task<(bool Success, ErrorInfo? Error)> DeleteCategoryAsync(short categoryId);
    
    Task<(List<Supplier> Suppliers, ErrorInfo? Error)> GetAllSuppliersAsync();
    Task<(Supplier? Supplier, ErrorInfo? Error)> GetSupplierByIdAsync(short supplierId);
    Task<(short? SupplierId, ErrorInfo? Error)> CreateSupplierAsync(Supplier supplier);
    Task<(bool Success, ErrorInfo? Error)> UpdateSupplierAsync(Supplier supplier);
    Task<(bool Success, ErrorInfo? Error)> DeleteSupplierAsync(short supplierId);
    
    Task<(List<Customer> Customers, ErrorInfo? Error)> GetAllCustomersAsync();
    Task<(Customer? Customer, ErrorInfo? Error)> GetCustomerByIdAsync(string customerId);
    Task<(List<Customer> Customers, ErrorInfo? Error)> SearchCustomersAsync(string searchTerm);
    Task<(string? CustomerId, ErrorInfo? Error)> CreateCustomerAsync(Customer customer);
    Task<(bool Success, ErrorInfo? Error)> UpdateCustomerAsync(Customer customer);
    Task<(bool Success, ErrorInfo? Error)> DeleteCustomerAsync(string customerId);
    
    Task<(List<Order> Orders, ErrorInfo? Error)> GetAllOrdersAsync();
    Task<(Order? Order, ErrorInfo? Error)> GetOrderByIdAsync(short orderId);
    Task<(List<Order> Orders, ErrorInfo? Error)> GetOrdersByCustomerAsync(string customerId);
    Task<(List<OrderDetail> OrderDetails, ErrorInfo? Error)> GetOrderDetailsAsync(short orderId);
    Task<(short? OrderId, ErrorInfo? Error)> CreateOrderAsync(Order order);
    Task<(bool Success, ErrorInfo? Error)> DeleteOrderAsync(short orderId);
    
    Task<(List<Employee> Employees, ErrorInfo? Error)> GetAllEmployeesAsync();
    Task<(List<Shipper> Shippers, ErrorInfo? Error)> GetAllShippersAsync();
    
    Task<(DashboardStats? Stats, ErrorInfo? Error)> GetDashboardStatsAsync();
    Task<(List<Product> Products, ErrorInfo? Error)> GetLowStockProductsAsync(short threshold = 10);
}

public class DatabaseService : IDatabaseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private ErrorInfo CreateErrorInfo(Exception ex, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        var error = new ErrorInfo
        {
            Message = ex.Message,
            Details = ex.ToString(),
            FilePath = Path.GetFileName(filePath),
            LineNumber = lineNumber
        };

        // Add managed identity hints for common errors
        if (ex.Message.Contains("Login failed") || ex.Message.Contains("Cannot open server"))
        {
            error.ManagedIdentityHint = "Managed Identity authentication may have failed. Ensure:\n" +
                "1. The managed identity has been granted access to the database\n" +
                "2. Run the run-sql-dbrole.py script to create the database user\n" +
                "3. The AZURE_CLIENT_ID environment variable matches the managed identity client ID\n" +
                "4. For local development, use 'Authentication=Active Directory Default' and run 'az login'";
        }
        else if (ex.Message.Contains("Unable to load") || ex.Message.Contains("ManagedIdentity"))
        {
            error.ManagedIdentityHint = "Unable to authenticate with Managed Identity. Ensure:\n" +
                "1. The app is running in Azure App Service with a user-assigned managed identity\n" +
                "2. The AZURE_CLIENT_ID and ManagedIdentityClientId app settings are configured\n" +
                "3. For local development, ensure you have run 'az login' first";
        }

        return error;
    }

    #region Products

    public async Task<(List<Product> Products, ErrorInfo? Error)> GetAllProductsAsync()
    {
        try
        {
            var products = new List<Product>();
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetAllProducts", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(MapProduct(reader));
            }
            return (products, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all products");
            return (GetDummyProducts(), CreateErrorInfo(ex));
        }
    }

    public async Task<(Product? Product, ErrorInfo? Error)> GetProductByIdAsync(short productId)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetProductById", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ProductID", productId);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (MapProduct(reader), null);
            }
            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId}", productId);
            return (null, CreateErrorInfo(ex));
        }
    }

    public async Task<(List<Product> Products, ErrorInfo? Error)> GetProductsByCategoryAsync(short categoryId)
    {
        try
        {
            var products = new List<Product>();
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetProductsByCategory", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CategoryID", categoryId);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(MapProduct(reader));
            }
            return (products, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products by category {CategoryId}", categoryId);
            return (GetDummyProducts(), CreateErrorInfo(ex));
        }
    }

    public async Task<(List<Product> Products, ErrorInfo? Error)> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        try
        {
            var products = new List<Product>();
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetProductsByPriceRange", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@MinPrice", minPrice);
            command.Parameters.AddWithValue("@MaxPrice", maxPrice);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(MapProduct(reader));
            }
            return (products, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products by price range {MinPrice}-{MaxPrice}", minPrice, maxPrice);
            return (GetDummyProducts(), CreateErrorInfo(ex));
        }
    }

    public async Task<(List<Product> Products, ErrorInfo? Error)> SearchProductsAsync(string searchTerm)
    {
        try
        {
            var products = new List<Product>();
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_SearchProducts", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@SearchTerm", searchTerm);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(MapProduct(reader));
            }
            return (products, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with term {SearchTerm}", searchTerm);
            return (GetDummyProducts(), CreateErrorInfo(ex));
        }
    }

    public async Task<(short? ProductId, ErrorInfo? Error)> CreateProductAsync(Product product)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_CreateProduct", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ProductName", product.ProductName);
            command.Parameters.AddWithValue("@SupplierID", (object?)product.SupplierID ?? DBNull.Value);
            command.Parameters.AddWithValue("@CategoryID", (object?)product.CategoryID ?? DBNull.Value);
            command.Parameters.AddWithValue("@QuantityPerUnit", (object?)product.QuantityPerUnit ?? DBNull.Value);
            command.Parameters.AddWithValue("@UnitPrice", (object?)product.UnitPrice ?? DBNull.Value);
            command.Parameters.AddWithValue("@UnitsInStock", product.UnitsInStock ?? 0);
            command.Parameters.AddWithValue("@UnitsOnOrder", product.UnitsOnOrder ?? 0);
            command.Parameters.AddWithValue("@ReorderLevel", product.ReorderLevel ?? 0);
            command.Parameters.AddWithValue("@Discontinued", product.Discontinued);
            var result = await command.ExecuteScalarAsync();
            return (Convert.ToInt16(result), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product {ProductName}", product.ProductName);
            return (null, CreateErrorInfo(ex));
        }
    }

    public async Task<(bool Success, ErrorInfo? Error)> UpdateProductAsync(Product product)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_UpdateProduct", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ProductID", product.ProductID);
            command.Parameters.AddWithValue("@ProductName", product.ProductName);
            command.Parameters.AddWithValue("@SupplierID", (object?)product.SupplierID ?? DBNull.Value);
            command.Parameters.AddWithValue("@CategoryID", (object?)product.CategoryID ?? DBNull.Value);
            command.Parameters.AddWithValue("@QuantityPerUnit", (object?)product.QuantityPerUnit ?? DBNull.Value);
            command.Parameters.AddWithValue("@UnitPrice", (object?)product.UnitPrice ?? DBNull.Value);
            command.Parameters.AddWithValue("@UnitsInStock", product.UnitsInStock ?? 0);
            command.Parameters.AddWithValue("@UnitsOnOrder", product.UnitsOnOrder ?? 0);
            command.Parameters.AddWithValue("@ReorderLevel", product.ReorderLevel ?? 0);
            command.Parameters.AddWithValue("@Discontinued", product.Discontinued);
            await command.ExecuteNonQueryAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", product.ProductID);
            return (false, CreateErrorInfo(ex));
        }
    }

    public async Task<(bool Success, ErrorInfo? Error)> DeleteProductAsync(short productId)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_DeleteProduct", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ProductID", productId);
            await command.ExecuteNonQueryAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", productId);
            return (false, CreateErrorInfo(ex));
        }
    }

    private static Product MapProduct(SqlDataReader reader)
    {
        return new Product
        {
            ProductID = reader.GetInt16(reader.GetOrdinal("ProductID")),
            ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
            SupplierID = reader.IsDBNull(reader.GetOrdinal("SupplierID")) ? null : reader.GetInt16(reader.GetOrdinal("SupplierID")),
            CategoryID = reader.IsDBNull(reader.GetOrdinal("CategoryID")) ? null : reader.GetInt16(reader.GetOrdinal("CategoryID")),
            QuantityPerUnit = reader.IsDBNull(reader.GetOrdinal("QuantityPerUnit")) ? null : reader.GetString(reader.GetOrdinal("QuantityPerUnit")),
            UnitPrice = reader.IsDBNull(reader.GetOrdinal("UnitPrice")) ? null : reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
            UnitsInStock = reader.IsDBNull(reader.GetOrdinal("UnitsInStock")) ? null : reader.GetInt16(reader.GetOrdinal("UnitsInStock")),
            UnitsOnOrder = reader.IsDBNull(reader.GetOrdinal("UnitsOnOrder")) ? null : reader.GetInt16(reader.GetOrdinal("UnitsOnOrder")),
            ReorderLevel = reader.IsDBNull(reader.GetOrdinal("ReorderLevel")) ? null : reader.GetInt16(reader.GetOrdinal("ReorderLevel")),
            Discontinued = reader.GetBoolean(reader.GetOrdinal("Discontinued")),
            CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? null : reader.GetString(reader.GetOrdinal("CategoryName")),
            SupplierName = reader.IsDBNull(reader.GetOrdinal("SupplierName")) ? null : reader.GetString(reader.GetOrdinal("SupplierName"))
        };
    }

    private static List<Product> GetDummyProducts()
    {
        return new List<Product>
        {
            new() { ProductID = 1, ProductName = "Chai (Demo Data)", CategoryName = "Beverages", SupplierName = "Exotic Liquids", UnitPrice = 18.00m, UnitsInStock = 39, Discontinued = false },
            new() { ProductID = 2, ProductName = "Chang (Demo Data)", CategoryName = "Beverages", SupplierName = "Exotic Liquids", UnitPrice = 19.00m, UnitsInStock = 17, Discontinued = false },
            new() { ProductID = 3, ProductName = "Aniseed Syrup (Demo Data)", CategoryName = "Condiments", SupplierName = "Exotic Liquids", UnitPrice = 10.00m, UnitsInStock = 13, Discontinued = false },
            new() { ProductID = 4, ProductName = "Chef Anton's Cajun (Demo Data)", CategoryName = "Condiments", SupplierName = "New Orleans Cajun", UnitPrice = 22.00m, UnitsInStock = 53, Discontinued = false },
            new() { ProductID = 5, ProductName = "Grandma's Boysenberry (Demo Data)", CategoryName = "Condiments", SupplierName = "Grandma Kelly's", UnitPrice = 25.00m, UnitsInStock = 120, Discontinued = false }
        };
    }

    #endregion

    #region Categories

    public async Task<(List<Category> Categories, ErrorInfo? Error)> GetAllCategoriesAsync()
    {
        try
        {
            var categories = new List<Category>();
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetAllCategories", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    CategoryID = reader.GetInt16(reader.GetOrdinal("CategoryID")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description"))
                });
            }
            return (categories, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all categories");
            return (GetDummyCategories(), CreateErrorInfo(ex));
        }
    }

    public async Task<(Category? Category, ErrorInfo? Error)> GetCategoryByIdAsync(short categoryId)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetCategoryById", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CategoryID", categoryId);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (new Category
                {
                    CategoryID = reader.GetInt16(reader.GetOrdinal("CategoryID")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description"))
                }, null);
            }
            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category {CategoryId}", categoryId);
            return (null, CreateErrorInfo(ex));
        }
    }

    public async Task<(short? CategoryId, ErrorInfo? Error)> CreateCategoryAsync(Category category)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_CreateCategory", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CategoryName", category.CategoryName);
            command.Parameters.AddWithValue("@Description", (object?)category.Description ?? DBNull.Value);
            var result = await command.ExecuteScalarAsync();
            return (Convert.ToInt16(result), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category {CategoryName}", category.CategoryName);
            return (null, CreateErrorInfo(ex));
        }
    }

    public async Task<(bool Success, ErrorInfo? Error)> UpdateCategoryAsync(Category category)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_UpdateCategory", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CategoryID", category.CategoryID);
            command.Parameters.AddWithValue("@CategoryName", category.CategoryName);
            command.Parameters.AddWithValue("@Description", (object?)category.Description ?? DBNull.Value);
            await command.ExecuteNonQueryAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", category.CategoryID);
            return (false, CreateErrorInfo(ex));
        }
    }

    public async Task<(bool Success, ErrorInfo? Error)> DeleteCategoryAsync(short categoryId)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_DeleteCategory", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CategoryID", categoryId);
            await command.ExecuteNonQueryAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", categoryId);
            return (false, CreateErrorInfo(ex));
        }
    }

    private static List<Category> GetDummyCategories()
    {
        return new List<Category>
        {
            new() { CategoryID = 1, CategoryName = "Beverages", Description = "Soft drinks, coffees, teas, beers, and ales" },
            new() { CategoryID = 2, CategoryName = "Condiments", Description = "Sweet and savory sauces, relishes, spreads, and seasonings" },
            new() { CategoryID = 3, CategoryName = "Confections", Description = "Desserts, candies, and sweet breads" },
            new() { CategoryID = 4, CategoryName = "Dairy Products", Description = "Cheeses" },
            new() { CategoryID = 5, CategoryName = "Grains/Cereals", Description = "Breads, crackers, pasta, and cereal" }
        };
    }

    #endregion

    #region Suppliers

    public async Task<(List<Supplier> Suppliers, ErrorInfo? Error)> GetAllSuppliersAsync()
    {
        try
        {
            var suppliers = new List<Supplier>();
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetAllSuppliers", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                suppliers.Add(MapSupplier(reader));
            }
            return (suppliers, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all suppliers");
            return (GetDummySuppliers(), CreateErrorInfo(ex));
        }
    }

    public async Task<(Supplier? Supplier, ErrorInfo? Error)> GetSupplierByIdAsync(short supplierId)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetSupplierById", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@SupplierID", supplierId);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (MapSupplier(reader), null);
            }
            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier {SupplierId}", supplierId);
            return (null, CreateErrorInfo(ex));
        }
    }

    public async Task<(short? SupplierId, ErrorInfo? Error)> CreateSupplierAsync(Supplier supplier)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_CreateSupplier", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CompanyName", supplier.CompanyName);
            command.Parameters.AddWithValue("@ContactName", (object?)supplier.ContactName ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContactTitle", (object?)supplier.ContactTitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@Address", (object?)supplier.Address ?? DBNull.Value);
            command.Parameters.AddWithValue("@City", (object?)supplier.City ?? DBNull.Value);
            command.Parameters.AddWithValue("@Region", (object?)supplier.Region ?? DBNull.Value);
            command.Parameters.AddWithValue("@PostalCode", (object?)supplier.PostalCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@Country", (object?)supplier.Country ?? DBNull.Value);
            command.Parameters.AddWithValue("@Phone", (object?)supplier.Phone ?? DBNull.Value);
            command.Parameters.AddWithValue("@Fax", (object?)supplier.Fax ?? DBNull.Value);
            command.Parameters.AddWithValue("@HomePage", (object?)supplier.HomePage ?? DBNull.Value);
            var result = await command.ExecuteScalarAsync();
            return (Convert.ToInt16(result), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier {CompanyName}", supplier.CompanyName);
            return (null, CreateErrorInfo(ex));
        }
    }

    public async Task<(bool Success, ErrorInfo? Error)> UpdateSupplierAsync(Supplier supplier)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_UpdateSupplier", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@SupplierID", supplier.SupplierID);
            command.Parameters.AddWithValue("@CompanyName", supplier.CompanyName);
            command.Parameters.AddWithValue("@ContactName", (object?)supplier.ContactName ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContactTitle", (object?)supplier.ContactTitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@Address", (object?)supplier.Address ?? DBNull.Value);
            command.Parameters.AddWithValue("@City", (object?)supplier.City ?? DBNull.Value);
            command.Parameters.AddWithValue("@Region", (object?)supplier.Region ?? DBNull.Value);
            command.Parameters.AddWithValue("@PostalCode", (object?)supplier.PostalCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@Country", (object?)supplier.Country ?? DBNull.Value);
            command.Parameters.AddWithValue("@Phone", (object?)supplier.Phone ?? DBNull.Value);
            command.Parameters.AddWithValue("@Fax", (object?)supplier.Fax ?? DBNull.Value);
            command.Parameters.AddWithValue("@HomePage", (object?)supplier.HomePage ?? DBNull.Value);
            await command.ExecuteNonQueryAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier {SupplierId}", supplier.SupplierID);
            return (false, CreateErrorInfo(ex));
        }
    }

    public async Task<(bool Success, ErrorInfo? Error)> DeleteSupplierAsync(short supplierId)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_DeleteSupplier", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@SupplierID", supplierId);
            await command.ExecuteNonQueryAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting supplier {SupplierId}", supplierId);
            return (false, CreateErrorInfo(ex));
        }
    }

    private static Supplier MapSupplier(SqlDataReader reader)
    {
        return new Supplier
        {
            SupplierID = reader.GetInt16(reader.GetOrdinal("SupplierID")),
            CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
            ContactName = reader.IsDBNull(reader.GetOrdinal("ContactName")) ? null : reader.GetString(reader.GetOrdinal("ContactName")),
            ContactTitle = reader.IsDBNull(reader.GetOrdinal("ContactTitle")) ? null : reader.GetString(reader.GetOrdinal("ContactTitle")),
            Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
            City = reader.IsDBNull(reader.GetOrdinal("City")) ? null : reader.GetString(reader.GetOrdinal("City")),
            Region = reader.IsDBNull(reader.GetOrdinal("Region")) ? null : reader.GetString(reader.GetOrdinal("Region")),
            PostalCode = reader.IsDBNull(reader.GetOrdinal("PostalCode")) ? null : reader.GetString(reader.GetOrdinal("PostalCode")),
            Country = reader.IsDBNull(reader.GetOrdinal("Country")) ? null : reader.GetString(reader.GetOrdinal("Country")),
            Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
            Fax = reader.IsDBNull(reader.GetOrdinal("Fax")) ? null : reader.GetString(reader.GetOrdinal("Fax")),
            HomePage = reader.IsDBNull(reader.GetOrdinal("HomePage")) ? null : reader.GetString(reader.GetOrdinal("HomePage"))
        };
    }

    private static List<Supplier> GetDummySuppliers()
    {
        return new List<Supplier>
        {
            new() { SupplierID = 1, CompanyName = "Exotic Liquids", ContactName = "Charlotte Cooper", City = "London", Country = "UK" },
            new() { SupplierID = 2, CompanyName = "New Orleans Cajun Delights", ContactName = "Shelley Burke", City = "New Orleans", Country = "USA" },
            new() { SupplierID = 3, CompanyName = "Grandma Kelly's Homestead", ContactName = "Regina Murphy", City = "Ann Arbor", Country = "USA" }
        };
    }

    #endregion

    #region Customers

    public async Task<(List<Customer> Customers, ErrorInfo? Error)> GetAllCustomersAsync()
    {
        try
        {
            var customers = new List<Customer>();
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetAllCustomers", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                customers.Add(MapCustomer(reader));
            }
            return (customers, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all customers");
            return (GetDummyCustomers(), CreateErrorInfo(ex));
        }
    }

    public async Task<(Customer? Customer, ErrorInfo? Error)> GetCustomerByIdAsync(string customerId)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetCustomerById", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CustomerID", customerId);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (MapCustomer(reader), null);
            }
            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {CustomerId}", customerId);
            return (null, CreateErrorInfo(ex));
        }
    }

    public async Task<(List<Customer> Customers, ErrorInfo? Error)> SearchCustomersAsync(string searchTerm)
    {
        try
        {
            var customers = new List<Customer>();
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_SearchCustomers", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@SearchTerm", searchTerm);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                customers.Add(MapCustomer(reader));
            }
            return (customers, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers with term {SearchTerm}", searchTerm);
            return (GetDummyCustomers(), CreateErrorInfo(ex));
        }
    }

    public async Task<(string? CustomerId, ErrorInfo? Error)> CreateCustomerAsync(Customer customer)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_CreateCustomer", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CustomerID", customer.CustomerID);
            command.Parameters.AddWithValue("@CompanyName", customer.CompanyName);
            command.Parameters.AddWithValue("@ContactName", (object?)customer.ContactName ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContactTitle", (object?)customer.ContactTitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@Address", (object?)customer.Address ?? DBNull.Value);
            command.Parameters.AddWithValue("@City", (object?)customer.City ?? DBNull.Value);
            command.Parameters.AddWithValue("@Region", (object?)customer.Region ?? DBNull.Value);
            command.Parameters.AddWithValue("@PostalCode", (object?)customer.PostalCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@Country", (object?)customer.Country ?? DBNull.Value);
            command.Parameters.AddWithValue("@Phone", (object?)customer.Phone ?? DBNull.Value);
            command.Parameters.AddWithValue("@Fax", (object?)customer.Fax ?? DBNull.Value);
            var result = await command.ExecuteScalarAsync();
            return (result?.ToString(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer {CustomerId}", customer.CustomerID);
            return (null, CreateErrorInfo(ex));
        }
    }

    public async Task<(bool Success, ErrorInfo? Error)> UpdateCustomerAsync(Customer customer)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_UpdateCustomer", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CustomerID", customer.CustomerID);
            command.Parameters.AddWithValue("@CompanyName", customer.CompanyName);
            command.Parameters.AddWithValue("@ContactName", (object?)customer.ContactName ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContactTitle", (object?)customer.ContactTitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@Address", (object?)customer.Address ?? DBNull.Value);
            command.Parameters.AddWithValue("@City", (object?)customer.City ?? DBNull.Value);
            command.Parameters.AddWithValue("@Region", (object?)customer.Region ?? DBNull.Value);
            command.Parameters.AddWithValue("@PostalCode", (object?)customer.PostalCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@Country", (object?)customer.Country ?? DBNull.Value);
            command.Parameters.AddWithValue("@Phone", (object?)customer.Phone ?? DBNull.Value);
            command.Parameters.AddWithValue("@Fax", (object?)customer.Fax ?? DBNull.Value);
            await command.ExecuteNonQueryAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", customer.CustomerID);
            return (false, CreateErrorInfo(ex));
        }
    }

    public async Task<(bool Success, ErrorInfo? Error)> DeleteCustomerAsync(string customerId)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_DeleteCustomer", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CustomerID", customerId);
            await command.ExecuteNonQueryAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {CustomerId}", customerId);
            return (false, CreateErrorInfo(ex));
        }
    }

    private static Customer MapCustomer(SqlDataReader reader)
    {
        return new Customer
        {
            CustomerID = reader.GetString(reader.GetOrdinal("CustomerID")).Trim(),
            CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
            ContactName = reader.IsDBNull(reader.GetOrdinal("ContactName")) ? null : reader.GetString(reader.GetOrdinal("ContactName")),
            ContactTitle = reader.IsDBNull(reader.GetOrdinal("ContactTitle")) ? null : reader.GetString(reader.GetOrdinal("ContactTitle")),
            Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
            City = reader.IsDBNull(reader.GetOrdinal("City")) ? null : reader.GetString(reader.GetOrdinal("City")),
            Region = reader.IsDBNull(reader.GetOrdinal("Region")) ? null : reader.GetString(reader.GetOrdinal("Region")),
            PostalCode = reader.IsDBNull(reader.GetOrdinal("PostalCode")) ? null : reader.GetString(reader.GetOrdinal("PostalCode")),
            Country = reader.IsDBNull(reader.GetOrdinal("Country")) ? null : reader.GetString(reader.GetOrdinal("Country")),
            Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
            Fax = reader.IsDBNull(reader.GetOrdinal("Fax")) ? null : reader.GetString(reader.GetOrdinal("Fax"))
        };
    }

    private static List<Customer> GetDummyCustomers()
    {
        return new List<Customer>
        {
            new() { CustomerID = "ALFKI", CompanyName = "Alfreds Futterkiste", ContactName = "Maria Anders", City = "Berlin", Country = "Germany" },
            new() { CustomerID = "ANATR", CompanyName = "Ana Trujillo Emparedados y helados", ContactName = "Ana Trujillo", City = "México D.F.", Country = "Mexico" },
            new() { CustomerID = "ANTON", CompanyName = "Antonio Moreno Taquería", ContactName = "Antonio Moreno", City = "México D.F.", Country = "Mexico" }
        };
    }

    #endregion

    #region Orders

    public async Task<(List<Order> Orders, ErrorInfo? Error)> GetAllOrdersAsync()
    {
        try
        {
            var orders = new List<Order>();
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetAllOrders", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orders.Add(MapOrder(reader));
            }
            return (orders, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all orders");
            return (GetDummyOrders(), CreateErrorInfo(ex));
        }
    }

    public async Task<(Order? Order, ErrorInfo? Error)> GetOrderByIdAsync(short orderId)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetOrderById", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@OrderID", orderId);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (MapOrder(reader), null);
            }
            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId}", orderId);
            return (null, CreateErrorInfo(ex));
        }
    }

    public async Task<(List<Order> Orders, ErrorInfo? Error)> GetOrdersByCustomerAsync(string customerId)
    {
        try
        {
            var orders = new List<Order>();
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetOrdersByCustomer", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CustomerID", customerId);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orders.Add(MapOrder(reader));
            }
            return (orders, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders for customer {CustomerId}", customerId);
            return (GetDummyOrders(), CreateErrorInfo(ex));
        }
    }

    public async Task<(List<OrderDetail> OrderDetails, ErrorInfo? Error)> GetOrderDetailsAsync(short orderId)
    {
        try
        {
            var details = new List<OrderDetail>();
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetOrderDetails", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@OrderID", orderId);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                details.Add(new OrderDetail
                {
                    OrderID = reader.GetInt16(reader.GetOrdinal("OrderID")),
                    ProductID = reader.GetInt16(reader.GetOrdinal("ProductID")),
                    UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                    Quantity = reader.GetInt16(reader.GetOrdinal("Quantity")),
                    Discount = reader.GetFloat(reader.GetOrdinal("Discount")),
                    ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? null : reader.GetString(reader.GetOrdinal("ProductName")),
                    ExtendedPrice = reader.GetDecimal(reader.GetOrdinal("ExtendedPrice"))
                });
            }
            return (details, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order details for order {OrderId}", orderId);
            return (new List<OrderDetail>(), CreateErrorInfo(ex));
        }
    }

    public async Task<(short? OrderId, ErrorInfo? Error)> CreateOrderAsync(Order order)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_CreateOrder", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CustomerID", (object?)order.CustomerID ?? DBNull.Value);
            command.Parameters.AddWithValue("@EmployeeID", (object?)order.EmployeeID ?? DBNull.Value);
            command.Parameters.AddWithValue("@OrderDate", (object?)order.OrderDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@RequiredDate", (object?)order.RequiredDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShippedDate", (object?)order.ShippedDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShipVia", (object?)order.ShipVia ?? DBNull.Value);
            command.Parameters.AddWithValue("@Freight", (object?)order.Freight ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShipName", (object?)order.ShipName ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShipAddress", (object?)order.ShipAddress ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShipCity", (object?)order.ShipCity ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShipRegion", (object?)order.ShipRegion ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShipPostalCode", (object?)order.ShipPostalCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShipCountry", (object?)order.ShipCountry ?? DBNull.Value);
            var result = await command.ExecuteScalarAsync();
            return (Convert.ToInt16(result), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return (null, CreateErrorInfo(ex));
        }
    }

    public async Task<(bool Success, ErrorInfo? Error)> DeleteOrderAsync(short orderId)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_DeleteOrder", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@OrderID", orderId);
            await command.ExecuteNonQueryAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order {OrderId}", orderId);
            return (false, CreateErrorInfo(ex));
        }
    }

    private static Order MapOrder(SqlDataReader reader)
    {
        return new Order
        {
            OrderID = reader.GetInt16(reader.GetOrdinal("OrderID")),
            CustomerID = reader.IsDBNull(reader.GetOrdinal("CustomerID")) ? null : reader.GetString(reader.GetOrdinal("CustomerID")).Trim(),
            EmployeeID = reader.IsDBNull(reader.GetOrdinal("EmployeeID")) ? null : reader.GetInt16(reader.GetOrdinal("EmployeeID")),
            OrderDate = reader.IsDBNull(reader.GetOrdinal("OrderDate")) ? null : reader.GetDateTime(reader.GetOrdinal("OrderDate")),
            RequiredDate = reader.IsDBNull(reader.GetOrdinal("RequiredDate")) ? null : reader.GetDateTime(reader.GetOrdinal("RequiredDate")),
            ShippedDate = reader.IsDBNull(reader.GetOrdinal("ShippedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ShippedDate")),
            ShipVia = reader.IsDBNull(reader.GetOrdinal("ShipVia")) ? null : reader.GetInt16(reader.GetOrdinal("ShipVia")),
            Freight = reader.IsDBNull(reader.GetOrdinal("Freight")) ? null : reader.GetDecimal(reader.GetOrdinal("Freight")),
            ShipName = reader.IsDBNull(reader.GetOrdinal("ShipName")) ? null : reader.GetString(reader.GetOrdinal("ShipName")),
            ShipAddress = reader.IsDBNull(reader.GetOrdinal("ShipAddress")) ? null : reader.GetString(reader.GetOrdinal("ShipAddress")),
            ShipCity = reader.IsDBNull(reader.GetOrdinal("ShipCity")) ? null : reader.GetString(reader.GetOrdinal("ShipCity")),
            ShipRegion = reader.IsDBNull(reader.GetOrdinal("ShipRegion")) ? null : reader.GetString(reader.GetOrdinal("ShipRegion")),
            ShipPostalCode = reader.IsDBNull(reader.GetOrdinal("ShipPostalCode")) ? null : reader.GetString(reader.GetOrdinal("ShipPostalCode")),
            ShipCountry = reader.IsDBNull(reader.GetOrdinal("ShipCountry")) ? null : reader.GetString(reader.GetOrdinal("ShipCountry")),
            CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? null : reader.GetString(reader.GetOrdinal("CustomerName")),
            EmployeeName = reader.IsDBNull(reader.GetOrdinal("EmployeeName")) ? null : reader.GetString(reader.GetOrdinal("EmployeeName")),
            ShipperName = reader.IsDBNull(reader.GetOrdinal("ShipperName")) ? null : reader.GetString(reader.GetOrdinal("ShipperName"))
        };
    }

    private static List<Order> GetDummyOrders()
    {
        return new List<Order>
        {
            new() { OrderID = 10248, CustomerID = "VINET", OrderDate = DateTime.Now.AddDays(-30), CustomerName = "Vins et alcools Chevalier", ShipCountry = "France" },
            new() { OrderID = 10249, CustomerID = "TOMSP", OrderDate = DateTime.Now.AddDays(-29), CustomerName = "Toms Spezialitäten", ShipCountry = "Germany" },
            new() { OrderID = 10250, CustomerID = "HANAR", OrderDate = DateTime.Now.AddDays(-28), CustomerName = "Hanari Carnes", ShipCountry = "Brazil" }
        };
    }

    #endregion

    #region Employees and Shippers

    public async Task<(List<Employee> Employees, ErrorInfo? Error)> GetAllEmployeesAsync()
    {
        try
        {
            var employees = new List<Employee>();
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetAllEmployees", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                employees.Add(new Employee
                {
                    EmployeeID = reader.GetInt16(reader.GetOrdinal("EmployeeID")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? null : reader.GetString(reader.GetOrdinal("Title")),
                    City = reader.IsDBNull(reader.GetOrdinal("City")) ? null : reader.GetString(reader.GetOrdinal("City")),
                    Country = reader.IsDBNull(reader.GetOrdinal("Country")) ? null : reader.GetString(reader.GetOrdinal("Country"))
                });
            }
            return (employees, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all employees");
            return (new List<Employee>
            {
                new() { EmployeeID = 1, FirstName = "Nancy", LastName = "Davolio", Title = "Sales Representative" },
                new() { EmployeeID = 2, FirstName = "Andrew", LastName = "Fuller", Title = "Vice President, Sales" }
            }, CreateErrorInfo(ex));
        }
    }

    public async Task<(List<Shipper> Shippers, ErrorInfo? Error)> GetAllShippersAsync()
    {
        try
        {
            var shippers = new List<Shipper>();
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetAllShippers", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                shippers.Add(new Shipper
                {
                    ShipperID = reader.GetInt16(reader.GetOrdinal("ShipperID")),
                    CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                    Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone"))
                });
            }
            return (shippers, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all shippers");
            return (new List<Shipper>
            {
                new() { ShipperID = 1, CompanyName = "Speedy Express", Phone = "(503) 555-9831" },
                new() { ShipperID = 2, CompanyName = "United Package", Phone = "(503) 555-3199" }
            }, CreateErrorInfo(ex));
        }
    }

    #endregion

    #region Dashboard

    public async Task<(DashboardStats? Stats, ErrorInfo? Error)> GetDashboardStatsAsync()
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetDashboardStats", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (new DashboardStats
                {
                    TotalProducts = reader.GetInt32(reader.GetOrdinal("TotalProducts")),
                    ActiveProducts = reader.GetInt32(reader.GetOrdinal("ActiveProducts")),
                    TotalCategories = reader.GetInt32(reader.GetOrdinal("TotalCategories")),
                    TotalSuppliers = reader.GetInt32(reader.GetOrdinal("TotalSuppliers")),
                    TotalCustomers = reader.GetInt32(reader.GetOrdinal("TotalCustomers")),
                    TotalOrders = reader.GetInt32(reader.GetOrdinal("TotalOrders")),
                    TotalEmployees = reader.GetInt32(reader.GetOrdinal("TotalEmployees"))
                }, null);
            }
            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            return (new DashboardStats
            {
                TotalProducts = 77,
                ActiveProducts = 69,
                TotalCategories = 8,
                TotalSuppliers = 29,
                TotalCustomers = 91,
                TotalOrders = 830,
                TotalEmployees = 9
            }, CreateErrorInfo(ex));
        }
    }

    public async Task<(List<Product> Products, ErrorInfo? Error)> GetLowStockProductsAsync(short threshold = 10)
    {
        try
        {
            var products = new List<Product>();
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_GetLowStockProducts", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Threshold", threshold);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(new Product
                {
                    ProductID = reader.GetInt16(reader.GetOrdinal("ProductID")),
                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                    UnitsInStock = reader.IsDBNull(reader.GetOrdinal("UnitsInStock")) ? null : reader.GetInt16(reader.GetOrdinal("UnitsInStock")),
                    ReorderLevel = reader.IsDBNull(reader.GetOrdinal("ReorderLevel")) ? null : reader.GetInt16(reader.GetOrdinal("ReorderLevel")),
                    CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? null : reader.GetString(reader.GetOrdinal("CategoryName")),
                    SupplierName = reader.IsDBNull(reader.GetOrdinal("SupplierName")) ? null : reader.GetString(reader.GetOrdinal("SupplierName"))
                });
            }
            return (products, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting low stock products");
            return (new List<Product>(), CreateErrorInfo(ex));
        }
    }

    #endregion
}
