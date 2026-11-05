using Microsoft.EntityFrameworkCore;
using MiniSupermarket.API.Models;

namespace MiniSupermarket.API.Data
{
    public class SupermarketDbContext : DbContext
    {
        public SupermarketDbContext(DbContextOptions<SupermarketDbContext> options)
            : base(options)
        {
        }

        // [Tiêu chí 19] Khai báo đủ 6 DbSet cho User, Category, Product, Customer, Order và OrderDetail để EF Core quản lý các bảng.
        public DbSet<User> Users => Set<User>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasDefaultValue("Cashier");

            modelBuilder.Entity<User>()
                .Property(u => u.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Barcode)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .Property(p => p.StockQuantity)
                .HasDefaultValue(0);

            modelBuilder.Entity<Product>().ToTable("Products", table =>
            {
                table.HasCheckConstraint("CK_Products_Price_Positive", "Price > 0");
                table.HasCheckConstraint("CK_Products_Stock_Nonnegative", "StockQuantity >= 0");
            });

            // [Tiêu chí 19] Quan hệ 1-N Category - Product: mỗi sản phẩm có CategoryId; không xóa nhóm hàng đang có sản phẩm.
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.PhoneNumber)
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .Property(c => c.RewardPoints)
                .HasDefaultValue(0);

            modelBuilder.Entity<Customer>()
                .Property(c => c.MembershipRank)
                .HasDefaultValue("Chuẩn");

            modelBuilder.Entity<Customer>().ToTable("Customers", table => table.HasCheckConstraint(
                "CK_Customers_Address_FourParts", "[Address] LIKE '%,%,%,%' AND [Address] NOT LIKE '%,%,%,%,%'"));

            modelBuilder.Entity<Order>()
                .Property(o => o.OrderDate)
                .HasDefaultValueSql("SYSDATETIME()");

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasDefaultValue(0m);

            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasDefaultValue("Hoàn tất");

