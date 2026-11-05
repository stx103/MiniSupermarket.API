using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniSupermarket.API.Data;
using MiniSupermarket.API.Models;

namespace MiniSupermarket.API.Controllers
{
    // [Tiêu chí 13] Controller nhóm hàng riêng; [controller] tạo route REST api/categories (xem thêm AuthController và ProductsController).
    [Route("api/[controller]")]
    [ApiController]
    // [Tiêu chí 29] Bắt buộc đăng nhập bằng JWT; chưa đăng nhập gọi API này sẽ nhận HTTP 401.
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly SupermarketDbContext _context;

        public CategoriesController(SupermarketDbContext context)
        {
            _context = context;
        }

        private IQueryable<CategoryDto> ReadCategories() => _context.Categories.AsNoTracking()
            .Select(c => new CategoryDto { CategoryId = c.CategoryId, CategoryName = c.CategoryName, Description = c.Description });

        // [Tiêu chí 14] GET đọc dữ liệu (200); POST tạo mới (201); PUT sửa, DELETE xóa (204); dữ liệu sai 400, không tìm thấy 404.
        // [Tiêu chí 16] Các action dùng async Task<IActionResult> và await truy vấn/lưu dữ liệu để không chặn luồng khi chờ CSDL.
        // GET /api/categories
        [HttpGet]
        [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> GetAll()
        {
            var list = await ReadCategories()
                .OrderBy(c => c.CategoryId)
                .ToListAsync();

            return Ok(list);
        }

        // GET /api/categories/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await ReadCategories().FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy nhóm hàng trong CSDL!"
                });
            }

            return Ok(category);
        }

        // GET /api/categories/search?keyword=...
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> Search([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new
                {
                    message = "Vui lòng nhập từ khóa tìm kiếm!"
                });
            }

            var result = await ReadCategories()
                .Where(c => c.CategoryName.Contains(keyword)
                    || (c.Description != null && c.Description.Contains(keyword)))
                .OrderBy(c => c.CategoryId)
                .ToListAsync();

            return result.Count == 0
                ? NotFound(new { message = "Không tìm thấy nhóm hàng phù hợp với từ khóa." })
                : Ok(result);
        }

        // [Tiêu chí 23] CRUD bằng EF Core: Create dùng Add, Update sửa entity, Delete dùng Remove; đều await SaveChangesAsync.
        // POST /api/categories
        [HttpPost]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> Create([FromBody] CategoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newCat = new Category { CategoryName = request.CategoryName.Trim(), Description = request.Description?.Trim() };
            // [Tiêu chí 23] Thêm nhóm hàng qua DbSet, lưu CSDL bằng EF Core rồi trả DTO kèm HTTP 201.
            _context.Categories.Add(newCat);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = newCat.CategoryId },
                new CategoryDto { CategoryId = newCat.CategoryId, CategoryName = newCat.CategoryName, Description = newCat.Description });
        }

        // PUT /api/categories/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy nhóm hàng cần sửa!"
                });
            }

            // [Tiêu chí 23] Cập nhật entity đang được EF Core theo dõi; SaveChangesAsync ghi các thay đổi xuống CSDL.
            category.CategoryName = request.CategoryName.Trim();
            category.Description = request.Description?.Trim();

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/categories/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy nhóm hàng cần xóa!"
                });
            }

            if (category.Products?.Any() == true)
            {
                return BadRequest(new
                {
                    message = "Không thể xóa nhóm hàng đang có sản phẩm tồn tại!"
                });
            }

            // [Tiêu chí 23] Xóa nhóm hàng sau khi kiểm tra ràng buộc sản phẩm; SaveChangesAsync thực thi lệnh xóa.
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("admin-dashboard")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminDashboard() => await GetAccessInfoAsync(
            "Chào mừng Admin! Bạn có toàn quyền quản trị hệ thống Tạp hóa Tiện lợi Minh Khang.");

        [HttpGet("staff-pos")]
        [Authorize(Roles = "Admin,Cashier")]
        public async Task<IActionResult> GetStaffPos() => await GetAccessInfoAsync(
            "Màn hình POS Thu ngân Tạp hóa Tiện lợi Minh Khang sẵn sàng phục vụ bán hàng.");

        private async Task<IActionResult> GetAccessInfoAsync(string message)
        {
            var info = await _context.Users.AsNoTracking().Where(u => u.Username == User.Identity!.Name)
                .Select(u => new AccessInfoDto { Message = message, Username = u.Username, FullName = u.FullName, Role = u.Role })
                .FirstOrDefaultAsync();
            return info == null ? Unauthorized() : Ok(info);
        }
    }
}
