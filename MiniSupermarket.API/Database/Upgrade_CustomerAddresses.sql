-- Nang cap dia chi mau, khong xoa bang/hoa don, khong doi diem hay ton kho.
-- CSDL do EF quan ly: dung dotnet ef database update thay vi script nay.
USE MinhKhangMiniSupermarketDb;
GO
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    UPDATE c SET [Address] = d.NewAddress
    FROM dbo.Customers c
    INNER JOIN (VALUES
        (1, N'0901122334', N'Quận 1, TP.HCM', N'12, Đường Nguyễn Huệ, Phường Sài Gòn, Thành phố Hồ Chí Minh'),
        (2, N'0918877665', N'Quận 3, TP.HCM', N'34, Đường Lê Lợi, Phường Bến Thành, Thành phố Hồ Chí Minh'),
        (3, N'0983344556', N'Bình Thạnh, TP.HCM', N'56/2, Đường Đinh Tiên Hoàng, Phường Tân Định, Thành phố Hồ Chí Minh'),
        (4, N'0902233445', N'Quận 5, TP.HCM', N'78, Đường Nguyễn Trãi, Phường Chợ Quán, Thành phố Hồ Chí Minh'),
        (5, N'0903344556', N'Quận 7, TP.HCM', N'90A, Đường Nguyễn Thị Thập, Phường Tân Mỹ, Thành phố Hồ Chí Minh'),
        (6, N'0904455667', N'Thủ Đức, TP.HCM', N'102, Đường Võ Văn Ngân, Phường Thủ Đức, Thành phố Hồ Chí Minh'),
        (7, N'0905566778', N'Quận 10, TP.HCM', N'114, Đường Thành Thái, Phường Diên Hồng, Thành phố Hồ Chí Minh'),
        (8, N'0906677889', N'Quận 4, TP.HCM', N'126, Đường Hoàng Diệu, Phường Khánh Hội, Thành phố Hồ Chí Minh'),
        (9, N'0907788990', N'Gò Vấp, TP.HCM', N'138, Đường Quang Trung, Phường Hạnh Thông, Thành phố Hồ Chí Minh'),
        (10, N'0908899001', N'Tân Bình, TP.HCM', N'150, Đường Cộng Hòa, Phường Tân Bình, Thành phố Hồ Chí Minh'),
        (11, N'0910001122', N'Phú Nhuận, TP.HCM', N'162/5, Đường Phan Xích Long, Phường Cầu Kiệu, Thành phố Hồ Chí Minh'),
        (12, N'0911112233', N'Quận 6, TP.HCM', N'174, Đường Hậu Giang, Phường Bình Tây, Thành phố Hồ Chí Minh'),
        (13, N'0912223344', N'Quận 8, TP.HCM', N'186, Đường Phạm Thế Hiển, Phường Bình Đông, Thành phố Hồ Chí Minh'),
        (14, N'0913334455', N'Bình Tân, TP.HCM', N'198, Đường Tên Lửa, Phường Bình Trị Đông, Thành phố Hồ Chí Minh'),
        (15, N'0914445566', N'Nhà Bè, TP.HCM', N'210, Đường Huỳnh Tấn Phát, Phường Tân Thuận, Thành phố Hồ Chí Minh'),
        (16, N'0915556677', N'Quận 11, TP.HCM', N'222, Đường Lạc Long Quân, Phường Hòa Bình, Thành phố Hồ Chí Minh'),
        (17, N'0916667788', N'Quận 12, TP.HCM', N'234, Đường Lê Văn Khương, Phường Thới An, Thành phố Hồ Chí Minh'),
        (18, N'0917778899', N'Củ Chi, TP.HCM', N'246, Đường Rừng Sác, Xã Bình Khánh, Thành phố Hồ Chí Minh'),
        (19, N'0918889900', N'Hóc Môn, TP.HCM', N'258, Đường số 1, Xã Thạnh An, Thành phố Hồ Chí Minh'),
        (20, N'0919990011', N'Quận 2, TP.HCM', N'270, Đường Lương Định Của, Phường An Khánh, Thành phố Hồ Chí Minh')
    ) d(CustomerId, PhoneNumber, OldAddress, NewAddress)
        ON c.CustomerId = d.CustomerId AND c.PhoneNumber = d.PhoneNumber
    WHERE c.[Address] = d.OldAddress OR NULLIF(LTRIM(RTRIM(c.[Address])), N'') IS NULL;

    IF EXISTS (SELECT 1 FROM dbo.Customers WHERE [Address] IS NULL
        OR [Address] NOT LIKE '%,%,%,%' OR [Address] LIKE '%,%,%,%,%')
        THROW 50011, 'Update missing or malformed custom customer addresses before upgrading.', 1;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Customers')
        AND name = N'Address' AND is_nullable = 1)
        ALTER TABLE dbo.Customers ALTER COLUMN [Address] NVARCHAR(200) NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.Customers') AND name = N'CK_Customers_Address_FourParts')
        ALTER TABLE dbo.Customers WITH CHECK ADD CONSTRAINT CK_Customers_Address_FourParts
            CHECK ([Address] LIKE '%,%,%,%' AND [Address] NOT LIKE '%,%,%,%,%');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
SELECT CustomerId, CustomerName, [Address] FROM dbo.Customers ORDER BY CustomerId;
GO
