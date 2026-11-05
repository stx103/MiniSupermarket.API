using System.Reflection;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using MiniSupermarket.WinForms;
using Xunit;

namespace MiniSupermarket.Tests
{
    public class StructureTests
    {
        [Theory]
        [InlineData("MinhKhangMiniSupermarketDb.sql")]
        [InlineData("Upgrade_Buoi4.sql")]
        [InlineData("Upgrade_Buoi5_6.sql")]
        [InlineData("Upgrade_25Criteria.sql")]
        [InlineData("Upgrade_CustomerAddresses.sql")]
        [InlineData("Upgrade_Dashboard_Loyalty.sql")]
        public void SqlScriptsHaveValidSqlServerSyntax(string name)
        {
            using var reader = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Database", name));
            new TSql160Parser(true).Parse(reader, out var errors);
            Assert.True(errors.Count == 0, string.Join("\n", errors.Select(e => $"{e.Line}: {e.Message}")));
        }

        [Fact]
        public void FreshSqlRetainsTwentyRowsInEachTable()
        {
            var sql = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Database", "MinhKhangMiniSupermarketDb.sql"));
            Assert.Contains("SET QUOTED_IDENTIFIER ON;", sql);
            Assert.Contains("SET ARITHABORT ON;", sql);
            foreach (var table in new[] { "Users", "Categories", "Products", "Customers", "Orders", "OrderDetails" })
            {
                var block = Regex.Match(sql, $@"INSERT INTO dbo\.{table}\b[\s\S]*?VALUES([\s\S]*?);", RegexOptions.IgnoreCase);
                Assert.True(block.Success, table);
                Assert.Equal(20, Regex.Matches(block.Groups[1].Value, @"(?m)^\(").Count);
            }
            var upgrade = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Database", "Upgrade_Buoi4.sql"));
            Assert.DoesNotMatch(@"(?i)\b(DROP|TRUNCATE|DELETE)\b", upgrade);
        }

