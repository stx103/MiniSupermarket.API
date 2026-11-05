using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MiniSupermarket.WinForms
{
    public static class SessionManager
    {
        public static string JwtToken { get; set; } = string.Empty;
        public static string CurrentUsername { get; set; } = string.Empty;
        public static string CurrentFullName { get; set; } = string.Empty;
        public static string CurrentRole { get; set; } = string.Empty;

        // [Tiêu chí 30] Phân quyền giao diện: Admin toàn quyền, Cashier chỉ POS, Warehouse quản lý nhóm hàng/sản phẩm; API vẫn kiểm tra Role riêng.
        public static bool CanAccess(string screen) => CurrentRole switch
        {
            "Admin" => true,
            "Cashier" => screen == "POS",
            "Warehouse" => screen is "Category" or "Product",
            _ => false
        };

        public static void Clear()
        {
            JwtToken = CurrentUsername = CurrentFullName = CurrentRole = string.Empty;
            ApiClientService.Client.DefaultRequestHeaders.Authorization = null;
        }
    }

    public static class ApiClientService
    {
        public static Uri BaseAddress { get; } = ReadBaseAddress();
        public static HttpClient Client { get; } = new() { BaseAddress = BaseAddress, Timeout = TimeSpan.FromSeconds(30) };

        private static Uri ReadBaseAddress()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "api-settings.json");
            if (!File.Exists(path)) return new Uri("https://localhost:7163/api/");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return new Uri(document.RootElement.GetProperty("ApiBaseUrl").GetString()!.TrimEnd('/') + "/");
        }

        public static HttpClient CreateAuthenticatedClient()
        {
            var client = new HttpClient { BaseAddress = BaseAddress, Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);
            return client;
        }

        public static async Task<bool> LoginAsync(string username, string password)
        {
            SessionManager.Clear();
            using var response = await Client.PostAsJsonAsync("auth/login", new { Username = username, Password = password });
            if (response.StatusCode == HttpStatusCode.Unauthorized) return false;
            await EnsureSuccessAsync(response);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            SessionManager.JwtToken = doc.RootElement.GetProperty("token").GetString() ?? string.Empty;
            SessionManager.CurrentRole = doc.RootElement.GetProperty("role").GetString() ?? string.Empty;
            SessionManager.CurrentUsername = doc.RootElement.GetProperty("username").GetString() ?? username;
            SessionManager.CurrentFullName = doc.RootElement.GetProperty("fullName").GetString() ?? username;
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);
            return true;
        }

        public static async Task<T> GetAsync<T>(string endpoint)
        {
            using var response = await Client.GetAsync(endpoint);
            await EnsureSuccessAsync(response);
            return await response.Content.ReadFromJsonAsync<T>() ?? throw new InvalidOperationException("Máy chủ trả về dữ liệu trống.");
        }

        public static async Task<string> GetDataWithTokenAsync(string endpoint)
        {
            using var response = await Client.GetAsync(endpoint);
            await EnsureSuccessAsync(response);
            return await response.Content.ReadAsStringAsync();
        }

        public static async Task EnsureSuccessAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;
            throw new HttpRequestException(await ApiFeedback.ReadErrorAsync(response), null, response.StatusCode);
        }
    }
}
