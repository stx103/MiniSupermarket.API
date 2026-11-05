using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MiniSupermarket.API.Migrations
{
    /// <inheritdoc />
    // [Tiêu chí 21] EF Core Migrations: Add-Migration <TenMigration> sinh thay đổi cấu trúc; Update-Database áp dụng các migration vào SQL Server.
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        // [Tiêu chí 21] Up tạo bảng, khóa ngoại và seed ban đầu; các migration tiếp theo cập nhật thủ tục báo cáo và chuẩn hóa địa chỉ.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RewardPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MembershipRank = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Chuẩn")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Cashier"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductId);
                    table.CheckConstraint("CK_Products_Price_Positive", "Price > 0");
                    table.CheckConstraint("CK_Products_Stock_Nonnegative", "StockQuantity >= 0");
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderCode = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: ""),
                    RewardPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Hoàn tất")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderDetails",
                columns: table => new
                {
                    OrderDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetails", x => x.OrderDetailId);
                    table.ForeignKey(
                        name: "FK_OrderDetails_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "CategoryName", "Description" },
                values: new object[,]
                {
                    { 1, "Bánh kẹo & Đồ ăn vặt", "Snack, bánh quy, kẹo dẻo" },
                    { 2, "Nước giải khát & Trà", "Nước ngọt, nước khoáng, trà" },
                    { 3, "Sữa & Sản phẩm từ sữa", "Sữa tươi, sữa chua, phô mai" },
                    { 4, "Mì gói & Thực phẩm ăn liền", "Mì ăn liền, phở khô, cháo gói" },
                    { 5, "Gia vị & Dầu ăn", "Nước mắm, hạt nêm, dầu thực vật" },
                    { 6, "Gạo & Ngũ cốc", "Gạo, yến mạch, bột ngũ cốc" },
                    { 7, "Thực phẩm đông lạnh", "Xúc xích, cá viên, thực phẩm cấp đông" },
                    { 8, "Đồ hộp", "Cá hộp, pate, thịt hộp" },
                    { 9, "Rau củ quả", "Rau xanh, củ quả đóng gói" },
                    { 10, "Trái cây", "Trái cây tươi theo mùa" },
                    { 11, "Chăm sóc cá nhân", "Dầu gội, sữa tắm, kem đánh răng" },
                    { 12, "Vệ sinh gia đình", "Nước rửa chén, bột giặt, lau sàn" },
                    { 13, "Văn phòng phẩm", "Bút, vở, giấy ghi chú" },
                    { 14, "Đồ dùng nhà bếp", "Ly, hộp nhựa, túi rác" },
                    { 15, "Thức ăn thú cưng", "Thức ăn khô và pate cho thú cưng" },
                    { 16, "Mẹ & bé", "Tã, khăn giấy, sữa bột" },
                    { 17, "Dược phẩm cơ bản", "Khẩu trang, nước muối, bông băng" },
                    { 18, "Bia & Đồ uống có cồn", "Bia lon, bia chai" },
                    { 19, "Kem & Đồ lạnh", "Kem que, kem hộp, đá viên" },
                    { 20, "Sản phẩm khuyến mãi", "Hàng bán nhanh và combo ưu đãi" }
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "CustomerId", "Address", "CustomerName", "MembershipRank", "PhoneNumber", "RewardPoints" },
                values: new object[,]
                {
                    { 1, "Quận 1, TP.HCM", "Nguyễn Văn A", "Vàng", "0901122334", 150 },
                    { 2, "Quận 3, TP.HCM", "Trần Thị B", "Bạc", "0918877665", 50 },
                    { 3, "Bình Thạnh, TP.HCM", "Lê Văn C", "Chuẩn", "0983344556", 10 },
                    { 4, "Quận 5, TP.HCM", "Phạm Thị D", "Vàng", "0902233445", 220 },
                    { 5, "Quận 7, TP.HCM", "Hoàng Văn E", "Bạc", "0903344556", 35 },
                    { 6, "Thủ Đức, TP.HCM", "Đỗ Thị F", "Chuẩn", "0904455667", 5 },
                    { 7, "Quận 10, TP.HCM", "Vũ Văn G", "Bạc", "0905566778", 80 },
                    { 8, "Quận 4, TP.HCM", "Bùi Thị H", "Kim cương", "0906677889", 310 }
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "CustomerId", "Address", "CustomerName", "MembershipRank", "PhoneNumber" },
                values: new object[] { 9, "Gò Vấp, TP.HCM", "Ngô Văn I", "Chuẩn", "0907788990" });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "CustomerId", "Address", "CustomerName", "MembershipRank", "PhoneNumber", "RewardPoints" },
                values: new object[,]
                {
                    { 10, "Tân Bình, TP.HCM", "Đặng Thị K", "Vàng", "0908899001", 120 },
                    { 11, "Phú Nhuận, TP.HCM", "Lý Văn L", "Bạc", "0910001122", 45 },
                    { 12, "Quận 6, TP.HCM", "Mai Thị M", "Chuẩn", "0911112233", 15 },
                    { 13, "Quận 8, TP.HCM", "Cao Văn N", "Bạc", "0912223344", 65 },
                    { 14, "Bình Tân, TP.HCM", "Tạ Thị O", "Vàng", "0913334455", 190 },
                    { 15, "Nhà Bè, TP.HCM", "Chu Văn P", "Chuẩn", "0914445566", 25 },
                    { 16, "Quận 11, TP.HCM", "Trịnh Thị Q", "Kim cương", "0915556677", 410 },
                    { 17, "Quận 12, TP.HCM", "Huỳnh Văn R", "Bạc", "0916667788", 95 },
                    { 18, "Củ Chi, TP.HCM", "Đinh Thị S", "Bạc", "0917778899", 55 },
                    { 19, "Hóc Môn, TP.HCM", "La Văn T", "Chuẩn", "0918889900", 8 },
                    { 20, "Quận 2, TP.HCM", "Kiều Thị U", "Vàng", "0919990011", 260 }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "Email", "FullName", "IsActive", "Password", "Role", "Username" },
                values: new object[,]
                {
                    { 1, null, "Quản trị Minh Khang", true, "123456", "Admin", "admin" },
                    { 2, null, "Thu ngân Minh Khang", true, "123456", "Cashier", "cashier" },
                    { 3, null, "Lê Thu Ngân", true, "123456", "Cashier", "cashier01" },
                    { 4, null, "Phạm Bán Hàng", true, "123456", "Cashier", "cashier02" },
                    { 5, null, "Hoàng Thu Ngân", true, "123456", "Cashier", "cashier03" },
                    { 6, null, "Thủ kho Minh Khang 1", true, "123456", "Warehouse", "warehouse01" },
                    { 7, null, "Thủ kho Minh Khang 2", true, "123456", "Warehouse", "warehouse02" },
                    { 8, null, "Nguyễn Quản Trị", true, "123456", "Admin", "admin01" },
                    { 9, null, "Trần Giám Đốc", true, "123456", "Admin", "admin02" },
                    { 10, null, "Vũ Thị Quầy", true, "123456", "Cashier", "cashier04" },
                    { 11, null, "Đỗ Bán Lẻ", true, "123456", "Cashier", "cashier05" },
                    { 12, null, "Ngô Quản Kho", true, "123456", "Warehouse", "ware01" },
                    { 13, null, "Bùi Kiểm Kê", true, "123456", "Warehouse", "ware02" },
                    { 14, null, "Dương Thủ Kho", true, "123456", "Warehouse", "ware03" },
                    { 15, null, "Lý Nhập Hàng", true, "123456", "Warehouse", "ware04" },
                    { 16, null, "Đặng Hỗ Trợ", true, "123456", "Admin", "admin_backup" },
                    { 17, null, "Hồ Ca Chiều", true, "123456", "Cashier", "cashier06" },
                    { 18, null, "Trương Vận Chuyển", true, "123456", "Warehouse", "ware05" },
                    { 19, null, "Nhân viên kiểm kê", true, "123456", "Warehouse", "inventory01" },
                    { 20, null, "Mai Giám Sát", true, "123456", "Admin", "supervisor" }
                });

            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "OrderId", "CustomerId", "OrderCode", "OrderDate", "Status", "TotalAmount", "UserId" },
                values: new object[,]
                {
                    { 1, 1, "", new DateTime(2026, 10, 8, 8, 0, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 12000m, 2 },
                    { 2, 2, "", new DateTime(2026, 10, 8, 8, 15, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 6000m, 3 },
                    { 3, 3, "", new DateTime(2026, 10, 8, 8, 30, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 9000m, 4 },
                    { 4, 4, "", new DateTime(2026, 10, 8, 8, 45, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 28000m, 5 },
                    { 5, 5, "", new DateTime(2026, 10, 8, 9, 0, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 36000m, 2 },
                    { 6, 6, "", new DateTime(2026, 10, 8, 9, 15, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 11000m, 3 },
                    { 7, 7, "", new DateTime(2026, 10, 8, 9, 30, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 15000m, 4 },
                    { 8, 8, "", new DateTime(2026, 10, 8, 9, 45, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 24000m, 5 },
                    { 9, 9, "", new DateTime(2026, 10, 8, 10, 0, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 20000m, 2 },
                    { 10, 10, "", new DateTime(2026, 10, 8, 10, 15, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 12000m, 3 },
                    { 11, 11, "", new DateTime(2026, 10, 8, 10, 30, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 16000m, 4 },
                    { 12, 12, "", new DateTime(2026, 10, 8, 10, 45, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 27000m, 5 },
                    { 13, 13, "", new DateTime(2026, 10, 8, 11, 0, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 42000m, 2 },
                    { 14, 14, "", new DateTime(2026, 10, 8, 11, 15, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 15000m, 3 },
                    { 15, 15, "", new DateTime(2026, 10, 8, 11, 30, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 18000m, 4 },
                    { 16, 16, "", new DateTime(2026, 10, 8, 11, 45, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 25000m, 5 },
                    { 17, 17, "", new DateTime(2026, 10, 8, 12, 0, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 52000m, 2 },
                    { 18, 18, "", new DateTime(2026, 10, 8, 12, 15, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 39000m, 3 },
                    { 19, 19, "", new DateTime(2026, 10, 8, 12, 30, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 28000m, 4 },
                    { 20, 20, "", new DateTime(2026, 10, 8, 12, 45, 0, 0, DateTimeKind.Unspecified), "Hoàn tất", 8000m, 5 }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "ProductId", "Barcode", "CategoryId", "Price", "ProductName", "StockQuantity" },
                values: new object[,]
                {
                    { 1, "8935001710011", 4, 12000m, "Mì ly hải sản Minh Khang", 120 },
                    { 2, "8934804030012", 2, 6000m, "Nước suối 500ml", 240 },
                    { 3, "8936028940023", 3, 9000m, "Sữa tươi hộp 180ml", 90 },
                    { 4, "8934673100044", 1, 28000m, "Bánh quy bơ 150g", 45 },
                    { 5, "8934563000055", 5, 36000m, "Nước mắm chai 500ml", 35 },
                    { 6, "8935001710066", 4, 5500m, "Mì gói bò hầm", 300 },
                    { 7, "8935001710073", 4, 7500m, "Cháo gói thịt bằm", 160 },
                    { 8, "8934804030081", 2, 11000m, "Trà xanh chai 455ml", 180 },
                    { 9, "8934804030098", 2, 10000m, "Nước ngọt cola lon 330ml", 150 },
                    { 10, "8934804030104", 2, 12000m, "Nước tăng lực lon 250ml", 130 },
                    { 11, "8936028940115", 3, 8000m, "Sữa chua có đường hộp", 100 },
                    { 12, "8936028940122", 3, 27000m, "Sữa đặc lon 380g", 55 },
                    { 13, "8936028940139", 3, 42000m, "Phô mai miếng 120g", 32 },
                    { 14, "8934673100143", 1, 15000m, "Kẹo dẻo trái cây 100g", 80 },
                    { 15, "8934673100150", 1, 18000m, "Snack khoai tây 90g", 95 },
                    { 16, "8934673100167", 1, 25000m, "Bánh quy socola 120g", 60 },
                    { 17, "8934563000178", 5, 52000m, "Dầu ăn thực vật 1L", 42 },
                    { 18, "8934563000185", 5, 39000m, "Hạt nêm gói 400g", 50 },
                    { 19, "8934563000192", 5, 28000m, "Đường trắng túi 1kg", 75 },
                    { 20, "8934563000208", 5, 8000m, "Muối i-ốt gói 500g", 110 }
                });

            migrationBuilder.InsertData(
                table: "OrderDetails",
                columns: new[] { "OrderDetailId", "LineTotal", "OrderId", "ProductId", "Quantity", "UnitPrice" },
                values: new object[,]
                {
                    { 1, 12000m, 1, 1, 1, 12000m },
                    { 2, 6000m, 2, 2, 1, 6000m },
                    { 3, 9000m, 3, 3, 1, 9000m },
                    { 4, 28000m, 4, 4, 1, 28000m },
                    { 5, 36000m, 5, 5, 1, 36000m },
                    { 6, 11000m, 6, 8, 1, 11000m },
                    { 7, 15000m, 7, 14, 1, 15000m },
                    { 8, 24000m, 8, 10, 2, 12000m },
                    { 9, 20000m, 9, 9, 2, 10000m },
                    { 10, 12000m, 10, 1, 1, 12000m },
                    { 11, 16000m, 11, 11, 2, 8000m },
                    { 12, 27000m, 12, 12, 1, 27000m },
                    { 13, 42000m, 13, 13, 1, 42000m },
                    { 14, 15000m, 14, 14, 1, 15000m },
                    { 15, 18000m, 15, 15, 1, 18000m },
                    { 16, 25000m, 16, 16, 1, 25000m },
                    { 17, 52000m, 17, 17, 1, 52000m },
                    { 18, 39000m, 18, 18, 1, 39000m },
                    { 19, 28000m, 19, 19, 1, 28000m },
                    { 20, 8000m, 20, 20, 1, 8000m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PhoneNumber",
                table: "Customers",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_OrderId",
                table: "OrderDetails",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_ProductId",
                table: "OrderDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderCode",
                table: "Orders",
                column: "OrderCode",
                unique: true,
                filter: "OrderCode <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderDate",
                table: "Orders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderDetails");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
