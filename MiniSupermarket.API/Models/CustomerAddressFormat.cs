namespace MiniSupermarket.API.Models
{
    public static class CustomerAddressFormat
    {
        public const string Pattern = @"(?i)^[ \t]*[0-9][\p{L}0-9/.\-]*[ \t]*,[ \t]*[^,\s][^,\r\n]*,[ \t]*(?:phường|xã)[ \t]+[^,\s][^,\r\n]*,[ \t]*(?:tỉnh|thành phố|tp\.?)[ \t]+[^,\s][^,\r\n]*$";
        public const string ErrorMessage = "Địa chỉ phải đủ 4 phần: số nhà, tên đường, phường/xã, tỉnh/thành phố; ngăn cách bằng dấu phẩy.";
    }
}
