-- Stored Procedures for Northwind Database
-- All app code uses these stored procedures to interact with the database

-- =====================================================
-- Products Stored Procedures
-- =====================================================

CREATE OR ALTER PROCEDURE sp_GetAllProducts
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.ProductID, p.ProductName, p.SupplierID, p.CategoryID, 
           p.QuantityPerUnit, p.UnitPrice, p.UnitsInStock, p.UnitsOnOrder,
           p.ReorderLevel, p.Discontinued,
           c.CategoryName, s.CompanyName AS SupplierName
    FROM Products p
    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
    LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
    ORDER BY p.ProductName;
END
GO

CREATE OR ALTER PROCEDURE sp_GetProductById
    @ProductID SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.ProductID, p.ProductName, p.SupplierID, p.CategoryID, 
           p.QuantityPerUnit, p.UnitPrice, p.UnitsInStock, p.UnitsOnOrder,
           p.ReorderLevel, p.Discontinued,
           c.CategoryName, s.CompanyName AS SupplierName
    FROM Products p
    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
    LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
    WHERE p.ProductID = @ProductID;
END
GO

CREATE OR ALTER PROCEDURE sp_GetProductsByCategory
    @CategoryID SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.ProductID, p.ProductName, p.SupplierID, p.CategoryID, 
           p.QuantityPerUnit, p.UnitPrice, p.UnitsInStock, p.UnitsOnOrder,
           p.ReorderLevel, p.Discontinued,
           c.CategoryName, s.CompanyName AS SupplierName
    FROM Products p
    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
    LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
    WHERE p.CategoryID = @CategoryID
    ORDER BY p.ProductName;
END
GO

CREATE OR ALTER PROCEDURE sp_GetProductsByPriceRange
    @MinPrice MONEY,
    @MaxPrice MONEY
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.ProductID, p.ProductName, p.SupplierID, p.CategoryID, 
           p.QuantityPerUnit, p.UnitPrice, p.UnitsInStock, p.UnitsOnOrder,
           p.ReorderLevel, p.Discontinued,
           c.CategoryName, s.CompanyName AS SupplierName
    FROM Products p
    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
    LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
    WHERE p.UnitPrice BETWEEN @MinPrice AND @MaxPrice
    ORDER BY p.UnitPrice;
END
GO

CREATE OR ALTER PROCEDURE sp_SearchProducts
    @SearchTerm NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.ProductID, p.ProductName, p.SupplierID, p.CategoryID, 
           p.QuantityPerUnit, p.UnitPrice, p.UnitsInStock, p.UnitsOnOrder,
           p.ReorderLevel, p.Discontinued,
           c.CategoryName, s.CompanyName AS SupplierName
    FROM Products p
    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
    LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
    WHERE p.ProductName LIKE '%' + @SearchTerm + '%'
       OR c.CategoryName LIKE '%' + @SearchTerm + '%'
       OR s.CompanyName LIKE '%' + @SearchTerm + '%'
    ORDER BY p.ProductName;
END
GO

