-- TAO MOI / DAT LAI DU LIEU DEMO: script nay xoa 6 bang hien co.
-- Neu da co du lieu, dung Upgrade_Buoi4.sql va Upgrade_Buoi5_6.sql de giu du lieu.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO
IF DB_ID(N'MinhKhangMiniSupermarketDb') IS NULL
BEGIN
    CREATE DATABASE MinhKhangMiniSupermarketDb;
END
GO

USE MinhKhangMiniSupermarketDb;
GO

IF OBJECT_ID(N'dbo.OrderDetails', N'U') IS NOT NULL DROP TABLE dbo.OrderDetails;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL DROP TABLE dbo.Customers;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users
(
    UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    FullName NVARCHAR(100) NOT NULL CONSTRAINT DF_Users_FullName DEFAULT N'',
    Email NVARCHAR(254) NULL,
    [Password] NVARCHAR(100) NOT NULL,
    [Role] NVARCHAR(30) NOT NULL CONSTRAINT DF_Users_Role DEFAULT N'Cashier',
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    CONSTRAINT UQ_Users_Username UNIQUE (Username)
);
GO

CREATE TABLE dbo.Categories
(
    CategoryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(255) NULL
);
GO

CREATE TABLE dbo.Products
(
    ProductId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    Barcode NVARCHAR(50) NOT NULL,
    ProductName NVARCHAR(150) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    StockQuantity INT NOT NULL CONSTRAINT DF_Products_StockQuantity DEFAULT 0,
    CategoryId INT NOT NULL,
    CONSTRAINT CK_Products_Price_Positive CHECK (Price > 0),
    CONSTRAINT CK_Products_Stock_Nonnegative CHECK (StockQuantity >= 0),
    CONSTRAINT UQ_Products_Barcode UNIQUE (Barcode),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories(CategoryId)
);
GO

CREATE TABLE dbo.Customers
(
    CustomerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    CustomerName NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NULL,
    PhoneNumber VARCHAR(15) NOT NULL,
    [Address] NVARCHAR(200) NOT NULL,
    RewardPoints INT NOT NULL CONSTRAINT DF_Customers_RewardPoints DEFAULT 0,
    MembershipRank NVARCHAR(50) NOT NULL CONSTRAINT DF_Customers_MembershipRank DEFAULT N'Chuẩn',
    CONSTRAINT UQ_Customers_PhoneNumber UNIQUE (PhoneNumber),
    CONSTRAINT CK_Customers_Address_FourParts CHECK ([Address] LIKE '%,%,%,%' AND [Address] NOT LIKE '%,%,%,%,%')
);
GO

CREATE TABLE dbo.Orders
(
    OrderId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    OrderCode VARCHAR(30) NOT NULL CONSTRAINT DF_Orders_OrderCode DEFAULT '',
    RewardPoints INT NOT NULL CONSTRAINT DF_Orders_RewardPoints DEFAULT 0,
    OrderDate DATETIME2 NOT NULL CONSTRAINT DF_Orders_OrderDate DEFAULT SYSDATETIME(),
    CustomerId INT NULL,
    UserId INT NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_TotalAmount DEFAULT 0,
    RedeemedPoints INT NOT NULL CONSTRAINT DF_Orders_RedeemedPoints DEFAULT 0,
    DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_DiscountAmount DEFAULT 0,
    FinalAmount AS (TotalAmount - DiscountAmount) PERSISTED,
    CONSTRAINT CK_Orders_Discount CHECK (DiscountAmount >= 0 AND DiscountAmount <= TotalAmount),
    CONSTRAINT CK_Orders_RedeemedPoints CHECK (RedeemedPoints >= 0),
    [Status] NVARCHAR(30) NOT NULL CONSTRAINT DF_Orders_Status DEFAULT N'Hoàn tất',
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId)
        REFERENCES dbo.Customers(CustomerId)
        ON DELETE SET NULL,
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users(UserId)
);
CREATE UNIQUE INDEX IX_Orders_OrderCode ON dbo.Orders(OrderCode) WHERE OrderCode <> '';
CREATE INDEX IX_Orders_OrderDate ON dbo.Orders(OrderDate);
GO