            modelBuilder.Entity<Order>().Property(o => o.RedeemedPoints).HasDefaultValue(0);
            modelBuilder.Entity<Order>().Property(o => o.DiscountAmount).HasDefaultValue(0m);
            modelBuilder.Entity<Order>().Property(o => o.FinalAmount)
                .HasComputedColumnSql("[TotalAmount] - [DiscountAmount]", stored: true);
            modelBuilder.Entity<Order>().ToTable("Orders", table =>
            {
                table.HasCheckConstraint("CK_Orders_Discount", "DiscountAmount >= 0 AND DiscountAmount <= TotalAmount");
                table.HasCheckConstraint("CK_Orders_RedeemedPoints", "RedeemedPoints >= 0");
            });
            modelBuilder.Entity<Order>().Property(o => o.OrderCode).HasDefaultValue("");
            modelBuilder.Entity<Order>().Property(o => o.RewardPoints).HasDefaultValue(0);
            modelBuilder.Entity<Order>().HasIndex(o => o.OrderCode).IsUnique().HasFilter("OrderCode <> ''");
            modelBuilder.Entity<Order>().HasIndex(o => o.OrderDate);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // [Tiêu chí 19] Quan hệ 1-N Order - OrderDetail qua OrderId; các khóa ngoại khách hàng, nhân viên và sản phẩm cấu hình cùng phương thức.
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // [Tiêu chí 22] HasData khai báo dữ liệu mồi cho cả 6 bảng, mỗi bảng 20 bản ghi; EF Core đưa seed vào migration khi tạo CSDL.
            modelBuilder.Entity<User>().HasData(
                new User { UserId = 1, Username = "admin", FullName = "Quản trị Minh Khang", Password = "123456", Role = "Admin", IsActive = true },
                new User { UserId = 2, Username = "cashier", FullName = "Thu ngân Minh Khang", Password = "123456", Role = "Cashier", IsActive = true },
                new User { UserId = 3, Username = "cashier01", FullName = "Lê Thu Ngân", Password = "123456", Role = "Cashier", IsActive = true },
                new User { UserId = 4, Username = "cashier02", FullName = "Phạm Bán Hàng", Password = "123456", Role = "Cashier", IsActive = true },
                new User { UserId = 5, Username = "cashier03", FullName = "Hoàng Thu Ngân", Password = "123456", Role = "Cashier", IsActive = true },
                new User { UserId = 6, Username = "warehouse01", FullName = "Thủ kho Minh Khang 1", Password = "123456", Role = "Warehouse", IsActive = true },
                new User { UserId = 7, Username = "warehouse02", FullName = "Thủ kho Minh Khang 2", Password = "123456", Role = "Warehouse", IsActive = true },
                new User { UserId = 8, Username = "admin01", FullName = "Nguyễn Quản Trị", Password = "123456", Role = "Admin", IsActive = true },
                new User { UserId = 9, Username = "admin02", FullName = "Trần Giám Đốc", Password = "123456", Role = "Admin", IsActive = true },
                new User { UserId = 10, Username = "cashier04", FullName = "Vũ Thị Quầy", Password = "123456", Role = "Cashier", IsActive = true },
                new User { UserId = 11, Username = "cashier05", FullName = "Đỗ Bán Lẻ", Password = "123456", Role = "Cashier", IsActive = true },
                new User { UserId = 12, Username = "ware01", FullName = "Ngô Quản Kho", Password = "123456", Role = "Warehouse", IsActive = true },
                new User { UserId = 13, Username = "ware02", FullName = "Bùi Kiểm Kê", Password = "123456", Role = "Warehouse", IsActive = true },
                new User { UserId = 14, Username = "ware03", FullName = "Dương Thủ Kho", Password = "123456", Role = "Warehouse", IsActive = true },
                new User { UserId = 15, Username = "ware04", FullName = "Lý Nhập Hàng", Password = "123456", Role = "Warehouse", IsActive = true },
                new User { UserId = 16, Username = "admin_backup", FullName = "Đặng Hỗ Trợ", Password = "123456", Role = "Admin", IsActive = true },
                new User { UserId = 17, Username = "cashier06", FullName = "Hồ Ca Chiều", Password = "123456", Role = "Cashier", IsActive = true },
                new User { UserId = 18, Username = "ware05", FullName = "Trương Vận Chuyển", Password = "123456", Role = "Warehouse", IsActive = true },
                new User { UserId = 19, Username = "inventory01", FullName = "Nhân viên kiểm kê", Password = "123456", Role = "Warehouse", IsActive = true },
                new User { UserId = 20, Username = "supervisor", FullName = "Mai Giám Sát", Password = "123456", Role = "Admin", IsActive = true }
            );

            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, CategoryName = "Bánh kẹo & Đồ ăn vặt", Description = "Snack, bánh quy, kẹo dẻo" },
                new Category { CategoryId = 2, CategoryName = "Nước giải khát & Trà", Description = "Nước ngọt, nước khoáng, trà" },
                new Category { CategoryId = 3, CategoryName = "Sữa & Sản phẩm từ sữa", Description = "Sữa tươi, sữa chua, phô mai" },
                new Category { CategoryId = 4, CategoryName = "Mì gói & Thực phẩm ăn liền", Description = "Mì ăn liền, phở khô, cháo gói" },
                new Category { CategoryId = 5, CategoryName = "Gia vị & Dầu ăn", Description = "Nước mắm, hạt nêm, dầu thực vật" },
                new Category { CategoryId = 6, CategoryName = "Gạo & Ngũ cốc", Description = "Gạo, yến mạch, bột ngũ cốc" },
                new Category { CategoryId = 7, CategoryName = "Thực phẩm đông lạnh", Description = "Xúc xích, cá viên, thực phẩm cấp đông" },
                new Category { CategoryId = 8, CategoryName = "Đồ hộp", Description = "Cá hộp, pate, thịt hộp" },
                new Category { CategoryId = 9, CategoryName = "Rau củ quả", Description = "Rau xanh, củ quả đóng gói" },
                new Category { CategoryId = 10, CategoryName = "Trái cây", Description = "Trái cây tươi theo mùa" },
                new Category { CategoryId = 11, CategoryName = "Chăm sóc cá nhân", Description = "Dầu gội, sữa tắm, kem đánh răng" },
                new Category { CategoryId = 12, CategoryName = "Vệ sinh gia đình", Description = "Nước rửa chén, bột giặt, lau sàn" },
                new Category { CategoryId = 13, CategoryName = "Văn phòng phẩm", Description = "Bút, vở, giấy ghi chú" },
                new Category { CategoryId = 14, CategoryName = "Đồ dùng nhà bếp", Description = "Ly, hộp nhựa, túi rác" },
                new Category { CategoryId = 15, CategoryName = "Thức ăn thú cưng", Description = "Thức ăn khô và pate cho thú cưng" },
                new Category { CategoryId = 16, CategoryName = "Mẹ & bé", Description = "Tã, khăn giấy, sữa bột" },
                new Category { CategoryId = 17, CategoryName = "Dược phẩm cơ bản", Description = "Khẩu trang, nước muối, bông băng" },
                new Category { CategoryId = 18, CategoryName = "Bia & Đồ uống có cồn", Description = "Bia lon, bia chai" },
                new Category { CategoryId = 19, CategoryName = "Kem & Đồ lạnh", Description = "Kem que, kem hộp, đá viên" },
                new Category { CategoryId = 20, CategoryName = "Sản phẩm khuyến mãi", Description = "Hàng bán nhanh và combo ưu đãi" }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { ProductId = 1, Barcode = "8935001710011", ProductName = "Mì ly hải sản Minh Khang", Price = 12000m, StockQuantity = 120, CategoryId = 4 },
                new Product { ProductId = 2, Barcode = "8934804030012", ProductName = "Nước suối 500ml", Price = 6000m, StockQuantity = 240, CategoryId = 2 },
                new Product { ProductId = 3, Barcode = "8936028940023", ProductName = "Sữa tươi hộp 180ml", Price = 9000m, StockQuantity = 90, CategoryId = 3 },
                new Product { ProductId = 4, Barcode = "8934673100044", ProductName = "Bánh quy bơ 150g", Price = 28000m, StockQuantity = 45, CategoryId = 1 },
                new Product { ProductId = 5, Barcode = "8934563000055", ProductName = "Nước mắm chai 500ml", Price = 36000m, StockQuantity = 35, CategoryId = 5 },
                new Product { ProductId = 6, Barcode = "8935001710066", ProductName = "Mì gói bò hầm", Price = 5500m, StockQuantity = 300, CategoryId = 4 },
                new Product { ProductId = 7, Barcode = "8935001710073", ProductName = "Cháo gói thịt bằm", Price = 7500m, StockQuantity = 160, CategoryId = 4 },
                new Product { ProductId = 8, Barcode = "8934804030081", ProductName = "Trà xanh chai 455ml", Price = 11000m, StockQuantity = 180, CategoryId = 2 },
                new Product { ProductId = 9, Barcode = "8934804030098", ProductName = "Nước ngọt cola lon 330ml", Price = 10000m, StockQuantity = 150, CategoryId = 2 },
                new Product { ProductId = 10, Barcode = "8934804030104", ProductName = "Nước tăng lực lon 250ml", Price = 12000m, StockQuantity = 130, CategoryId = 2 },
                new Product { ProductId = 11, Barcode = "8936028940115", ProductName = "Sữa chua có đường hộp", Price = 8000m, StockQuantity = 100, CategoryId = 3 },
                new Product { ProductId = 12, Barcode = "8936028940122", ProductName = "Sữa đặc lon 380g", Price = 27000m, StockQuantity = 55, CategoryId = 3 },
                new Product { ProductId = 13, Barcode = "8936028940139", ProductName = "Phô mai miếng 120g", Price = 42000m, StockQuantity = 32, CategoryId = 3 },
                new Product { ProductId = 14, Barcode = "8934673100143", ProductName = "Kẹo dẻo trái cây 100g", Price = 15000m, StockQuantity = 80, CategoryId = 1 },
                new Product { ProductId = 15, Barcode = "8934673100150", ProductName = "Snack khoai tây 90g", Price = 18000m, StockQuantity = 95, CategoryId = 1 },
                new Product { ProductId = 16, Barcode = "8934673100167", ProductName = "Bánh quy socola 120g", Price = 25000m, StockQuantity = 60, CategoryId = 1 },
                new Product { ProductId = 17, Barcode = "8934563000178", ProductName = "Dầu ăn thực vật 1L", Price = 52000m, StockQuantity = 42, CategoryId = 5 },
                new Product { ProductId = 18, Barcode = "8934563000185", ProductName = "Hạt nêm gói 400g", Price = 39000m, StockQuantity = 50, CategoryId = 5 },
                new Product { ProductId = 19, Barcode = "8934563000192", ProductName = "Đường trắng túi 1kg", Price = 28000m, StockQuantity = 75, CategoryId = 5 },
                new Product { ProductId = 20, Barcode = "8934563000208", ProductName = "Muối i-ốt gói 500g", Price = 8000m, StockQuantity = 110, CategoryId = 5 }
            );

            modelBuilder.Entity<Customer>().HasData(
                new Customer { CustomerId = 1, CustomerName = "Nguyễn Văn A", PhoneNumber = "0901122334", Address = "12, Đường Nguyễn Huệ, Phường Sài Gòn, Thành phố Hồ Chí Minh", MembershipRank = "Vàng", RewardPoints = 150 },
                new Customer { CustomerId = 2, CustomerName = "Trần Thị B", PhoneNumber = "0918877665", Address = "34, Đường Lê Lợi, Phường Bến Thành, Thành phố Hồ Chí Minh", MembershipRank = "Bạc", RewardPoints = 50 },
                new Customer { CustomerId = 3, CustomerName = "Lê Văn C", PhoneNumber = "0983344556", Address = "56/2, Đường Đinh Tiên Hoàng, Phường Tân Định, Thành phố Hồ Chí Minh", MembershipRank = "Chuẩn", RewardPoints = 10 },
                new Customer { CustomerId = 4, CustomerName = "Phạm Thị D", PhoneNumber = "0902233445", Address = "78, Đường Nguyễn Trãi, Phường Chợ Quán, Thành phố Hồ Chí Minh", MembershipRank = "Vàng", RewardPoints = 220 },
                new Customer { CustomerId = 5, CustomerName = "Hoàng Văn E", PhoneNumber = "0903344556", Address = "90A, Đường Nguyễn Thị Thập, Phường Tân Mỹ, Thành phố Hồ Chí Minh", MembershipRank = "Bạc", RewardPoints = 35 },
                new Customer { CustomerId = 6, CustomerName = "Đỗ Thị F", PhoneNumber = "0904455667", Address = "102, Đường Võ Văn Ngân, Phường Thủ Đức, Thành phố Hồ Chí Minh", MembershipRank = "Chuẩn", RewardPoints = 5 },
                new Customer { CustomerId = 7, CustomerName = "Vũ Văn G", PhoneNumber = "0905566778", Address = "114, Đường Thành Thái, Phường Diên Hồng, Thành phố Hồ Chí Minh", MembershipRank = "Bạc", RewardPoints = 80 },
                new Customer { CustomerId = 8, CustomerName = "Bùi Thị H", PhoneNumber = "0906677889", Address = "126, Đường Hoàng Diệu, Phường Khánh Hội, Thành phố Hồ Chí Minh", MembershipRank = "Kim cương", RewardPoints = 310 },
                new Customer { CustomerId = 9, CustomerName = "Ngô Văn I", PhoneNumber = "0907788990", Address = "138, Đường Quang Trung, Phường Hạnh Thông, Thành phố Hồ Chí Minh", MembershipRank = "Chuẩn", RewardPoints = 0 },
                new Customer { CustomerId = 10, CustomerName = "Đặng Thị K", PhoneNumber = "0908899001", Address = "150, Đường Cộng Hòa, Phường Tân Bình, Thành phố Hồ Chí Minh", MembershipRank = "Vàng", RewardPoints = 120 },
                new Customer { CustomerId = 11, CustomerName = "Lý Văn L", PhoneNumber = "0910001122", Address = "162/5, Đường Phan Xích Long, Phường Cầu Kiệu, Thành phố Hồ Chí Minh", MembershipRank = "Bạc", RewardPoints = 45 },
                new Customer { CustomerId = 12, CustomerName = "Mai Thị M", PhoneNumber = "0911112233", Address = "174, Đường Hậu Giang, Phường Bình Tây, Thành phố Hồ Chí Minh", MembershipRank = "Chuẩn", RewardPoints = 15 },
                new Customer { CustomerId = 13, CustomerName = "Cao Văn N", PhoneNumber = "0912223344", Address = "186, Đường Phạm Thế Hiển, Phường Bình Đông, Thành phố Hồ Chí Minh", MembershipRank = "Bạc", RewardPoints = 65 },
                new Customer { CustomerId = 14, CustomerName = "Tạ Thị O", PhoneNumber = "0913334455", Address = "198, Đường Tên Lửa, Phường Bình Trị Đông, Thành phố Hồ Chí Minh", MembershipRank = "Vàng", RewardPoints = 190 },
                new Customer { CustomerId = 15, CustomerName = "Chu Văn P", PhoneNumber = "0914445566", Address = "210, Đường Huỳnh Tấn Phát, Phường Tân Thuận, Thành phố Hồ Chí Minh", MembershipRank = "Chuẩn", RewardPoints = 25 },
                new Customer { CustomerId = 16, CustomerName = "Trịnh Thị Q", PhoneNumber = "0915556677", Address = "222, Đường Lạc Long Quân, Phường Hòa Bình, Thành phố Hồ Chí Minh", MembershipRank = "Kim cương", RewardPoints = 410 },
                new Customer { CustomerId = 17, CustomerName = "Huỳnh Văn R", PhoneNumber = "0916667788", Address = "234, Đường Lê Văn Khương, Phường Thới An, Thành phố Hồ Chí Minh", MembershipRank = "Bạc", RewardPoints = 95 },
                new Customer { CustomerId = 18, CustomerName = "Đinh Thị S", PhoneNumber = "0917778899", Address = "246, Đường Rừng Sác, Xã Bình Khánh, Thành phố Hồ Chí Minh", MembershipRank = "Bạc", RewardPoints = 55 },
                new Customer { CustomerId = 19, CustomerName = "La Văn T", PhoneNumber = "0918889900", Address = "258, Đường số 1, Xã Thạnh An, Thành phố Hồ Chí Minh", MembershipRank = "Chuẩn", RewardPoints = 8 },
                new Customer { CustomerId = 20, CustomerName = "Kiều Thị U", PhoneNumber = "0919990011", Address = "270, Đường Lương Định Của, Phường An Khánh, Thành phố Hồ Chí Minh", MembershipRank = "Vàng", RewardPoints = 260 }
            );

            modelBuilder.Entity<Order>().HasData(
                new Order { OrderId = 1, OrderDate = new DateTime(2026, 10, 8, 8, 0, 0), CustomerId = 1, UserId = 2, TotalAmount = 12000m, Status = "Hoàn tất" },
                new Order { OrderId = 2, OrderDate = new DateTime(2026, 10, 8, 8, 15, 0), CustomerId = 2, UserId = 3, TotalAmount = 6000m, Status = "Hoàn tất" },
                new Order { OrderId = 3, OrderDate = new DateTime(2026, 10, 8, 8, 30, 0), CustomerId = 3, UserId = 4, TotalAmount = 9000m, Status = "Hoàn tất" },
                new Order { OrderId = 4, OrderDate = new DateTime(2026, 10, 8, 8, 45, 0), CustomerId = 4, UserId = 5, TotalAmount = 28000m, Status = "Hoàn tất" },
                new Order { OrderId = 5, OrderDate = new DateTime(2026, 10, 8, 9, 0, 0), CustomerId = 5, UserId = 2, TotalAmount = 36000m, Status = "Hoàn tất" },
                new Order { OrderId = 6, OrderDate = new DateTime(2026, 10, 8, 9, 15, 0), CustomerId = 6, UserId = 3, TotalAmount = 11000m, Status = "Hoàn tất" },
                new Order { OrderId = 7, OrderDate = new DateTime(2026, 10, 8, 9, 30, 0), CustomerId = 7, UserId = 4, TotalAmount = 15000m, Status = "Hoàn tất" },
                new Order { OrderId = 8, OrderDate = new DateTime(2026, 10, 8, 9, 45, 0), CustomerId = 8, UserId = 5, TotalAmount = 24000m, Status = "Hoàn tất" },
                new Order { OrderId = 9, OrderDate = new DateTime(2026, 10, 8, 10, 0, 0), CustomerId = 9, UserId = 2, TotalAmount = 20000m, Status = "Hoàn tất" },
                new Order { OrderId = 10, OrderDate = new DateTime(2026, 10, 8, 10, 15, 0), CustomerId = 10, UserId = 3, TotalAmount = 12000m, Status = "Hoàn tất" },
                new Order { OrderId = 11, OrderDate = new DateTime(2026, 10, 8, 10, 30, 0), CustomerId = 11, UserId = 4, TotalAmount = 16000m, Status = "Hoàn tất" },
                new Order { OrderId = 12, OrderDate = new DateTime(2026, 10, 8, 10, 45, 0), CustomerId = 12, UserId = 5, TotalAmount = 27000m, Status = "Hoàn tất" },
                new Order { OrderId = 13, OrderDate = new DateTime(2026, 10, 8, 11, 0, 0), CustomerId = 13, UserId = 2, TotalAmount = 42000m, Status = "Hoàn tất" },
                new Order { OrderId = 14, OrderDate = new DateTime(2026, 10, 8, 11, 15, 0), CustomerId = 14, UserId = 3, TotalAmount = 15000m, Status = "Hoàn tất" },
                new Order { OrderId = 15, OrderDate = new DateTime(2026, 10, 8, 11, 30, 0), CustomerId = 15, UserId = 4, TotalAmount = 18000m, Status = "Hoàn tất" },
                new Order { OrderId = 16, OrderDate = new DateTime(2026, 10, 8, 11, 45, 0), CustomerId = 16, UserId = 5, TotalAmount = 25000m, Status = "Hoàn tất" },
                new Order { OrderId = 17, OrderDate = new DateTime(2026, 10, 8, 12, 0, 0), CustomerId = 17, UserId = 2, TotalAmount = 52000m, Status = "Hoàn tất" },
                new Order { OrderId = 18, OrderDate = new DateTime(2026, 10, 8, 12, 15, 0), CustomerId = 18, UserId = 3, TotalAmount = 39000m, Status = "Hoàn tất" },
                new Order { OrderId = 19, OrderDate = new DateTime(2026, 10, 8, 12, 30, 0), CustomerId = 19, UserId = 4, TotalAmount = 28000m, Status = "Hoàn tất" },
                new Order { OrderId = 20, OrderDate = new DateTime(2026, 10, 8, 12, 45, 0), CustomerId = 20, UserId = 5, TotalAmount = 8000m, Status = "Hoàn tất" }
            );

            modelBuilder.Entity<OrderDetail>().HasData(
                new OrderDetail { OrderDetailId = 1, OrderId = 1, ProductId = 1, Quantity = 1, UnitPrice = 12000m, LineTotal = 12000m },
                new OrderDetail { OrderDetailId = 2, OrderId = 2, ProductId = 2, Quantity = 1, UnitPrice = 6000m, LineTotal = 6000m },
                new OrderDetail { OrderDetailId = 3, OrderId = 3, ProductId = 3, Quantity = 1, UnitPrice = 9000m, LineTotal = 9000m },
                new OrderDetail { OrderDetailId = 4, OrderId = 4, ProductId = 4, Quantity = 1, UnitPrice = 28000m, LineTotal = 28000m },
                new OrderDetail { OrderDetailId = 5, OrderId = 5, ProductId = 5, Quantity = 1, UnitPrice = 36000m, LineTotal = 36000m },
                new OrderDetail { OrderDetailId = 6, OrderId = 6, ProductId = 8, Quantity = 1, UnitPrice = 11000m, LineTotal = 11000m },
                new OrderDetail { OrderDetailId = 7, OrderId = 7, ProductId = 14, Quantity = 1, UnitPrice = 15000m, LineTotal = 15000m },
                new OrderDetail { OrderDetailId = 8, OrderId = 8, ProductId = 10, Quantity = 2, UnitPrice = 12000m, LineTotal = 24000m },
                new OrderDetail { OrderDetailId = 9, OrderId = 9, ProductId = 9, Quantity = 2, UnitPrice = 10000m, LineTotal = 20000m },
                new OrderDetail { OrderDetailId = 10, OrderId = 10, ProductId = 1, Quantity = 1, UnitPrice = 12000m, LineTotal = 12000m },
                new OrderDetail { OrderDetailId = 11, OrderId = 11, ProductId = 11, Quantity = 2, UnitPrice = 8000m, LineTotal = 16000m },
                new OrderDetail { OrderDetailId = 12, OrderId = 12, ProductId = 12, Quantity = 1, UnitPrice = 27000m, LineTotal = 27000m },
                new OrderDetail { OrderDetailId = 13, OrderId = 13, ProductId = 13, Quantity = 1, UnitPrice = 42000m, LineTotal = 42000m },
                new OrderDetail { OrderDetailId = 14, OrderId = 14, ProductId = 14, Quantity = 1, UnitPrice = 15000m, LineTotal = 15000m },
                new OrderDetail { OrderDetailId = 15, OrderId = 15, ProductId = 15, Quantity = 1, UnitPrice = 18000m, LineTotal = 18000m },
                new OrderDetail { OrderDetailId = 16, OrderId = 16, ProductId = 16, Quantity = 1, UnitPrice = 25000m, LineTotal = 25000m },
                new OrderDetail { OrderDetailId = 17, OrderId = 17, ProductId = 17, Quantity = 1, UnitPrice = 52000m, LineTotal = 52000m },
                new OrderDetail { OrderDetailId = 18, OrderId = 18, ProductId = 18, Quantity = 1, UnitPrice = 39000m, LineTotal = 39000m },
                new OrderDetail { OrderDetailId = 19, OrderId = 19, ProductId = 19, Quantity = 1, UnitPrice = 28000m, LineTotal = 28000m },
                new OrderDetail { OrderDetailId = 20, OrderId = 20, ProductId = 20, Quantity = 1, UnitPrice = 8000m, LineTotal = 8000m }
            );
        }
    }
}