CREATE OR ALTER PROCEDURE sp_CreateProduct
    @ProductName NVARCHAR(40),
    @SupplierID SMALLINT = NULL,
    @CategoryID SMALLINT = NULL,
    @QuantityPerUnit NVARCHAR(20) = NULL,
    @UnitPrice MONEY = NULL,
    @UnitsInStock SMALLINT = 0,
    @UnitsOnOrder SMALLINT = 0,
    @ReorderLevel SMALLINT = 0,
    @Discontinued BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Products (ProductName, SupplierID, CategoryID, QuantityPerUnit, 
                          UnitPrice, UnitsInStock, UnitsOnOrder, ReorderLevel, Discontinued)
    VALUES (@ProductName, @SupplierID, @CategoryID, @QuantityPerUnit, 
            @UnitPrice, @UnitsInStock, @UnitsOnOrder, @ReorderLevel, @Discontinued);
    
    SELECT SCOPE_IDENTITY() AS ProductID;
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateProduct
    @ProductID SMALLINT,
    @ProductName NVARCHAR(40),
    @SupplierID SMALLINT = NULL,
    @CategoryID SMALLINT = NULL,
    @QuantityPerUnit NVARCHAR(20) = NULL,
    @UnitPrice MONEY = NULL,
    @UnitsInStock SMALLINT = 0,
    @UnitsOnOrder SMALLINT = 0,
    @ReorderLevel SMALLINT = 0,
    @Discontinued BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Products
    SET ProductName = @ProductName,
        SupplierID = @SupplierID,
        CategoryID = @CategoryID,
        QuantityPerUnit = @QuantityPerUnit,
        UnitPrice = @UnitPrice,
        UnitsInStock = @UnitsInStock,
        UnitsOnOrder = @UnitsOnOrder,
        ReorderLevel = @ReorderLevel,
        Discontinued = @Discontinued
    WHERE ProductID = @ProductID;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

CREATE OR ALTER PROCEDURE sp_DeleteProduct
    @ProductID SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check for dependent order details
    IF EXISTS (SELECT 1 FROM OrderDetails WHERE ProductID = @ProductID)
    BEGIN
        RAISERROR('Cannot delete product. It is referenced in order details.', 16, 1);
        RETURN;
    END
    
    DELETE FROM Products WHERE ProductID = @ProductID;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =====================================================
-- Categories Stored Procedures
-- =====================================================

CREATE OR ALTER PROCEDURE sp_GetAllCategories
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryID, CategoryName, Description
    FROM Categories
    ORDER BY CategoryName;
END
GO

CREATE OR ALTER PROCEDURE sp_GetCategoryById
    @CategoryID SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryID, CategoryName, Description
    FROM Categories
    WHERE CategoryID = @CategoryID;
END
GO

CREATE OR ALTER PROCEDURE sp_CreateCategory
    @CategoryName NVARCHAR(15),
    @Description NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Categories (CategoryName, Description)
    VALUES (@CategoryName, @Description);
    
    SELECT SCOPE_IDENTITY() AS CategoryID;
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateCategory
    @CategoryID SMALLINT,
    @CategoryName NVARCHAR(15),
    @Description NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Categories
    SET CategoryName = @CategoryName,
        Description = @Description
    WHERE CategoryID = @CategoryID;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

CREATE OR ALTER PROCEDURE sp_DeleteCategory
    @CategoryID SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Categories WHERE CategoryID = @CategoryID;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =====================================================
-- Suppliers Stored Procedures
-- =====================================================

CREATE OR ALTER PROCEDURE sp_GetAllSuppliers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SupplierID, CompanyName, ContactName, ContactTitle, Address,
           City, Region, PostalCode, Country, Phone, Fax, HomePage
    FROM Suppliers
    ORDER BY CompanyName;
END
GO

CREATE OR ALTER PROCEDURE sp_GetSupplierById
    @SupplierID SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SupplierID, CompanyName, ContactName, ContactTitle, Address,
           City, Region, PostalCode, Country, Phone, Fax, HomePage
    FROM Suppliers
    WHERE SupplierID = @SupplierID;
END
GO

