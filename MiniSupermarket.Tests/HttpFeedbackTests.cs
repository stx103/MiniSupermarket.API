using System.Net;
using System.Net.Http.Json;
using MiniSupermarket.WinForms;
using Xunit;

namespace MiniSupermarket.Tests
{
    public class HttpFeedbackTests
    {
        [Theory]
        [InlineData("customers/99999", "khách hàng")]
        [InlineData("products/99999", "sản phẩm")]
        [InlineData("ado-products/barcode/unknown", "sản phẩm")]
        [InlineData("orders/99999", "đơn hàng")]
        [InlineData("checkout/cancel/99999", "đơn hàng")]
        [InlineData("categories/99999", "nhóm hàng")]
        [InlineData("users/99999/password", "tài khoản")]
        public async Task EmptyNotFoundResponsesKeepTheHttpCodeAndIdentifyTheResource(string route, string resource)
        {
            using var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/" + route),
                Content = new StringContent(string.Empty)
            };
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => ApiClientService.EnsureSuccessAsync(response));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
            Assert.Equal($"404 - Không tìm thấy {resource}.", exception.Message);
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized, "401 - ")]
        [InlineData(HttpStatusCode.Forbidden, "403 - ")]
        [InlineData(HttpStatusCode.Conflict, "409 - ")]
        public async Task ErrorCodesAndServerMessagesArePreserved(HttpStatusCode code, string prefix)
        {
            using var response = new HttpResponseMessage(code)
            {
                Content = JsonContent.Create(new { message = "Thông báo nghiệp vụ từ API." })
            };
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => ApiClientService.EnsureSuccessAsync(response));
            Assert.Equal(code, exception.StatusCode);
            Assert.Equal(prefix + "Thông báo nghiệp vụ từ API.", exception.Message);
        }

        [Fact]
        public async Task ValidationErrorsShowReadableDetailsWithoutRawJson()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                RequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://localhost/api/customers"),
                Content = JsonContent.Create(new { errors = new { Address = new[] { "Địa chỉ phải đủ 4 phần." } } })
            };
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => ApiClientService.EnsureSuccessAsync(response));
            Assert.StartsWith("400 - Dữ liệu khách hàng không hợp lệ.", exception.Message);
            Assert.Contains("Địa chỉ phải đủ 4 phần.", exception.Message);
            Assert.DoesNotContain("{", exception.Message);
        }

        [Fact]
        public async Task HtmlFailureDoesNotExposeAStackTraceAndConnectionFailureHasNoFakeHttpCode()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("<html>Private server stack trace</html>")
            };
            Assert.Equal("500 - Máy chủ không thể xử lý dữ liệu yêu cầu. Vui lòng thử lại sau.", await ApiFeedback.ReadErrorAsync(response));
            Assert.StartsWith("Không thể kết nối API.", ApiFeedback.ErrorMessage(new HttpRequestException("Connection refused")));
        }

        [Theory]
        [InlineData("/api/categories/search?keyword=NO-MATCH-HTTP", "nhóm hàng")]
        [InlineData("/api/customers/search?keyword=NO-MATCH-HTTP", "khách hàng")]
        [InlineData("/api/customers/paged?keyword=NO-MATCH-HTTP", "khách hàng")]
        [InlineData("/api/products?keyword=NO-MATCH-HTTP", "sản phẩm")]
        [InlineData("/api/products/paged?keyword=NO-MATCH-HTTP", "sản phẩm")]
        [InlineData("/api/customers/phone/0999999999", "khách hàng")]
        [InlineData("/api/orders/99999", "đơn hàng")]
        public async Task MissingDataTravelsFromApiToClientAs404(string endpoint, string resource)
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync("admin01");
            using var response = await client.GetAsync(endpoint);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => ApiClientService.EnsureSuccessAsync(response));
            Assert.StartsWith("404 - ", exception.Message);
            Assert.Contains(resource, exception.Message);
        }
    }
}
