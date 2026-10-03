using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;

namespace OnlineLearningApp.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class FinanceController : Controller
{
    private readonly OnlineLearningAppDbContext _context;
    public FinanceController(OnlineLearningAppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Account)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Course)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var paid = orders.Where(o => o.PaymentStatus.ToString().Equals("Paid", StringComparison.OrdinalIgnoreCase)).ToList();
        var pending = orders.Count(o => o.PaymentStatus.ToString().Equals("Pending", StringComparison.OrdinalIgnoreCase));
        var failed = orders.Count(o => o.PaymentStatus.ToString().Equals("Failed", StringComparison.OrdinalIgnoreCase));

        ViewBag.TotalOrders = orders.Count;
        ViewBag.PaidOrders = paid.Count;
        ViewBag.PendingOrders = pending;
        ViewBag.FailedOrders = failed;
        ViewBag.GrossRevenue = paid.Sum(o => o.TotalAmount);
        ViewBag.Currency = "NGN";
        return View(orders);
    }
}
