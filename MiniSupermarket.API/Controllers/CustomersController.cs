using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniSupermarket.API.Data;
using MiniSupermarket.API.Models;

namespace MiniSupermarket.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Tiêu chí 30] Admin và Cashier được quản lý khách hàng; Warehouse không có quyền truy cập.
    [Authorize(Roles = "Admin,Cashier")]
    public class CustomersController : ControllerBase
    {
        private readonly SupermarketDbContext _context;

        public CustomersController(SupermarketDbContext context)
        {
            _context = context;
        }

        private IQueryable<CustomerDto> ReadCustomers() => _context.Customers.AsNoTracking().Select(c => new CustomerDto
        {
            CustomerId = c.CustomerId, CreatedAt = c.CreatedAt, CustomerName = c.CustomerName, PhoneNumber = c.PhoneNumber,
            Address = c.Address, RewardPoints = c.RewardPoints, MembershipRank = c.MembershipRank
        });

        // GET /api/customers
        [HttpGet]
        [ProducesResponseType(typeof(List<CustomerDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var customers = await ReadCustomers()
                .OrderBy(c => c.CustomerId)
                .ToListAsync();

            return Ok(customers);
        }

        // GET /api/customers/{id}
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResult<CustomerDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPaged(string? keyword, int pageNumber = 1, int pageSize = 5, CancellationToken cancellationToken = default)
        {
            var query = ReadCustomers();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var search = keyword.Trim().ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(search) || c.PhoneNumber.Contains(search)
                    || c.MembershipRank.ToLower().Contains(search));
            }
            var result = await PagedResult<CustomerDto>.CreateAsync(query.OrderByDescending(c => c.RewardPoints)
                .ThenBy(c => c.CustomerId), pageNumber, pageSize, cancellationToken);
            return result.TotalItems == 0 && !string.IsNullOrWhiteSpace(keyword)
                ? NotFound(new { message = "Không tìm thấy khách hàng phù hợp với từ khóa." })
                : Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await ReadCustomers().FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy khách hàng trong CSDL!"
                });
            }

            return Ok(customer);
        }

        // GET /api/customers/search?keyword=...
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<CustomerDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Search([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new
                {
                    message = "Vui lòng nhập từ khóa tìm kiếm!"
                });
            }

            var result = await ReadCustomers()
                .Where(c => c.CustomerName.Contains(keyword)
                    || c.PhoneNumber.Contains(keyword)
                    || c.MembershipRank.Contains(keyword))
                .OrderBy(c => c.CustomerId)
                .ToListAsync();

            return result.Count == 0
                ? NotFound(new { message = "Không tìm thấy khách hàng phù hợp với từ khóa." })
                : Ok(result);
        }

        // POST /api/customers
        [HttpGet("phone/{phone}")]
        [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByPhone(string phone)
        {
            var customer = await ReadCustomers()
                .FirstOrDefaultAsync(c => c.PhoneNumber == phone);
            return customer == null
                ? NotFound(new { message = "Không tìm thấy khách hàng với số điện thoại này." })
                : Ok(customer);
        }

        [HttpPost]
        [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] CustomerRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newCustomer = new Customer { CreatedAt = DateTime.Now };
            Assign(newCustomer, request);
            bool phoneExists = await _context.Customers
                .AnyAsync(c => c.PhoneNumber == newCustomer.PhoneNumber);

            if (phoneExists)
            {
                return BadRequest(new
                {
                    message = "Số điện thoại khách hàng đã tồn tại!"
                });
            }

            if (string.IsNullOrWhiteSpace(newCustomer.MembershipRank))
            {
                newCustomer.MembershipRank = "Chuẩn";
            }

            newCustomer.CustomerId = 0;
            _context.Customers.Add(newCustomer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = newCustomer.CustomerId },
                await ReadCustomers().SingleAsync(c => c.CustomerId == newCustomer.CustomerId));
        }

        // PUT /api/customers/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customer = await _context.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy khách hàng cần sửa!"
                });
            }

            bool phoneExists = await _context.Customers
                .AnyAsync(c => c.CustomerId != id
                    && c.PhoneNumber == request.PhoneNumber.Trim());

            if (phoneExists)
            {
                return BadRequest(new
                {
                    message = "Số điện thoại khách hàng đã tồn tại!"
                });
            }

            Assign(customer, request);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/customers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy khách hàng cần xóa!"
                });
            }

            if (customer.Orders?.Any() == true)
            {
                return BadRequest(new
                {
                    message = "Không thể xóa khách hàng đã phát sinh hóa đơn!"
                });
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static void Assign(Customer customer, CustomerRequest request)
        {
            customer.CustomerName = request.CustomerName.Trim();
            customer.PhoneNumber = request.PhoneNumber.Trim();
            customer.Address = request.Address.Trim();
            customer.RewardPoints = request.RewardPoints;
            customer.MembershipRank = string.IsNullOrWhiteSpace(request.MembershipRank) ? "Chuẩn" : request.MembershipRank.Trim();
        }
    }
}
