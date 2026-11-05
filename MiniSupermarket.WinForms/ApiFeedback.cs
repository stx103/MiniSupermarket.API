using System.Net;
using System.Text.Json;

namespace MiniSupermarket.WinForms
{
    public static class ApiFeedback
    {
        // [Tiêu chí 14] Hiển thị mã HTTP thật kèm thông báo theo tài nguyên, ví dụ: 404 - Không tìm thấy khách hàng.
        public static string Format(HttpStatusCode status, string message) => $"{(int)status} - {message}";

        public static async Task<string> ReadErrorAsync(HttpResponseMessage response)
        {
            var resource = Resource(response.RequestMessage?.RequestUri);
            var fallback = response.StatusCode switch
            {
                HttpStatusCode.BadRequest => $"Dữ liệu {resource} không hợp lệ.",
                HttpStatusCode.Unauthorized => "Chưa đăng nhập, phiên đã hết hạn hoặc tài khoản không hợp lệ. Vui lòng đăng nhập lại.",
                HttpStatusCode.Forbidden => $"Bạn không có quyền thao tác với {resource}.",
                HttpStatusCode.NotFound => $"Không tìm thấy {resource}.",
                HttpStatusCode.Conflict => $"Dữ liệu {resource} bị trùng hoặc đã thay đổi. Vui lòng kiểm tra lại.",
                _ when (int)response.StatusCode >= 500 => $"Máy chủ không thể xử lý {resource}. Vui lòng thử lại sau.",
                _ => $"Không thể xử lý yêu cầu về {resource}."
            };
            var body = await response.Content.ReadAsStringAsync();
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(message.GetString()))
                        return Format(response.StatusCode, message.GetString()!);
                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var details = errors.EnumerateObject().SelectMany(p => Messages(p.Value)).Distinct();
                        return Format(response.StatusCode, fallback + "\n" + string.Join("\n", details));
                    }
                    // Older actions may return ModelState directly instead of ProblemDetails.
                    var validation = root.EnumerateObject().Where(p => p.Value.ValueKind == JsonValueKind.Array)
                        .SelectMany(p => Messages(p.Value)).Distinct().ToList();
                    if (validation.Count > 0)
                        return Format(response.StatusCode, fallback + "\n" + string.Join("\n", validation));
                }
            }
            catch (JsonException) { }
            // Empty responses, HTML and exception pages use a readable message, not raw server output.
            return Format(response.StatusCode, fallback);
        }

        public static string ErrorMessage(Exception exception) => exception switch
        {
            HttpRequestException { StatusCode: null } => "Không thể kết nối API. Hãy kiểm tra API đang chạy và địa chỉ kết nối.",
            OperationCanceledException => "Yêu cầu quá thời gian chờ. Vui lòng thử lại.",
            _ => exception.Message
        };

        private static IEnumerable<string> Messages(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                    if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                        yield return item.GetString()!;
            }
            else if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
                yield return value.GetString()!;
        }

        private static string Resource(Uri? uri)
        {
            var path = uri == null ? string.Empty : uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString.Split('?')[0];
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return parts.FirstOrDefault(p => p is "customers" or "categories" or "products" or "ado-products"
                or "orders" or "checkout" or "users" or "auth" or "reports") switch
            {
                "customers" => "khách hàng",
                "categories" => "nhóm hàng",
                "products" or "ado-products" => "sản phẩm",
                "orders" or "checkout" => "đơn hàng",
                "users" => "tài khoản",
                "auth" => "thông tin đăng nhập",
                "reports" => "báo cáo",
                _ => "dữ liệu yêu cầu"
            };
        }
    }
}
