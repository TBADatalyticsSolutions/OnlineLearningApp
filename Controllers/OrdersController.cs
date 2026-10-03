using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Data.Cart;
using OnlineLearningApp.Models;
using System.Security.Claims;

namespace OnlineLearningApp.Controllers;

[Authorize(Roles = UserRoles.Admin + "," + UserRoles.Student)]
public class OrdersController : Controller
{
    private readonly ICourseService _courseService;
    private readonly ShoppingCart _shoppingCart;
    private readonly IOrderService _ordersService;
    private readonly OnlineLearningAppDbContext _context;

    public OrdersController(ICourseService courseService, ShoppingCart shoppingCart, IOrderService ordersService, OnlineLearningAppDbContext context)
    {
        _courseService = courseService; _shoppingCart = shoppingCart; _ordersService = ordersService; _context = context;
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.Student)]
    public async Task<IActionResult> GetCartItemCount() => Json(new { count = (await _shoppingCart.GetShoppingCartItemsAsync()).Sum(i => i.Amount) });

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();
        var isAdmin = User.IsInRole(UserRoles.Admin);
        var orders = await _ordersService.GetOrdersByUserIdAndRoleAsync(userId, isAdmin ? UserRoles.Admin : UserRoles.Student);
        return View(orders);
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.Student)]
    public async Task<IActionResult> ShoppingCart()
    {
        var items = await _shoppingCart.GetShoppingCartItemsAsync();
        return View(new ShoppingCartViewModel { ShoppingCart = _shoppingCart, ShoppingCartItems = items, ShoppingCartTotal = await _shoppingCart.GetShoppingCartTotalAsync() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Student)]
    public async Task<IActionResult> AddItemToShoppingCart(int id)
    {
        var item = await _courseService.GetCourseByIdAsync(id);
        if (item is null) return NotFound();
        await _shoppingCart.AddItemToCartAsync(item);
        TempData["Success"] = "Course added to your cart. Complete payment before graded assessments and certification are unlocked.";
        return RedirectToAction(nameof(ShoppingCart));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Student)]
    public async Task<IActionResult> RemoveItemFromShoppingCart(int id)
    {
        var item = await _courseService.GetCourseByIdAsync(id);
        if (item is null) return NotFound();
        await _shoppingCart.RemoveItemFromCartAsync(item);
        return RedirectToAction(nameof(ShoppingCart));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Student)]
    public async Task<IActionResult> CompleteOrder()
    {
        var items = await _shoppingCart.GetShoppingCartItemsAsync();
        if (items.Count == 0) { TempData["Error"] = "Your cart is empty."; return RedirectToAction(nameof(ShoppingCart)); }
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email)) return Challenge();

        // The order is intentionally Pending until a payment provider confirms settlement.
        // This prevents an order record from being treated as proof of payment.
        await _ordersService.StoreOrderAsync(items, userId, email);
        var order = await _context.Orders.Where(o => o.AccountId == userId).OrderByDescending(o => o.Id).FirstAsync();
        order.PaymentStatus = PaymentStatus.Pending;
        await _context.SaveChangesAsync();
        await _shoppingCart.ClearShoppingCartAsync();
        TempData["Success"] = "Order created. Payment confirmation is required before graded assessments and certificates are unlocked.";
        return View("OrderCompleted", order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> MarkPaid(int id, string? paymentReference)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();
        order.PaymentStatus = PaymentStatus.Paid;
        order.PaymentReference = string.IsNullOrWhiteSpace(paymentReference) ? $"MANUAL-{Guid.NewGuid():N}"[..20].ToUpperInvariant() : paymentReference.Trim();
        order.PaidAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Order #{id} marked as paid. The learner can now take graded assessments and become eligible for certification once all course requirements are met.";
        return RedirectToAction(nameof(Index));
    }
}
