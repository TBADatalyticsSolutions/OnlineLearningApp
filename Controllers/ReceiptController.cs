using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using System.Security.Claims;

namespace OnlineLearningApp.Controllers;

[Authorize]
public class ReceiptController : Controller
{
    private readonly OnlineLearningAppDbContext _context;
    public ReceiptController(OnlineLearningAppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> View(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();

        var query = _context.Orders.AsNoTracking()
            .Include(o => o.OrderItems).ThenInclude(i => i.Course)
            .Where(o => o.Id == id);

        if (!User.IsInRole(UserRoles.Admin)) query = query.Where(o => o.AccountId == userId);
        var order = await query.SingleOrDefaultAsync();
        if (order == null) return NotFound();
        if (!order.PaymentStatus.ToString().Equals("Paid", StringComparison.OrdinalIgnoreCase)) return BadRequest("A receipt is available only for confirmed payments.");
        return base.View(order);
    }
}
