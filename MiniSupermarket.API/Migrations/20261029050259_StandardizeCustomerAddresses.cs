using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniSupermarket.API.Migrations
{
    public partial class StandardizeCustomerAddresses : Migration
    {
        private static readonly (int Id, string Phone, string OldAddress, string NewAddress)[] Addresses =
        {
            (1, "0901122334", "Quận 1, TP.HCM", "12, Đường Nguyễn Huệ, Phường Sài Gòn, Thành phố Hồ Chí Minh"),
            (2, "0918877665", "Quận 3, TP.HCM", "34, Đường Lê Lợi, Phường Bến Thành, Thành phố Hồ Chí Minh"),
            (3, "0983344556", "Bình Thạnh, TP.HCM", "56/2, Đường Đinh Tiên Hoàng, Phường Tân Định, Thành phố Hồ Chí Minh"),
            (4, "0902233445", "Quận 5, TP.HCM", "78, Đường Nguyễn Trãi, Phường Chợ Quán, Thành phố Hồ Chí Minh"),
            (5, "0903344556", "Quận 7, TP.HCM", "90A, Đường Nguyễn Thị Thập, Phường Tân Mỹ, Thành phố Hồ Chí Minh"),
            (6, "0904455667", "Thủ Đức, TP.HCM", "102, Đường Võ Văn Ngân, Phường Thủ Đức, Thành phố Hồ Chí Minh"),
            (7, "0905566778", "Quận 10, TP.HCM", "114, Đường Thành Thái, Phường Diên Hồng, Thành phố Hồ Chí Minh"),
            (8, "0906677889", "Quận 4, TP.HCM", "126, Đường Hoàng Diệu, Phường Khánh Hội, Thành phố Hồ Chí Minh"),
            (9, "0907788990", "Gò Vấp, TP.HCM", "138, Đường Quang Trung, Phường Hạnh Thông, Thành phố Hồ Chí Minh"),
            (10, "0908899001", "Tân Bình, TP.HCM", "150, Đường Cộng Hòa, Phường Tân Bình, Thành phố Hồ Chí Minh"),
            (11, "0910001122", "Phú Nhuận, TP.HCM", "162/5, Đường Phan Xích Long, Phường Cầu Kiệu, Thành phố Hồ Chí Minh"),
            (12, "0911112233", "Quận 6, TP.HCM", "174, Đường Hậu Giang, Phường Bình Tây, Thành phố Hồ Chí Minh"),
            (13, "0912223344", "Quận 8, TP.HCM", "186, Đường Phạm Thế Hiển, Phường Bình Đông, Thành phố Hồ Chí Minh"),
            (14, "0913334455", "Bình Tân, TP.HCM", "198, Đường Tên Lửa, Phường Bình Trị Đông, Thành phố Hồ Chí Minh"),
            (15, "0914445566", "Nhà Bè, TP.HCM", "210, Đường Huỳnh Tấn Phát, Phường Tân Thuận, Thành phố Hồ Chí Minh"),
            (16, "0915556677", "Quận 11, TP.HCM", "222, Đường Lạc Long Quân, Phường Hòa Bình, Thành phố Hồ Chí Minh"),
            (17, "0916667788", "Quận 12, TP.HCM", "234, Đường Lê Văn Khương, Phường Thới An, Thành phố Hồ Chí Minh"),
            (18, "0917778899", "Củ Chi, TP.HCM", "246, Đường Rừng Sác, Xã Bình Khánh, Thành phố Hồ Chí Minh"),
            (19, "0918889900", "Hóc Môn, TP.HCM", "258, Đường số 1, Xã Thạnh An, Thành phố Hồ Chí Minh"),
            (20, "0919990011", "Quận 2, TP.HCM", "270, Đường Lương Định Của, Phường An Khánh, Thành phố Hồ Chí Minh")
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            UpdateAddresses(migrationBuilder, true);
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM dbo.Customers WHERE [Address] IS NULL
                    OR [Address] NOT LIKE '%,%,%,%' OR [Address] LIKE '%,%,%,%,%')
                    THROW 50011, 'Update missing or malformed custom customer addresses before upgrading.', 1;
                """);
            migrationBuilder.AlterColumn<string>(
                name: "Address", table: "Customers", type: "nvarchar(200)", maxLength: 200,
                nullable: false, oldClrType: typeof(string), oldType: "nvarchar(200)",
                oldMaxLength: 200, oldNullable: true);
            migrationBuilder.AddCheckConstraint(
                name: "CK_Customers_Address_FourParts", table: "Customers",
                sql: "[Address] LIKE '%,%,%,%' AND [Address] NOT LIKE '%,%,%,%,%'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "CK_Customers_Address_FourParts", table: "Customers");
            migrationBuilder.AlterColumn<string>(
                name: "Address", table: "Customers", type: "nvarchar(200)", maxLength: 200,
                nullable: true, oldClrType: typeof(string), oldType: "nvarchar(200)", oldMaxLength: 200);
            UpdateAddresses(migrationBuilder, false);
        }

        private static void UpdateAddresses(MigrationBuilder migrationBuilder, bool upgrade)
        {
            foreach (var row in Addresses)
            {
                var target = upgrade ? row.NewAddress : row.OldAddress;
                var source = upgrade ? row.OldAddress : row.NewAddress;
                var empty = upgrade ? " OR NULLIF(LTRIM(RTRIM([Address])), N'') IS NULL" : string.Empty;
                migrationBuilder.Sql($"""
                    UPDATE dbo.Customers SET [Address] = {SqlLiteral(target)}
                    WHERE CustomerId = {row.Id} AND PhoneNumber = {SqlLiteral(row.Phone)}
                        AND ([Address] = {SqlLiteral(source)}{empty});
                    """);
            }
        }

        private static string SqlLiteral(string value) => "N'" + value.Replace("'", "''") + "'";
    }
}
