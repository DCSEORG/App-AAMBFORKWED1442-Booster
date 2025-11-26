using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using NorthwindApp.Models;
using OpenAI.Chat;
using System.Text.Json;

namespace NorthwindApp.Services;

public interface IChatService
{
    Task<string> GetChatResponseAsync(string userMessage, List<ChatMessageInfo>? conversationHistory = null);
    bool IsGenAIEnabled { get; }
}

public class ChatMessageInfo
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly IDatabaseService _databaseService;
    private readonly AzureOpenAIClient? _openAIClient;
    private readonly string? _deploymentName;

    public bool IsGenAIEnabled { get; }

    public ChatService(IConfiguration configuration, ILogger<ChatService> logger, IDatabaseService databaseService)
    {
        _configuration = configuration;
        _logger = logger;
        _databaseService = databaseService;

        var endpoint = _configuration["OpenAI:Endpoint"];
        _deploymentName = _configuration["OpenAI:DeploymentName"];
        var genAIEnabled = _configuration.GetValue<bool>("GenAI:Enabled");

        if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(_deploymentName) && genAIEnabled)
        {
            try
            {
                // Use ManagedIdentityCredential with explicit client ID
                var managedIdentityClientId = _configuration["ManagedIdentityClientId"];
                TokenCredential credential;

                if (!string.IsNullOrEmpty(managedIdentityClientId))
                {
                    _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                    credential = new ManagedIdentityCredential(managedIdentityClientId);
                }
                else
                {
                    _logger.LogInformation("Using DefaultAzureCredential");
                    credential = new DefaultAzureCredential();
                }

                _openAIClient = new AzureOpenAIClient(new Uri(endpoint), credential);
                IsGenAIEnabled = true;
                _logger.LogInformation("Chat service initialized with Azure OpenAI at {Endpoint}", endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure OpenAI client");
                IsGenAIEnabled = false;
            }
        }
        else
        {
            _logger.LogWarning("GenAI is not enabled. Set GenAI:Enabled=true and configure OpenAI:Endpoint and OpenAI:DeploymentName");
            IsGenAIEnabled = false;
        }
    }

    public async Task<string> GetChatResponseAsync(string userMessage, List<ChatMessageInfo>? conversationHistory = null)
    {
        if (!IsGenAIEnabled || _openAIClient == null)
        {
            return GetDummyResponse(userMessage);
        }

        try
        {
            var chatClient = _openAIClient.GetChatClient(_deploymentName);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(GetSystemPrompt())
            };

            // Add conversation history
            if (conversationHistory != null)
            {
                foreach (var msg in conversationHistory)
                {
                    if (msg.Role == "user")
                        messages.Add(new UserChatMessage(msg.Content));
                    else if (msg.Role == "assistant")
                        messages.Add(new AssistantChatMessage(msg.Content));
                }
            }

            messages.Add(new UserChatMessage(userMessage));

            // Define function tools
            var options = new ChatCompletionOptions
            {
                Tools = {
                    ChatTool.CreateFunctionTool(
                        "get_all_products",
                        "Retrieves all products from the database"),
                    ChatTool.CreateFunctionTool(
                        "get_product_by_id",
                        "Gets a specific product by its ID",
                        BinaryData.FromObjectAsJson(new {
                            type = "object",
                            properties = new {
                                productId = new { type = "integer", description = "The product ID" }
                            },
                            required = new[] { "productId" }
                        })),
                    ChatTool.CreateFunctionTool(
                        "search_products",
                        "Searches for products by name, category, or supplier",
                        BinaryData.FromObjectAsJson(new {
                            type = "object",
                            properties = new {
                                searchTerm = new { type = "string", description = "The search term" }
                            },
                            required = new[] { "searchTerm" }
                        })),
                    ChatTool.CreateFunctionTool(
                        "get_products_by_category",
                        "Gets products filtered by category ID",
                        BinaryData.FromObjectAsJson(new {
                            type = "object",
                            properties = new {
                                categoryId = new { type = "integer", description = "The category ID" }
                            },
                            required = new[] { "categoryId" }
                        })),
                    ChatTool.CreateFunctionTool(
                        "get_products_by_price_range",
                        "Gets products within a price range",
                        BinaryData.FromObjectAsJson(new {
                            type = "object",
                            properties = new {
                                minPrice = new { type = "number", description = "Minimum price" },
                                maxPrice = new { type = "number", description = "Maximum price" }
                            },
                            required = new[] { "minPrice", "maxPrice" }
                        })),
                    ChatTool.CreateFunctionTool(
                        "get_all_categories",
                        "Retrieves all product categories"),
                    ChatTool.CreateFunctionTool(
                        "get_all_suppliers",
                        "Retrieves all suppliers"),
                    ChatTool.CreateFunctionTool(
                        "get_all_customers",
                        "Retrieves all customers"),
                    ChatTool.CreateFunctionTool(
                        "search_customers",
                        "Searches for customers by company name, contact name, or city",
                        BinaryData.FromObjectAsJson(new {
                            type = "object",
                            properties = new {
                                searchTerm = new { type = "string", description = "The search term" }
                            },
                            required = new[] { "searchTerm" }
                        })),
                    ChatTool.CreateFunctionTool(
                        "get_all_orders",
                        "Retrieves all orders"),
                    ChatTool.CreateFunctionTool(
                        "get_orders_by_customer",
                        "Gets orders for a specific customer",
                        BinaryData.FromObjectAsJson(new {
                            type = "object",
                            properties = new {
                                customerId = new { type = "string", description = "The customer ID (5 character code)" }
                            },
                            required = new[] { "customerId" }
                        })),
                    ChatTool.CreateFunctionTool(
                        "get_dashboard_stats",
                        "Gets dashboard statistics including total counts of products, categories, suppliers, customers, and orders"),
                    ChatTool.CreateFunctionTool(
                        "get_low_stock_products",
                        "Gets products with low stock (below threshold)",
                        BinaryData.FromObjectAsJson(new {
                            type = "object",
                            properties = new {
                                threshold = new { type = "integer", description = "Stock threshold (default 10)" }
                            }
                        })),
                    ChatTool.CreateFunctionTool(
                        "create_product",
                        "Creates a new product in the database",
                        BinaryData.FromObjectAsJson(new {
                            type = "object",
                            properties = new {
                                productName = new { type = "string", description = "Name of the product" },
                                categoryId = new { type = "integer", description = "Category ID" },
                                supplierId = new { type = "integer", description = "Supplier ID" },
                                unitPrice = new { type = "number", description = "Unit price" },
                                unitsInStock = new { type = "integer", description = "Units in stock" },
                                quantityPerUnit = new { type = "string", description = "Quantity per unit description" }
                            },
                            required = new[] { "productName" }
                        }))
                }
            };

            // Function calling loop
            var response = await chatClient.CompleteChatAsync(messages, options);
            var responseMessage = response.Value;

            while (responseMessage.FinishReason == ChatFinishReason.ToolCalls)
            {
                // Add assistant message with tool calls
                messages.Add(new AssistantChatMessage(responseMessage));

                // Process each tool call
                foreach (var toolCall in responseMessage.ToolCalls)
                {
                    var functionResult = await ExecuteFunctionAsync(toolCall.FunctionName, toolCall.FunctionArguments);
                    messages.Add(new ToolChatMessage(toolCall.Id, functionResult));
                }

                // Get next response
                response = await chatClient.CompleteChatAsync(messages, options);
                responseMessage = response.Value;
            }

            return responseMessage.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat response from Azure OpenAI");
            return $"I encountered an error while processing your request: {ex.Message}\n\nPlease try again or check the application logs for more details.";
        }
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, BinaryData arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<JsonElement>(arguments.ToString());

            switch (functionName)
            {
                case "get_all_products":
                    var (products, _) = await _databaseService.GetAllProductsAsync();
                    return JsonSerializer.Serialize(products.Take(50)); // Limit to 50 for response size

                case "get_product_by_id":
                    var productId = args.GetProperty("productId").GetInt16();
                    var (product, _) = await _databaseService.GetProductByIdAsync(productId);
                    return product != null ? JsonSerializer.Serialize(product) : "Product not found";

                case "search_products":
                    var searchTerm = args.GetProperty("searchTerm").GetString() ?? "";
                    var (searchResults, _) = await _databaseService.SearchProductsAsync(searchTerm);
                    return JsonSerializer.Serialize(searchResults.Take(20));

                case "get_products_by_category":
                    var categoryId = args.GetProperty("categoryId").GetInt16();
                    var (categoryProducts, _) = await _databaseService.GetProductsByCategoryAsync(categoryId);
                    return JsonSerializer.Serialize(categoryProducts);

                case "get_products_by_price_range":
                    var minPrice = args.GetProperty("minPrice").GetDecimal();
                    var maxPrice = args.GetProperty("maxPrice").GetDecimal();
                    var (priceProducts, _) = await _databaseService.GetProductsByPriceRangeAsync(minPrice, maxPrice);
                    return JsonSerializer.Serialize(priceProducts.Take(20));

                case "get_all_categories":
                    var (categories, _) = await _databaseService.GetAllCategoriesAsync();
                    return JsonSerializer.Serialize(categories);

                case "get_all_suppliers":
                    var (suppliers, _) = await _databaseService.GetAllSuppliersAsync();
                    return JsonSerializer.Serialize(suppliers.Take(20));

                case "get_all_customers":
                    var (customers, _) = await _databaseService.GetAllCustomersAsync();
                    return JsonSerializer.Serialize(customers.Take(20));

                case "search_customers":
                    var customerSearch = args.GetProperty("searchTerm").GetString() ?? "";
                    var (customerResults, _) = await _databaseService.SearchCustomersAsync(customerSearch);
                    return JsonSerializer.Serialize(customerResults.Take(20));

                case "get_all_orders":
                    var (orders, _) = await _databaseService.GetAllOrdersAsync();
                    return JsonSerializer.Serialize(orders.Take(20));

                case "get_orders_by_customer":
                    var customerId = args.GetProperty("customerId").GetString() ?? "";
                    var (customerOrders, _) = await _databaseService.GetOrdersByCustomerAsync(customerId);
                    return JsonSerializer.Serialize(customerOrders);

                case "get_dashboard_stats":
                    var (stats, _) = await _databaseService.GetDashboardStatsAsync();
                    return stats != null ? JsonSerializer.Serialize(stats) : "Unable to get stats";

                case "get_low_stock_products":
                    short threshold = 10;
                    if (args.TryGetProperty("threshold", out var thresholdProp))
                    {
                        threshold = thresholdProp.GetInt16();
                    }
                    var (lowStock, _) = await _databaseService.GetLowStockProductsAsync(threshold);
                    return JsonSerializer.Serialize(lowStock);

                case "create_product":
                    var newProduct = new Product
                    {
                        ProductName = args.GetProperty("productName").GetString() ?? "Unknown Product"
                    };
                    if (args.TryGetProperty("categoryId", out var catProp))
                        newProduct.CategoryID = catProp.GetInt16();
                    if (args.TryGetProperty("supplierId", out var supProp))
                        newProduct.SupplierID = supProp.GetInt16();
                    if (args.TryGetProperty("unitPrice", out var priceProp))
                        newProduct.UnitPrice = priceProp.GetDecimal();
                    if (args.TryGetProperty("unitsInStock", out var stockProp))
                        newProduct.UnitsInStock = stockProp.GetInt16();
                    if (args.TryGetProperty("quantityPerUnit", out var qtyProp))
                        newProduct.QuantityPerUnit = qtyProp.GetString();

                    var (createdId, createError) = await _databaseService.CreateProductAsync(newProduct);
                    if (createError != null)
                        return $"Error creating product: {createError.Message}";
                    return $"Product created successfully with ID: {createdId}";

                default:
                    return $"Unknown function: {functionName}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return $"Error executing function: {ex.Message}";
        }
    }

    private static string GetSystemPrompt()
    {
        return @"You are a helpful assistant for the Northwind Traders application. You help users manage products, categories, suppliers, customers, and orders.

You have access to the following functions to interact with the database:
- get_all_products: Get all products
- get_product_by_id: Get a specific product
- search_products: Search products by name, category, or supplier
- get_products_by_category: Filter products by category
- get_products_by_price_range: Filter products by price
- get_all_categories: Get all categories
- get_all_suppliers: Get all suppliers
- get_all_customers: Get all customers
- search_customers: Search customers
- get_all_orders: Get all orders
- get_orders_by_customer: Get orders for a specific customer
- get_dashboard_stats: Get summary statistics
- get_low_stock_products: Find products running low on stock
- create_product: Create a new product

When listing items, format them in a readable way using markdown:
- Use **bold** for important information like names and prices
- Use numbered lists (1., 2., etc.) for ordered items
- Use bullet points (- or *) for unordered lists
- Keep responses concise but informative

If asked about something not related to the Northwind database, politely redirect the conversation to topics you can help with.";
    }

    private string GetDummyResponse(string userMessage)
    {
        var lowerMessage = userMessage.ToLower();

        if (lowerMessage.Contains("product"))
        {
            return @"**GenAI services are not deployed**

To enable intelligent chat features, please run the deployment script with GenAI support:

```bash
./deploy-with-chat.sh
```

This will deploy Azure OpenAI and AI Search resources, enabling natural language queries like:
- ""Show me all beverages under $20""
- ""Find products that are low on stock""
- ""What are the top selling categories?""

For now, you can use the **Products** page in the navigation menu to browse and manage products manually.";
        }

        if (lowerMessage.Contains("customer") || lowerMessage.Contains("order"))
        {
            return @"**GenAI services are not deployed**

To enable intelligent chat features, please run:
```bash
./deploy-with-chat.sh
```

You can currently browse customers and orders using the navigation menu above.";
        }

        return @"👋 **Welcome to Northwind Assistant!**

I'm an AI-powered assistant, but the **GenAI services have not been deployed** for this instance.

To enable my full capabilities:
1. Run `./deploy-with-chat.sh` to deploy Azure OpenAI
2. This will enable natural language queries about products, customers, orders, and more

In the meantime, you can use the navigation menu to:
- 📦 Browse **Products** and manage inventory
- 👥 View **Customers** and their details
- 📋 Check **Orders** and their status
- 📊 See **Dashboard** statistics

Would you like to know more about what I can do once GenAI is enabled?";
    }
}
