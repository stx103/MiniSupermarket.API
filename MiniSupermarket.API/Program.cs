using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MiniSupermarket.API.Data;
using MiniSupermarket.API.Services;
using System.Text;
var builder = WebApplication.CreateBuilder(args);

// [Tiêu chí 12] Đọc ConnectionStrings:DefaultConnection từ appsettings.json; appsettings.Development.json ghi đè khi chạy Development.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// [Tiêu chí 11] Solution gồm API và WinForms; backend đăng ký DbContext, dịch vụ nghiệp vụ và controller tại Program.cs.
builder.Services.AddDbContext<SupermarketDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<AdoDatabase>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<RevenueReportService>();
// [Tiêu chí 11] Đăng ký chính sách CORS theo các địa chỉ được phép trong JSON; kích hoạt bằng UseCors bên dưới.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options => options.AddPolicy("ConfiguredClients", policy =>
{
    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));

// Cấu hình dịch vụ xác thực JWT Bearer
var jwtSecret = builder.Configuration["JwtSettings:Secret"] ??
"SupermarketSecretKeyDoAnMonHoc2026SecureString!!";
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
// Cấu hình xác thực JWT Bearer
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<SupermarketDbContext>();
            var username = context.Principal?.Identity?.Name;
            var role = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!await db.Users.AsNoTracking().AnyAsync(u => u.Username == username && u.IsActive && u.Role == role))
                context.Fail("Tài khoản đã bị khóa hoặc quyền đã thay đổi.");
        }
    };
});
// Cấu hình dịch vụ và middleware khác
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // [Tiêu chí 31] Tạo nút Authorize trên Swagger: nhập JWT nhận từ /api/auth/login để gọi các API bảo mật.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        Description = "Đăng nhập tại /api/auth/login, sau đó nhập token tại đây."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});
var app = builder.Build();
// Cấu hình middleware
// [Tiêu chí 17] Chạy môi trường Development, mở /swagger rồi dùng Try it out / Execute để kiểm thử API.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("ConfiguredClients");
// Bắt buộc gọi UseAuthentication trước UseAuthorization
// [Tiêu chí 29] Xác thực JWT trước khi xét quyền; các endpoint [Authorize] trả 401 nếu thiếu token hoặc token không hợp lệ.
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
