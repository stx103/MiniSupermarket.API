using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniSupermarket.API.Data;
using MiniSupermarket.API.Models;
using MiniSupermarket.API.Services;

namespace MiniSupermarket.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Cashier")]
    public class OrdersController : ControllerBase
    {
        private readonly CheckoutService _checkout;
        private readonly SupermarketDbContext _context;
        public OrdersController(CheckoutService checkout, SupermarketDbContext context) { _checkout = checkout; _context = context; }

        // [Tiêu chí 35] API thanh toán chuyển sang Services/CheckoutService.cs: CheckoutAsync chứa transaction và RollbackAsync xử lý hoàn tác.
        [HttpPost("checkout")]
        [HttpPost("~/api/checkout")]
        [ProducesResponseType(typeof(CheckoutResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> Checkout(CheckoutRequest request, CancellationToken cancellationToken)
        {
            try { return Ok(await _checkout.CheckoutAsync(request, User.Identity!.Name!, cancellationToken)); }
            catch (PosException ex) { return StatusCode(ex.StatusCode, new { success = false, message = ex.Message }); }
        }

        [HttpDelete("{orderId:int}")]
        [HttpDelete("~/api/checkout/cancel/{orderId:int}")]
        [ProducesResponseType(typeof(CancelOrderResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> Cancel(int orderId, CancellationToken cancellationToken)
        {
            try { return Ok(await _checkout.CancelAsync(orderId, User.Identity!.Name!, User.IsInRole("Admin"), cancellationToken)); }
            catch (PosException ex) { return StatusCode(ex.StatusCode, new { success = false, message = ex.Message }); }
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var order = await _context.Orders.AsNoTracking().Include(o => o.User)
                .Include(o => o.Customer).Include(o => o.OrderDetails!).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id, cancellationToken);
            if (order == null) return NotFound();
            if (!User.IsInRole("Admin") && order.User!.Username != User.Identity!.Name) return Forbid();
            return Ok(new OrderDto
            {
                OrderId = order.OrderId, OrderCode = order.OrderCode, OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount, FinalAmount = order.FinalAmount, DiscountAmount = order.DiscountAmount, RedeemedPoints = order.RedeemedPoints, RewardPoints = order.RewardPoints, Status = order.Status,
                CashierUsername = order.User!.Username, CustomerName = order.Customer?.CustomerName,
                Items = order.OrderDetails!.Select(d => new OrderItemDto
                {
                    ProductId = d.ProductId, ProductName = d.Product!.ProductName,
                    Quantity = d.Quantity, UnitPrice = d.UnitPrice, LineTotal = d.LineTotal
                }).ToList()
            });
        }
    }
}