CREATE TABLE dbo.OrderDetails
(
    OrderDetailId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderDetails PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId)
        REFERENCES dbo.Orders(OrderId)
        ON DELETE CASCADE,
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductId)
        REFERENCES dbo.Products(ProductId)
);
GO

SET IDENTITY_INSERT dbo.Users ON;
-- [Tiêu chí 22] Script SQL nạp dữ liệu mẫu: 20 bản ghi cho mỗi bảng Users, Categories, Products, Customers, Orders và OrderDetails.
INSERT INTO dbo.Users (UserId, Username, FullName, [Password], [Role], IsActive) VALUES
(1, N'admin', N'Quản trị Minh Khang', N'123456', N'Admin', 1),
(2, N'cashier', N'Thu ngân Minh Khang', N'123456', N'Cashier', 1),
(3, N'cashier01', N'Lê Thu Ngân', N'123456', N'Cashier', 1),
(4, N'cashier02', N'Phạm Bán Hàng', N'123456', N'Cashier', 1),
(5, N'cashier03', N'Hoàng Thu Ngân', N'123456', N'Cashier', 1),
(6, N'warehouse01', N'Thủ kho Minh Khang 1', N'123456', N'Warehouse', 1),
(7, N'warehouse02', N'Thủ kho Minh Khang 2', N'123456', N'Warehouse', 1),
(8, N'admin01', N'Nguyễn Quản Trị', N'123456', N'Admin', 1),
(9, N'admin02', N'Trần Giám Đốc', N'123456', N'Admin', 1),
(10, N'cashier04', N'Vũ Thị Quầy', N'123456', N'Cashier', 1),
(11, N'cashier05', N'Đỗ Bán Lẻ', N'123456', N'Cashier', 1),
(12, N'ware01', N'Ngô Quản Kho', N'123456', N'Warehouse', 1),
(13, N'ware02', N'Bùi Kiểm Kê', N'123456', N'Warehouse', 1),
(14, N'ware03', N'Dương Thủ Kho', N'123456', N'Warehouse', 1),
(15, N'ware04', N'Lý Nhập Hàng', N'123456', N'Warehouse', 1),
(16, N'admin_backup', N'Đặng Hỗ Trợ', N'123456', N'Admin', 1),
(17, N'cashier06', N'Hồ Ca Chiều', N'123456', N'Cashier', 1),
(18, N'ware05', N'Trương Vận Chuyển', N'123456', N'Warehouse', 1),
(19, N'inventory01', N'Nhân viên kiểm kê', N'123456', N'Warehouse', 1),
(20, N'supervisor', N'Mai Giám Sát', N'123456', N'Admin', 1);
SET IDENTITY_INSERT dbo.Users OFF;
GO

SET IDENTITY_INSERT dbo.Categories ON;
INSERT INTO dbo.Categories (CategoryId, CategoryName, [Description]) VALUES
(1, N'Bánh kẹo & Đồ ăn vặt', N'Snack, bánh quy, kẹo dẻo'),
(2, N'Nước giải khát & Trà', N'Nước ngọt, nước khoáng, trà'),
(3, N'Sữa & Sản phẩm từ sữa', N'Sữa tươi, sữa chua, phô mai'),
(4, N'Mì gói & Thực phẩm ăn liền', N'Mì ăn liền, phở khô, cháo gói'),
(5, N'Gia vị & Dầu ăn', N'Nước mắm, hạt nêm, dầu thực vật'),
(6, N'Gạo & Ngũ cốc', N'Gạo, yến mạch, bột ngũ cốc'),
(7, N'Thực phẩm đông lạnh', N'Xúc xích, cá viên, thực phẩm cấp đông'),
(8, N'Đồ hộp', N'Cá hộp, pate, thịt hộp'),
(9, N'Rau củ quả', N'Rau xanh, củ quả đóng gói'),
(10, N'Trái cây', N'Trái cây tươi theo mùa'),
(11, N'Chăm sóc cá nhân', N'Dầu gội, sữa tắm, kem đánh răng'),
(12, N'Vệ sinh gia đình', N'Nước rửa chén, bột giặt, lau sàn'),
(13, N'Văn phòng phẩm', N'Bút, vở, giấy ghi chú'),
(14, N'Đồ dùng nhà bếp', N'Ly, hộp nhựa, túi rác'),
(15, N'Thức ăn thú cưng', N'Thức ăn khô và pate cho thú cưng'),
(16, N'Mẹ & bé', N'Tã, khăn giấy, sữa bột'),
(17, N'Dược phẩm cơ bản', N'Khẩu trang, nước muối, bông băng'),
(18, N'Bia & Đồ uống có cồn', N'Bia lon, bia chai'),
(19, N'Kem & Đồ lạnh', N'Kem que, kem hộp, đá viên'),
(20, N'Sản phẩm khuyến mãi', N'Hàng bán nhanh và combo ưu đãi');
SET IDENTITY_INSERT dbo.Categories OFF;
GO