        [Fact]
        public void FormsRenderAtShellSizeAndMinimumSize()
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
                    Application.EnableVisualStyles();
                    SessionManager.CurrentRole = "Admin";
                    var output = Environment.GetEnvironmentVariable("MINHKHANG_QA_OUTPUT")
                        ?? Path.Combine(Path.GetTempPath(), "MiniSupermarket-QA");
                    Directory.CreateDirectory(output);
                    foreach (var size in new[] { new Size(1050, 638), new Size(874, 578) })
                    {
                        foreach (var type in new[] { typeof(FormPOS), typeof(FormProductManagement), typeof(FormUserManagement), typeof(FormQuickReport), typeof(FormCategoryManagement), typeof(FormCustomerManagement) })
                        {
                            using var form = (Form)Activator.CreateInstance(type)!;
                            RemoveLoadHandlers(form);
                            form.FormBorderStyle = FormBorderStyle.None;
                            form.ShowInTaskbar = false;
                            form.StartPosition = FormStartPosition.Manual;
                            form.Location = new Point(-20000, -20000);
                            form.ClientSize = size;
                            form.Show();
                            PopulatePreview(form);
                            Application.DoEvents();
                            form.PerformLayout();
                            using var bitmap = new Bitmap(size.Width, size.Height);
                            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, size));
                            bitmap.Save(Path.Combine(output, $"{type.Name}-{size.Width}.png"));
                        }
                    }
                    using var shell = new FormMainShell();
                    RemoveLoadHandlers(shell);
                    shell.FormBorderStyle = FormBorderStyle.None;
                    shell.StartPosition = FormStartPosition.Manual;
                    shell.Location = new Point(-20000, -20000);
                    shell.ShowInTaskbar = false;
                    shell.Show();
                    foreach (var role in new[] { "Admin", "Cashier", "Warehouse" })
                    {
                        SessionManager.CurrentRole = role;
                        typeof(FormMainShell).GetMethod("ApplyRolePermissions", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(shell, null);
                        var names = new[] { "btnPOS", "btnCategory", "btnProduct", "btnCustomer", "btnReports", "btnUserManage" };
                        var expectedMenus = role switch
                        {
                            "Admin" => names,
                            "Cashier" => new[] { "btnPOS", "btnCustomer" },
                            _ => new[] { "btnCategory", "btnProduct" }
                        };
                        Assert.Equal(expectedMenus.OrderBy(name => name), names.Where(name => shell.Controls.Find(name, true).Single().Visible).OrderBy(name => name));
                    }
                    SessionManager.CurrentRole = "Admin";
                    typeof(FormMainShell).GetMethod("ApplyRolePermissions", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(shell, null);
                    // Chỉ kiểm tra bố cục tại đây; không gọi API thật từ bài test ảnh tĩnh.
                    var reportPreview = new FormQuickReport { TopLevel = false, FormBorderStyle = FormBorderStyle.None, Dock = DockStyle.Fill };
                    RemoveLoadHandlers(reportPreview);
                    shell.Controls.Find("panelMainContent", true).Single().Controls.Add(reportPreview);
                    reportPreview.Show();
                    PopulatePreview((Form)shell.Controls.Find("panelMainContent", true).Single().Controls[0]);
                    shell.Controls.Find("lblUserInfo", true).Single().Text = "Nhân viên: admin01 | Nguyễn Quản Trị | Vai trò: Admin";
                    Application.DoEvents();
                    using var shellBitmap = new Bitmap(shell.ClientSize.Width, shell.ClientSize.Height);
                    shell.DrawToBitmap(shellBitmap, new Rectangle(Point.Empty, shell.ClientSize));
                    shellBitmap.Save(Path.Combine(output, "FormMainShell.png"));
                }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Rendering timed out.");
            Assert.Null(failure);
        }

        private static void RemoveLoadHandlers(Form form)
        {
            var events = (EventHandlerList)typeof(Component).GetProperty("Events", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;
            var key = typeof(Form).GetFields(BindingFlags.Static | BindingFlags.NonPublic)
                .Single(f => f.Name.Contains("load", StringComparison.OrdinalIgnoreCase) && f.FieldType == typeof(object)).GetValue(null)!;
            var handler = events[key];
            if (handler != null) events.RemoveHandler(key, handler);
        }

        [Fact]
        public void ProtectedFormsDisableActionsEvenOutsideTheShell()
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                var previousRole = SessionManager.CurrentRole;
                try
                {
                    foreach (var role in new[] { "Cashier", "Warehouse", "Admin" })
                    {
                        SessionManager.CurrentRole = role;
                        foreach (var entry in new[]
                        {
                            (typeof(FormCategoryManagement), new[] { "btnAdd", "btnUpdate", "btnDelete" }, role != "Cashier"),
                            (typeof(FormProductManagement), new[] { "btnAdd", "btnUpdate", "btnDelete" }, role != "Cashier"),
                            (typeof(FormCustomerManagement), new[] { "btnAdd", "btnUpdate", "btnDelete" }, role is "Admin" or "Cashier"),
                            (typeof(FormQuickReport), new[] { "btnRunRange" }, role == "Admin"),
                            (typeof(FormUserManagement), new[] { "btnAddUser", "btnResetPassword", "btnToggleLock" }, role == "Admin")
                        })
                        {
                            using var form = (Form)Activator.CreateInstance(entry.Item1)!;
                            foreach (var name in entry.Item2)
                                Assert.Equal(entry.Item3, form.Controls.Find(name, true).Single().Enabled);
                        }
                    }
                }
                catch (Exception ex) { failure = ex; }
                finally { SessionManager.CurrentRole = previousRole; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
            Assert.Null(failure);
        }

        private static void PopulatePreview(Form form)
        {
            var product = new ProductDto { ProductId = 1, Barcode = "8935001710011", ProductName = "Mì ly hải sản Minh Khang", Price = 12000, StockQuantity = 120, CategoryId = 4, CategoryName = "Mì và thực phẩm ăn liền" };
            switch (form)
            {
                case FormProductManagement:
                    ((DataGridView)form.Controls.Find("dgvProducts", true).Single()).DataSource = new[] { product };
                    ((PagingBar)form.Controls.Find("pagingBar", true).Single()).Render(1, 4, 20);
                    break;
                case FormCustomerManagement:
                    ((DataGridView)form.Controls.Find("dgvCustomers", true).Single()).DataSource = new[]
                    {
                        new CustomerDto { CustomerId = 16, CustomerName = "Trịnh Thị Q", PhoneNumber = "0915556677", Address = "222, Đường Lạc Long Quân, Phường Hòa Bình, Thành phố Hồ Chí Minh", RewardPoints = 410, MembershipRank = "Kim cương" },
                        new CustomerDto { CustomerId = 3, CustomerName = "Lê Văn C", PhoneNumber = "0983344556", Address = "56/2, Đường Đinh Tiên Hoàng, Phường Tân Định, Thành phố Hồ Chí Minh", RewardPoints = 350, MembershipRank = "Vàng" },
                        new CustomerDto { CustomerId = 8, CustomerName = "Võ Văn H", PhoneNumber = "0906677889", Address = "126, Đường Hoàng Diệu, Phường Khánh Hội, Thành phố Hồ Chí Minh", RewardPoints = 280, MembershipRank = "Vàng" },
                        new CustomerDto { CustomerId = 18, CustomerName = "Đặng Thị S", PhoneNumber = "0917778899", Address = "246, Đường Rừng Sác, Xã Bình Khánh, Thành phố Hồ Chí Minh", RewardPoints = 240, MembershipRank = "Bạc" },
                        new CustomerDto { CustomerId = 1, CustomerName = "Nguyễn Văn A", PhoneNumber = "0901122334", Address = "12, Đường Nguyễn Huệ, Phường Sài Gòn, Thành phố Hồ Chí Minh", RewardPoints = 150, MembershipRank = "Bạc" }
                    };
                    typeof(FormCustomerManagement).GetMethod("FormatDataGridView", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(form, null);
                    form.Controls.Find("txtAddress", true).Single().Text = "222, Đường Lạc Long Quân, Phường Hòa Bình, Thành phố Hồ Chí Minh";
                    ((PagingBar)form.Controls.Find("pagingBar", true).Single()).Render(1, 4, 20);
                    break;
                case FormPOS:
                    ((DataGridView)form.Controls.Find("dgvCart", true).Single()).DataSource = new[] { new CartItemDto { ProductId = 1, ProductName = product.ProductName, UnitPrice = 12000, Quantity = 2 } };
                    form.Controls.Find("lblTotalAmount", true).Single().Text = "24.000 đ";
                    break;
                case FormUserManagement:
                    ((DataGridView)form.Controls.Find("dgvUsers", true).Single()).DataSource = new[] { new UserDto { UserId = 8, Username = "admin01", FullName = "Nguyễn Quản Trị", Role = "Admin", IsActive = true } };
                    break;
                case FormQuickReport:
                    form.Controls.Find("lblTotalOrders", true).Single().Text = "20";
                    form.Controls.Find("lblTotalRevenue", true).Single().Text = "443.000 đ";
                    form.Controls.Find("lblLowStock", true).Single().Text = "2";
                    ((DataGridView)form.Controls.Find("dgvCashierRevenue", true).Single()).DataSource = new[]
                    {
                        new CashierRevenueView { CashierUsername = "cashier01", TotalOrders = 5, TotalRevenue = 110000, TotalPointsAwarded = 10 }
                    };
                    ((DataGridView)form.Controls.Find("dgvTopProducts", true).Single()).DataSource = new[]
                    {
                        new TopProductView { ProductId = 1, ProductName = product.ProductName, TotalQuantity = 2, TotalRevenue = 24000 }
                    };
                    break;
            }
        }
    }
}
