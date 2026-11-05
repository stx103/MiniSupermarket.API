-- Nang cap CSDL Buoi 3; khong xoa bang, san pham, khach hang hay hoa don.
-- Co the chay lai; chi them cot va tai khoan mau con thieu.
USE MinhKhangMiniSupermarketDb;
GO

IF COL_LENGTH(N'dbo.Users', N'FullName') IS NULL
    ALTER TABLE dbo.Users ADD FullName NVARCHAR(100) NOT NULL
        CONSTRAINT DF_Users_FullName DEFAULT N'';
IF COL_LENGTH(N'dbo.Users', N'Email') IS NULL
    ALTER TABLE dbo.Users ADD Email NVARCHAR(254) NULL;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @Users TABLE (Username NVARCHAR(50), FullName NVARCHAR(100), [Role] NVARCHAR(30));
INSERT INTO @Users VALUES
(N'admin', N'Quản trị Minh Khang', N'Admin'),
(N'cashier', N'Thu ngân Minh Khang', N'Cashier'),
(N'cashier01', N'Lê Thu Ngân', N'Cashier'),
(N'cashier02', N'Phạm Bán Hàng', N'Cashier'),
(N'cashier03', N'Hoàng Thu Ngân', N'Cashier'),
(N'warehouse01', N'Thủ kho Minh Khang 1', N'Warehouse'),
(N'warehouse02', N'Thủ kho Minh Khang 2', N'Warehouse'),
(N'admin01', N'Nguyễn Quản Trị', N'Admin'),
(N'admin02', N'Trần Giám Đốc', N'Admin'),
(N'cashier04', N'Vũ Thị Quầy', N'Cashier'),
(N'cashier05', N'Đỗ Bán Lẻ', N'Cashier'),
(N'ware01', N'Ngô Quản Kho', N'Warehouse'),
(N'ware02', N'Bùi Kiểm Kê', N'Warehouse'),
(N'ware03', N'Dương Thủ Kho', N'Warehouse'),
(N'ware04', N'Lý Nhập Hàng', N'Warehouse'),
(N'admin_backup', N'Đặng Hỗ Trợ', N'Admin'),
(N'cashier06', N'Hồ Ca Chiều', N'Cashier'),
(N'ware05', N'Trương Vận Chuyển', N'Warehouse'),
(N'inventory01', N'Nhân viên kiểm kê', N'Warehouse'),
(N'supervisor', N'Mai Giám Sát', N'Admin');

UPDATE u SET FullName = s.FullName
FROM dbo.Users u INNER JOIN @Users s ON s.Username = u.Username
WHERE LTRIM(RTRIM(u.FullName)) = N'';

INSERT INTO dbo.Users (Username, FullName, [Password], [Role], IsActive)
SELECT s.Username, s.FullName, N'123456', s.[Role], 1
FROM @Users s WHERE NOT EXISTS (SELECT 1 FROM dbo.Users u WHERE u.Username = s.Username);

UPDATE dbo.Users SET FullName = Username WHERE LTRIM(RTRIM(FullName)) = N'';
COMMIT;
GO

SELECT UserId, Username, FullName, Email, [Role], IsActive FROM dbo.Users ORDER BY UserId;
GO