SET IDENTITY_INSERT dbo.Products ON;
INSERT INTO dbo.Products (ProductId, Barcode, ProductName, Price, StockQuantity, CategoryId) VALUES
(1, N'8935001710011', N'Mì ly hải sản Minh Khang', 12000, 120, 4),
(2, N'8934804030012', N'Nước suối 500ml', 6000, 240, 2),
(3, N'8936028940023', N'Sữa tươi hộp 180ml', 9000, 90, 3),
(4, N'8934673100044', N'Bánh quy bơ 150g', 28000, 45, 1),
(5, N'8934563000055', N'Nước mắm chai 500ml', 36000, 35, 5),
(6, N'8935001710066', N'Mì gói bò hầm', 5500, 300, 4),
(7, N'8935001710073', N'Cháo gói thịt bằm', 7500, 160, 4),
(8, N'8934804030081', N'Trà xanh chai 455ml', 11000, 180, 2),
(9, N'8934804030098', N'Nước ngọt cola lon 330ml', 10000, 150, 2),
(10, N'8934804030104', N'Nước tăng lực lon 250ml', 12000, 130, 2),
(11, N'8936028940115', N'Sữa chua có đường hộp', 8000, 100, 3),
(12, N'8936028940122', N'Sữa đặc lon 380g', 27000, 55, 3),
(13, N'8936028940139', N'Phô mai miếng 120g', 42000, 32, 3),
(14, N'8934673100143', N'Kẹo dẻo trái cây 100g', 15000, 80, 1),
(15, N'8934673100150', N'Snack khoai tây 90g', 18000, 95, 1),
(16, N'8934673100167', N'Bánh quy socola 120g', 25000, 60, 1),
(17, N'8934563000178', N'Dầu ăn thực vật 1L', 52000, 42, 5),
(18, N'8934563000185', N'Hạt nêm gói 400g', 39000, 50, 5),
(19, N'8934563000192', N'Đường trắng túi 1kg', 28000, 75, 5),
(20, N'8934563000208', N'Muối i-ốt gói 500g', 8000, 110, 5);
SET IDENTITY_INSERT dbo.Products OFF;
GO

