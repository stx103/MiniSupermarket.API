-- Dung cho CSDL da tao bang SQL thu cong. Khong xoa hay sua du lieu cu.
USE MinhKhangMiniSupermarketDb;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Products') AND name = N'CK_Products_Price_Positive')
    ALTER TABLE dbo.Products WITH CHECK ADD CONSTRAINT CK_Products_Price_Positive CHECK (Price > 0);
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Products') AND name = N'CK_Products_Stock_Nonnegative')
    ALTER TABLE dbo.Products WITH CHECK ADD CONSTRAINT CK_Products_Stock_Nonnegative CHECK (StockQuantity >= 0);
GO