CREATE OR ALTER PROCEDURE sp_CreateSupplier
    @CompanyName NVARCHAR(40),
    @ContactName NVARCHAR(30) = NULL,
    @ContactTitle NVARCHAR(30) = NULL,
    @Address NVARCHAR(60) = NULL,
    @City NVARCHAR(15) = NULL,
    @Region NVARCHAR(15) = NULL,
    @PostalCode NVARCHAR(10) = NULL,
    @Country NVARCHAR(15) = NULL,
    @Phone NVARCHAR(24) = NULL,
    @Fax NVARCHAR(24) = NULL,
    @HomePage NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Suppliers (CompanyName, ContactName, ContactTitle, Address, City, Region, 
                           PostalCode, Country, Phone, Fax, HomePage)
    VALUES (@CompanyName, @ContactName, @ContactTitle, @Address, @City, @Region,
            @PostalCode, @Country, @Phone, @Fax, @HomePage);
    
    SELECT SCOPE_IDENTITY() AS SupplierID;
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateSupplier
    @SupplierID SMALLINT,
    @CompanyName NVARCHAR(40),
    @ContactName NVARCHAR(30) = NULL,
    @ContactTitle NVARCHAR(30) = NULL,
    @Address NVARCHAR(60) = NULL,
    @City NVARCHAR(15) = NULL,
    @Region NVARCHAR(15) = NULL,
    @PostalCode NVARCHAR(10) = NULL,
    @Country NVARCHAR(15) = NULL,
    @Phone NVARCHAR(24) = NULL,
    @Fax NVARCHAR(24) = NULL,
    @HomePage NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Suppliers
    SET CompanyName = @CompanyName,
        ContactName = @ContactName,
        ContactTitle = @ContactTitle,
        Address = @Address,
        City = @City,
        Region = @Region,
        PostalCode = @PostalCode,
        Country = @Country,
        Phone = @Phone,
        Fax = @Fax,
        HomePage = @HomePage
    WHERE SupplierID = @SupplierID;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

CREATE OR ALTER PROCEDURE sp_DeleteSupplier
    @SupplierID SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Suppliers WHERE SupplierID = @SupplierID;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =====================================================
-- Customers Stored Procedures
-- =====================================================

CREATE OR ALTER PROCEDURE sp_GetAllCustomers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CustomerID, CompanyName, ContactName, ContactTitle, Address,
           City, Region, PostalCode, Country, Phone, Fax
    FROM Customers
    ORDER BY CompanyName;
END
GO

CREATE OR ALTER PROCEDURE sp_GetCustomerById
    @CustomerID CHAR(5)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CustomerID, CompanyName, ContactName, ContactTitle, Address,
           City, Region, PostalCode, Country, Phone, Fax
    FROM Customers
    WHERE CustomerID = @CustomerID;
END
GO

CREATE OR ALTER PROCEDURE sp_SearchCustomers
    @SearchTerm NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CustomerID, CompanyName, ContactName, ContactTitle, Address,
           City, Region, PostalCode, Country, Phone, Fax
    FROM Customers
    WHERE CompanyName LIKE '%' + @SearchTerm + '%'
       OR ContactName LIKE '%' + @SearchTerm + '%'
       OR City LIKE '%' + @SearchTerm + '%'
    ORDER BY CompanyName;
END
GO

CREATE OR ALTER PROCEDURE sp_CreateCustomer
    @CustomerID CHAR(5),
    @CompanyName NVARCHAR(40),
    @ContactName NVARCHAR(30) = NULL,
    @ContactTitle NVARCHAR(30) = NULL,
    @Address NVARCHAR(60) = NULL,
    @City NVARCHAR(15) = NULL,
    @Region NVARCHAR(15) = NULL,
    @PostalCode NVARCHAR(10) = NULL,
    @Country NVARCHAR(15) = NULL,
    @Phone NVARCHAR(24) = NULL,
    @Fax NVARCHAR(24) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Customers (CustomerID, CompanyName, ContactName, ContactTitle, Address, 
                           City, Region, PostalCode, Country, Phone, Fax)
    VALUES (@CustomerID, @CompanyName, @ContactName, @ContactTitle, @Address,
            @City, @Region, @PostalCode, @Country, @Phone, @Fax);
    
    SELECT @CustomerID AS CustomerID;
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateCustomer
    @CustomerID CHAR(5),
    @CompanyName NVARCHAR(40),
    @ContactName NVARCHAR(30) = NULL,
    @ContactTitle NVARCHAR(30) = NULL,
    @Address NVARCHAR(60) = NULL,
    @City NVARCHAR(15) = NULL,
    @Region NVARCHAR(15) = NULL,
    @PostalCode NVARCHAR(10) = NULL,
    @Country NVARCHAR(15) = NULL,
    @Phone NVARCHAR(24) = NULL,
    @Fax NVARCHAR(24) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Customers
    SET CompanyName = @CompanyName,
        ContactName = @ContactName,
        ContactTitle = @ContactTitle,
        Address = @Address,
        City = @City,
        Region = @Region,
        PostalCode = @PostalCode,
        Country = @Country,
        Phone = @Phone,
        Fax = @Fax
    WHERE CustomerID = @CustomerID;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