SET IDENTITY_INSERT dbo.Customers ON;
INSERT INTO dbo.Customers (CustomerId, CustomerName, PhoneNumber, [Address], RewardPoints, MembershipRank) VALUES
(1, N'Nguyễn Văn A', '0901122334', N'12, Đường Nguyễn Huệ, Phường Sài Gòn, Thành phố Hồ Chí Minh', 150, N'Vàng'),
(2, N'Trần Thị B', '0918877665', N'34, Đường Lê Lợi, Phường Bến Thành, Thành phố Hồ Chí Minh', 50, N'Bạc'),
(3, N'Lê Văn C', '0983344556', N'56/2, Đường Đinh Tiên Hoàng, Phường Tân Định, Thành phố Hồ Chí Minh', 10, N'Chuẩn'),
(4, N'Phạm Thị D', '0902233445', N'78, Đường Nguyễn Trãi, Phường Chợ Quán, Thành phố Hồ Chí Minh', 220, N'Vàng'),
(5, N'Hoàng Văn E', '0903344556', N'90A, Đường Nguyễn Thị Thập, Phường Tân Mỹ, Thành phố Hồ Chí Minh', 35, N'Bạc'),
(6, N'Đỗ Thị F', '0904455667', N'102, Đường Võ Văn Ngân, Phường Thủ Đức, Thành phố Hồ Chí Minh', 5, N'Chuẩn'),
(7, N'Vũ Văn G', '0905566778', N'114, Đường Thành Thái, Phường Diên Hồng, Thành phố Hồ Chí Minh', 80, N'Bạc'),
(8, N'Bùi Thị H', '0906677889', N'126, Đường Hoàng Diệu, Phường Khánh Hội, Thành phố Hồ Chí Minh', 310, N'Kim cương'),
(9, N'Ngô Văn I', '0907788990', N'138, Đường Quang Trung, Phường Hạnh Thông, Thành phố Hồ Chí Minh', 0, N'Chuẩn'),
(10, N'Đặng Thị K', '0908899001', N'150, Đường Cộng Hòa, Phường Tân Bình, Thành phố Hồ Chí Minh', 120, N'Vàng'),
(11, N'Lý Văn L', '0910001122', N'162/5, Đường Phan Xích Long, Phường Cầu Kiệu, Thành phố Hồ Chí Minh', 45, N'Bạc'),
(12, N'Mai Thị M', '0911112233', N'174, Đường Hậu Giang, Phường Bình Tây, Thành phố Hồ Chí Minh', 15, N'Chuẩn'),
(13, N'Cao Văn N', '0912223344', N'186, Đường Phạm Thế Hiển, Phường Bình Đông, Thành phố Hồ Chí Minh', 65, N'Bạc'),
(14, N'Tạ Thị O', '0913334455', N'198, Đường Tên Lửa, Phường Bình Trị Đông, Thành phố Hồ Chí Minh', 190, N'Vàng'),
(15, N'Chu Văn P', '0914445566', N'210, Đường Huỳnh Tấn Phát, Phường Tân Thuận, Thành phố Hồ Chí Minh', 25, N'Chuẩn'),
(16, N'Trịnh Thị Q', '0915556677', N'222, Đường Lạc Long Quân, Phường Hòa Bình, Thành phố Hồ Chí Minh', 410, N'Kim cương'),
(17, N'Huỳnh Văn R', '0916667788', N'234, Đường Lê Văn Khương, Phường Thới An, Thành phố Hồ Chí Minh', 95, N'Bạc'),
(18, N'Đinh Thị S', '0917778899', N'246, Đường Rừng Sác, Xã Bình Khánh, Thành phố Hồ Chí Minh', 55, N'Bạc'),
(19, N'La Văn T', '0918889900', N'258, Đường số 1, Xã Thạnh An, Thành phố Hồ Chí Minh', 8, N'Chuẩn'),
(20, N'Kiều Thị U', '0919990011', N'270, Đường Lương Định Của, Phường An Khánh, Thành phố Hồ Chí Minh', 260, N'Vàng');
SET IDENTITY_INSERT dbo.Customers OFF;
GO

INSERT INTO dbo.Orders (OrderDate, CustomerId, UserId, TotalAmount, [Status]) VALUES
(SYSDATETIME(), 1, 2, 12000, N'Hoàn tất'),
(SYSDATETIME(), 2, 3, 6000, N'Hoàn tất'),
(SYSDATETIME(), 3, 4, 9000, N'Hoàn tất'),
(SYSDATETIME(), 4, 5, 28000, N'Hoàn tất'),
(SYSDATETIME(), 5, 2, 36000, N'Hoàn tất'),
(SYSDATETIME(), 6, 3, 11000, N'Hoàn tất'),
(SYSDATETIME(), 7, 4, 15000, N'Hoàn tất'),
(SYSDATETIME(), 8, 5, 24000, N'Hoàn tất'),
(SYSDATETIME(), 9, 2, 20000, N'Hoàn tất'),
(SYSDATETIME(), 10, 3, 12000, N'Hoàn tất'),
(SYSDATETIME(), 11, 4, 16000, N'Hoàn tất'),
(SYSDATETIME(), 12, 5, 27000, N'Hoàn tất'),
(SYSDATETIME(), 13, 2, 42000, N'Hoàn tất'),
(SYSDATETIME(), 14, 3, 15000, N'Hoàn tất'),
(SYSDATETIME(), 15, 4, 18000, N'Hoàn tất'),
(SYSDATETIME(), 16, 5, 25000, N'Hoàn tất'),
(SYSDATETIME(), 17, 2, 52000, N'Hoàn tất'),
(SYSDATETIME(), 18, 3, 39000, N'Hoàn tất'),
(SYSDATETIME(), 19, 4, 28000, N'Hoàn tất'),
(SYSDATETIME(), 20, 5, 8000, N'Hoàn tất');
GO

