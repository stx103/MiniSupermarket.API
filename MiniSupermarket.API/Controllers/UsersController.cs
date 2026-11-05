using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MiniSupermarket.API.Data;
using MiniSupermarket.API.Models;
using MiniSupermarket.API.Security;

namespace MiniSupermarket.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly SupermarketDbContext _context;
        public UsersController(SupermarketDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _context.Users.AsNoTracking()
            .OrderBy(u => u.UserId)
            .Select(u => new UserDto
            {
                UserId = u.UserId, Username = u.Username, FullName = u.FullName,
                Email = u.Email, Role = u.Role, IsActive = u.IsActive
            })
            .ToListAsync());

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserRequest request)
        {
            var username = request.Username.Trim();
            if (await _context.Users.AnyAsync(u => u.Username == username))
                return Conflict(new { message = "Tên đăng nhập đã tồn tại." });
            var user = new User
            {
                Username = username, FullName = request.FullName.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                Role = request.Role, IsActive = true
            };
            user.Password = UserPasswords.Hash(user, request.Password);
            _context.Users.Add(user);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            { return Conflict(new { message = "Tên đăng nhập đã tồn tại." }); }
            return StatusCode(201, new UserDto
            {
                UserId = user.UserId, Username = user.Username, FullName = user.FullName,
                Email = user.Email, Role = user.Role, IsActive = user.IsActive
            });
        }

        [HttpPut("{id:int}/password")]
        public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.Password = UserPasswords.Hash(user, request.Password);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> SetStatus(int id, SetUserStatusRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            if (!request.IsActive && user.Username == User.Identity?.Name)
                return BadRequest(new { message = "Bạn không thể tự khóa tài khoản đang đăng nhập." });
            user.IsActive = request.IsActive;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