CREATE OR ALTER PROCEDURE sp_DeleteCustomer
    @CustomerID CHAR(5)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Customers WHERE CustomerID = @CustomerID;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =====================================================
-- Orders Stored Procedures
-- =====================================================

CREATE OR ALTER PROCEDURE sp_GetAllOrders
AS
BEGIN
    SET NOCOUNT ON;
    SELECT o.OrderID, o.CustomerID, o.EmployeeID, o.OrderDate, o.RequiredDate,
           o.ShippedDate, o.ShipVia, o.Freight, o.ShipName, o.ShipAddress,
           o.ShipCity, o.ShipRegion, o.ShipPostalCode, o.ShipCountry,
           c.CompanyName AS CustomerName,
           e.FirstName + ' ' + e.LastName AS EmployeeName,
           s.CompanyName AS ShipperName
    FROM Orders o
    LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
    LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
    LEFT JOIN Shippers s ON o.ShipVia = s.ShipperID
    ORDER BY o.OrderDate DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_GetOrderById
    @OrderID SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT o.OrderID, o.CustomerID, o.EmployeeID, o.OrderDate, o.RequiredDate,
           o.ShippedDate, o.ShipVia, o.Freight, o.ShipName, o.ShipAddress,
           o.ShipCity, o.ShipRegion, o.ShipPostalCode, o.ShipCountry,
           c.CompanyName AS CustomerName,
           e.FirstName + ' ' + e.LastName AS EmployeeName,
           s.CompanyName AS ShipperName
    FROM Orders o
    LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
    LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
    LEFT JOIN Shippers s ON o.ShipVia = s.ShipperID
    WHERE o.OrderID = @OrderID;
END
GO

CREATE OR ALTER PROCEDURE sp_GetOrdersByCustomer
    @CustomerID CHAR(5)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT o.OrderID, o.CustomerID, o.EmployeeID, o.OrderDate, o.RequiredDate,
           o.ShippedDate, o.ShipVia, o.Freight, o.ShipName, o.ShipAddress,
           o.ShipCity, o.ShipRegion, o.ShipPostalCode, o.ShipCountry,
           c.CompanyName AS CustomerName,
           e.FirstName + ' ' + e.LastName AS EmployeeName,
           s.CompanyName AS ShipperName
    FROM Orders o
    LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
    LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
    LEFT JOIN Shippers s ON o.ShipVia = s.ShipperID
    WHERE o.CustomerID = @CustomerID
    ORDER BY o.OrderDate DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_GetOrderDetails
    @OrderID SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT od.OrderID, od.ProductID, od.UnitPrice, od.Quantity, od.Discount,
           p.ProductName,
           (od.UnitPrice * od.Quantity * (1 - od.Discount)) AS ExtendedPrice
    FROM OrderDetails od
    LEFT JOIN Products p ON od.ProductID = p.ProductID
    WHERE od.OrderID = @OrderID;
END
GO