INSERT INTO dbo.OrderDetails (OrderId, ProductId, Quantity, UnitPrice, LineTotal) VALUES
(1, 1, 1, 12000, 12000),
(2, 2, 1, 6000, 6000),
(3, 3, 1, 9000, 9000),
(4, 4, 1, 28000, 28000),
(5, 5, 1, 36000, 36000),
(6, 8, 1, 11000, 11000),
(7, 14, 1, 15000, 15000),
(8, 10, 2, 12000, 24000),
(9, 9, 2, 10000, 20000),
(10, 1, 1, 12000, 12000),
(11, 11, 2, 8000, 16000),
(12, 12, 1, 27000, 27000),
(13, 13, 1, 42000, 42000),
(14, 14, 1, 15000, 15000),
(15, 15, 1, 18000, 18000),
(16, 16, 1, 25000, 25000),
(17, 17, 1, 52000, 52000),
(18, 18, 1, 39000, 39000),
(19, 19, 1, 28000, 28000),
(20, 20, 1, 8000, 8000);
GO

-- [Tiêu chí 34] Các stored procedure báo cáo được C# gọi tại RevenueReportService bằng CommandType.StoredProcedure.
CREATE OR ALTER PROCEDURE dbo.sp_GetRevenueByCashier
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.Username AS CashierUsername, COUNT(*) AS TotalOrders,
        SUM(o.FinalAmount) AS TotalRevenue,
        SUM(CONVERT(BIGINT, o.RewardPoints)) AS TotalPointsAwarded
    FROM dbo.Orders o INNER JOIN dbo.Users u ON u.UserId = o.UserId
    WHERE o.Status = N'Hoàn tất'
    GROUP BY u.Username ORDER BY TotalRevenue DESC, u.Username;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetRevenueByDateRange
    @FromDate DATETIME2, @ToDate DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.Username AS CashierUsername, COUNT(*) AS TotalOrders,
        SUM(o.FinalAmount) AS TotalRevenue,
        SUM(CONVERT(BIGINT, o.RewardPoints)) AS TotalPointsAwarded
    FROM dbo.Orders o INNER JOIN dbo.Users u ON u.UserId = o.UserId
    WHERE o.Status = N'Hoàn tất' AND o.OrderDate >= @FromDate AND o.OrderDate < @ToDate
    GROUP BY u.Username ORDER BY TotalRevenue DESC, u.Username;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetTop5SellingProducts
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (5) p.ProductId, p.ProductName,
        SUM(CONVERT(BIGINT, d.Quantity)) AS TotalQuantity, SUM(d.LineTotal) AS TotalRevenue
    FROM dbo.OrderDetails d INNER JOIN dbo.Products p ON p.ProductId = d.ProductId
    INNER JOIN dbo.Orders o ON o.OrderId = d.OrderId
    WHERE o.Status = N'Hoàn tất'
    GROUP BY p.ProductId, p.ProductName ORDER BY TotalQuantity DESC, p.ProductId;
END;
GO

SELECT N'Đã tạo đủ 6 bảng, 20 dòng mẫu mỗi bảng và 3 stored procedure. Địa chỉ khách hàng đủ số nhà, tên đường, phường/xã, tỉnh/thành phố.' AS Message;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetDailyRevenue @FromDate DATETIME2, @ToDate DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    -- @ToDate là mốc loại trừ; dùng OrderDate đúng tên cột thực tế.
    SELECT CAST(OrderDate AS DATE) AS PeriodStart, COUNT(*) AS TotalOrders,
        SUM(FinalAmount) AS TotalRevenue
    FROM dbo.Orders
    WHERE Status = N'Hoàn tất' AND OrderDate >= @FromDate AND OrderDate < @ToDate
    GROUP BY CAST(OrderDate AS DATE)
    ORDER BY PeriodStart;
END;
GO
