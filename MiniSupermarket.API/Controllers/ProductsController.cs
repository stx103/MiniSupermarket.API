using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MiniSupermarket.API.Data;
using MiniSupermarket.API.Models;

namespace MiniSupermarket.API.Controllers
{
    // [Tiêu chí 13] Controller sản phẩm riêng, route REST api/products; tách khỏi nhóm hàng và đăng nhập.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Cashier,Warehouse")]
    public class ProductsController : ControllerBase
    {
        private readonly SupermarketDbContext _context;
        public ProductsController(SupermarketDbContext context) => _context = context;

        [HttpGet]
        [ProducesResponseType(typeof(List<ProductItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> GetAll(string? keyword, int? categoryId)
        {
            var query = _context.Products.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(p => p.Barcode.Contains(keyword) || p.ProductName.Contains(keyword));
            }
            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId);
            var products = await query.OrderBy(p => p.ProductId).Select(p => new ProductItemDto
            {
                ProductId = p.ProductId, Barcode = p.Barcode, ProductName = p.ProductName,
                Price = p.Price, StockQuantity = p.StockQuantity, CategoryId = p.CategoryId,
                CategoryName = p.Category!.CategoryName
            }).ToListAsync();
            return products.Count == 0 && (!string.IsNullOrWhiteSpace(keyword) || categoryId.HasValue)
                ? NotFound(new { message = "Không tìm thấy sản phẩm phù hợp với điều kiện tìm kiếm." })
                : Ok(products);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProductItemDto), StatusCodes.Status200OK)]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _context.Products.AsNoTracking().Where(p => p.ProductId == id)
                .Select(p => new ProductItemDto
                {
                    ProductId = p.ProductId, Barcode = p.Barcode, ProductName = p.ProductName,
                    Price = p.Price, StockQuantity = p.StockQuantity, CategoryId = p.CategoryId,
                    CategoryName = p.Category!.CategoryName
                }).FirstOrDefaultAsync();
            return product == null ? NotFound() : Ok(product);
        }

        // Cashiers can look up a barcode, but cannot list or edit the inventory.
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResult<ProductItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> GetPagedProducts(string? keyword, int? categoryId, int pageNumber = 1, int pageSize = 5, CancellationToken cancellationToken = default)
        {
            // [Tiêu chí 26] Include(p => p.Category) nạp nhóm hàng để DTO hiển thị được CategoryName.
            // [Tiêu chí 27] AsNoTracking tắt theo dõi thay đổi cho truy vấn GET chỉ đọc, giảm bộ nhớ của ChangeTracker.
            var query = _context.Products.Include(p => p.Category).AsNoTracking().AsQueryable();
            // [Tiêu chí 24] LINQ Contains tìm gần đúng tên/mã vạch; Where theo CategoryId lọc nhóm hàng trước khi phân trang.
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var search = keyword.Trim().ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(search) || p.Barcode.ToLower().Contains(search));
            }
            if (categoryId > 0) query = query.Where(p => p.CategoryId == categoryId);
            var rows = query.OrderBy(p => p.ProductId).Select(p => new ProductItemDto
            {
                ProductId = p.ProductId, Barcode = p.Barcode, ProductName = p.ProductName,
                Price = p.Price, StockQuantity = p.StockQuantity, CategoryId = p.CategoryId,
                CategoryName = p.Category == null ? string.Empty : p.Category.CategoryName
            });
            // [Tiêu chí 25] Phân trang phía server: PagedResult.CreateAsync thực hiện CountAsync, Skip và Take trên IQueryable.
            var result = await PagedResult<ProductItemDto>.CreateAsync(rows, pageNumber, pageSize, cancellationToken);
            return result.TotalItems == 0 && (!string.IsNullOrWhiteSpace(keyword) || categoryId > 0)
                ? NotFound(new { message = "Không tìm thấy sản phẩm phù hợp với điều kiện tìm kiếm." })
                : Ok(result);
        }

        [HttpGet("barcode/{barcode}")]
        [ProducesResponseType(typeof(ProductItemDto), StatusCodes.Status200OK)]
        [Authorize(Roles = "Admin,Cashier,Warehouse")]
        public async Task<IActionResult> GetByBarcode(string barcode)
        {
            var product = await _context.Products.AsNoTracking()
                .Where(p => p.Barcode == barcode)
                .Select(p => new ProductItemDto
                {
                    ProductId = p.ProductId, Barcode = p.Barcode, ProductName = p.ProductName,
                    Price = p.Price, StockQuantity = p.StockQuantity, CategoryId = p.CategoryId,
                    CategoryName = p.Category!.CategoryName
                })
                .FirstOrDefaultAsync();
            return product == null ? NotFound(new { message = "Không tìm thấy mã vạch sản phẩm." }) : Ok(product);
        }

        // [Tiêu chí 30] Admin và Warehouse được quản lý sản phẩm; Cashier chỉ được tra barcode phục vụ bán hàng, không sửa giá/tồn kho.
        [HttpPost]
        [ProducesResponseType(typeof(ProductItemDto), StatusCodes.Status201Created)]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> Create(ProductRequest request)
        {
            var error = await ValidateAsync(request);
            if (error != null) return BadRequest(new { message = error });
            var product = new Product();
            Assign(product, request);
            _context.Products.Add(product);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            { return Conflict(new { message = "Mã vạch đã tồn tại." }); }
            return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, new ProductItemDto
            {
                ProductId = product.ProductId, Barcode = product.Barcode, ProductName = product.ProductName,
                Price = product.Price, StockQuantity = product.StockQuantity, CategoryId = product.CategoryId,
                CategoryName = await _context.Categories.AsNoTracking().Where(c => c.CategoryId == product.CategoryId)
                    .Select(c => c.CategoryName).SingleAsync()
            });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> Update(int id, ProductRequest request)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            var error = await ValidateAsync(request, id);
            if (error != null) return BadRequest(new { message = error });
            Assign(product, request);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            { return Conflict(new { message = "Mã vạch đã tồn tại." }); }
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            if (await _context.OrderDetails.AnyAsync(d => d.ProductId == id))
                return BadRequest(new { message = "Sản phẩm đã có trong hóa đơn, không thể xóa." });
            _context.Products.Remove(product);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
            { return Conflict(new { message = "Sản phẩm vừa phát sinh hóa đơn, không thể xóa." }); }
            return NoContent();
        }

        private async Task<string?> ValidateAsync(ProductRequest request, int id = 0)
        {
            if (!await _context.Categories.AnyAsync(c => c.CategoryId == request.CategoryId))
                return "Nhóm hàng không tồn tại.";
            if (await _context.Products.AnyAsync(p => p.ProductId != id && p.Barcode == request.Barcode.Trim()))
                return "Mã vạch đã tồn tại.";
            return null;
        }

        private static void Assign(Product product, ProductRequest request)
        {
            product.Barcode = request.Barcode.Trim();
            product.ProductName = request.ProductName.Trim();
            product.Price = decimal.Round(request.Price, 2);
            product.StockQuantity = request.StockQuantity;
            product.CategoryId = request.CategoryId;
        }
    }
}
