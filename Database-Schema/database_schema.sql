-- Northwind schema for Microsoft SQL Server

-- Drop tables if they exist (order matters due to foreign keys)
IF OBJECT_ID('dbo.EmployeeTerritories', 'U') IS NOT NULL DROP TABLE dbo.EmployeeTerritories;
IF OBJECT_ID('dbo.Territories', 'U') IS NOT NULL DROP TABLE dbo.Territories;
IF OBJECT_ID('dbo.Region', 'U') IS NOT NULL DROP TABLE dbo.Region;
IF OBJECT_ID('dbo.OrderDetails', 'U') IS NOT NULL DROP TABLE dbo.OrderDetails;
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Suppliers', 'U') IS NOT NULL DROP TABLE dbo.Suppliers;
IF OBJECT_ID('dbo.Shippers', 'U') IS NOT NULL DROP TABLE dbo.Shippers;
IF OBJECT_ID('dbo.Employees', 'U') IS NOT NULL DROP TABLE dbo.Employees;
IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL DROP TABLE dbo.Customers;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;

-- Categories table
CREATE TABLE Categories (
    CategoryID SMALLINT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(15) NOT NULL,
    Description NVARCHAR(MAX),
    Picture VARBINARY(MAX)
);

-- Customers table
CREATE TABLE Customers (
    CustomerID CHAR(5) PRIMARY KEY,
    CompanyName NVARCHAR(40) NOT NULL,
    ContactName NVARCHAR(30),
    ContactTitle NVARCHAR(30),
    Address NVARCHAR(60),
    City NVARCHAR(15),
    Region NVARCHAR(15),
    PostalCode NVARCHAR(10),
    Country NVARCHAR(15),
    Phone NVARCHAR(24),
    Fax NVARCHAR(24)
);

-- Employees table
CREATE TABLE Employees (
    EmployeeID SMALLINT IDENTITY(1,1) PRIMARY KEY,
    LastName NVARCHAR(20) NOT NULL,
    FirstName NVARCHAR(10) NOT NULL,
    Title NVARCHAR(30),
    TitleOfCourtesy NVARCHAR(25),
    BirthDate DATE,
    HireDate DATE,
    Address NVARCHAR(60),
    City NVARCHAR(15),
    Region NVARCHAR(15),
    PostalCode NVARCHAR(10),
    Country NVARCHAR(15),
    HomePhone NVARCHAR(24),
    Extension NVARCHAR(4),
    Photo VARBINARY(MAX),
    Notes NVARCHAR(MAX),
    ReportsTo SMALLINT NULL,
    PhotoPath NVARCHAR(255),
    CONSTRAINT FK_Employees_ReportsTo FOREIGN KEY (ReportsTo) REFERENCES Employees(EmployeeID)
);

-- Suppliers table
CREATE TABLE Suppliers (
    SupplierID SMALLINT IDENTITY(1,1) PRIMARY KEY,
    CompanyName NVARCHAR(40) NOT NULL,
    ContactName NVARCHAR(30),
    ContactTitle NVARCHAR(30),
    Address NVARCHAR(60),
    City NVARCHAR(15),
    Region NVARCHAR(15),
    PostalCode NVARCHAR(10),
    Country NVARCHAR(15),
    Phone NVARCHAR(24),
    Fax NVARCHAR(24),
    HomePage NVARCHAR(MAX)
);

-- Shippers table
CREATE TABLE Shippers (
    ShipperID SMALLINT IDENTITY(1,1) PRIMARY KEY,
    CompanyName NVARCHAR(40) NOT NULL,
    Phone NVARCHAR(24)
);

-- Products table
CREATE TABLE Products (
    ProductID SMALLINT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(40) NOT NULL,
    SupplierID SMALLINT NULL,
    CategoryID SMALLINT NULL,
    QuantityPerUnit NVARCHAR(20),
    UnitPrice MONEY,
    UnitsInStock SMALLINT,
    UnitsOnOrder SMALLINT,
    ReorderLevel SMALLINT,
    Discontinued BIT NOT NULL,
    CONSTRAINT FK_Products_Supplier FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID),
    CONSTRAINT FK_Products_Category FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);

-- Orders table
CREATE TABLE Orders (
    OrderID SMALLINT IDENTITY(1,1) PRIMARY KEY,
    CustomerID CHAR(5) NULL,
    EmployeeID SMALLINT NULL,
    OrderDate DATE,
    RequiredDate DATE,
    ShippedDate DATE,
    ShipVia SMALLINT NULL,
    Freight MONEY,
    ShipName NVARCHAR(40),
    ShipAddress NVARCHAR(60),
    ShipCity NVARCHAR(15),
    ShipRegion NVARCHAR(15),
    ShipPostalCode NVARCHAR(10),
    ShipCountry NVARCHAR(15),
    CONSTRAINT FK_Orders_Customer FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID),
    CONSTRAINT FK_Orders_Employee FOREIGN KEY (EmployeeID) REFERENCES Employees(EmployeeID),
    CONSTRAINT FK_Orders_Shipper FOREIGN KEY (ShipVia) REFERENCES Shippers(ShipperID)
);

-- OrderDetails table
CREATE TABLE OrderDetails (
    OrderID SMALLINT NOT NULL,
    ProductID SMALLINT NOT NULL,
    UnitPrice MONEY NOT NULL,
    Quantity SMALLINT NOT NULL,
    Discount REAL NOT NULL,
    PRIMARY KEY (OrderID, ProductID),
    CONSTRAINT FK_OrderDetails_Order FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),
    CONSTRAINT FK_OrderDetails_Product FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

-- Region table
CREATE TABLE Region (
    RegionID SMALLINT PRIMARY KEY,
    RegionDescription NCHAR(50) NOT NULL
);

-- Territories table
CREATE TABLE Territories (
    TerritoryID NVARCHAR(20) PRIMARY KEY,
    TerritoryDescription NCHAR(50) NOT NULL,
    RegionID SMALLINT NOT NULL,
    CONSTRAINT FK_Territories_Region FOREIGN KEY (RegionID) REFERENCES Region(RegionID)
);

-- EmployeeTerritories table
CREATE TABLE EmployeeTerritories (
    EmployeeID SMALLINT NOT NULL,
    TerritoryID NVARCHAR(20) NOT NULL,
    PRIMARY KEY (EmployeeID, TerritoryID),
    CONSTRAINT FK_EmployeeTerritories_Employee FOREIGN KEY (EmployeeID) REFERENCES Employees(EmployeeID),
    CONSTRAINT FK_EmployeeTerritories_Territory FOREIGN KEY (TerritoryID) REFERENCES Territories(TerritoryID)
);