CREATE OR ALTER PROCEDURE sp_CreateOrder
    @CustomerID CHAR(5) = NULL,
    @EmployeeID SMALLINT = NULL,
    @OrderDate DATE = NULL,
    @RequiredDate DATE = NULL,
    @ShippedDate DATE = NULL,
    @ShipVia SMALLINT = NULL,
    @Freight MONEY = NULL,
    @ShipName NVARCHAR(40) = NULL,
    @ShipAddress NVARCHAR(60) = NULL,
    @ShipCity NVARCHAR(15) = NULL,
    @ShipRegion NVARCHAR(15) = NULL,
    @ShipPostalCode NVARCHAR(10) = NULL,
    @ShipCountry NVARCHAR(15) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Orders (CustomerID, EmployeeID, OrderDate, RequiredDate, ShippedDate,
                        ShipVia, Freight, ShipName, ShipAddress, ShipCity, ShipRegion,
                        ShipPostalCode, ShipCountry)
    VALUES (@CustomerID, @EmployeeID, ISNULL(@OrderDate, GETDATE()), @RequiredDate, @ShippedDate,
            @ShipVia, @Freight, @ShipName, @ShipAddress, @ShipCity, @ShipRegion,
            @ShipPostalCode, @ShipCountry);
    
    SELECT SCOPE_IDENTITY() AS OrderID;
END
GO

CREATE OR ALTER PROCEDURE sp_DeleteOrder
    @OrderID SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        -- Delete order details first
        DELETE FROM OrderDetails WHERE OrderID = @OrderID;
        -- Then delete the order
        DELETE FROM Orders WHERE OrderID = @OrderID;
        COMMIT TRANSACTION;
        SELECT @@ROWCOUNT AS RowsAffected;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- =====================================================
-- Employees Stored Procedures
-- =====================================================

CREATE OR ALTER PROCEDURE sp_GetAllEmployees
AS
BEGIN
    SET NOCOUNT ON;
    SELECT EmployeeID, LastName, FirstName, Title, TitleOfCourtesy,
           BirthDate, HireDate, Address, City, Region, PostalCode, Country,
           HomePhone, Extension, Notes, ReportsTo, PhotoPath
    FROM Employees
    ORDER BY LastName, FirstName;
END
GO

CREATE OR ALTER PROCEDURE sp_GetEmployeeById
    @EmployeeID SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT EmployeeID, LastName, FirstName, Title, TitleOfCourtesy,
           BirthDate, HireDate, Address, City, Region, PostalCode, Country,
           HomePhone, Extension, Notes, ReportsTo, PhotoPath
    FROM Employees
    WHERE EmployeeID = @EmployeeID;
END
GO

-- =====================================================
-- Shippers Stored Procedures
-- =====================================================

CREATE OR ALTER PROCEDURE sp_GetAllShippers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ShipperID, CompanyName, Phone
    FROM Shippers
    ORDER BY CompanyName;
END
GO

-- =====================================================
-- Dashboard/Statistics Stored Procedures
-- =====================================================

CREATE OR ALTER PROCEDURE sp_GetDashboardStats
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        (SELECT COUNT(*) FROM Products) AS TotalProducts,
        (SELECT COUNT(*) FROM Products WHERE Discontinued = 0) AS ActiveProducts,
        (SELECT COUNT(*) FROM Categories) AS TotalCategories,
        (SELECT COUNT(*) FROM Suppliers) AS TotalSuppliers,
        (SELECT COUNT(*) FROM Customers) AS TotalCustomers,
        (SELECT COUNT(*) FROM Orders) AS TotalOrders,
        (SELECT COUNT(*) FROM Employees) AS TotalEmployees;
END
GO

CREATE OR ALTER PROCEDURE sp_GetLowStockProducts
    @Threshold SMALLINT = 10
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.ProductID, p.ProductName, p.UnitsInStock, p.ReorderLevel,
           c.CategoryName, s.CompanyName AS SupplierName
    FROM Products p
    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
    LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
    WHERE p.UnitsInStock <= @Threshold AND p.Discontinued = 0
    ORDER BY p.UnitsInStock;
END
GO
