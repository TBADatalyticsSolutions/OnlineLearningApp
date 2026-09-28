using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineLearningApp.Data.Cart;
using System.Security.Claims;

namespace OnlineLearningApp.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly ICourseService _courseService;
    private readonly ShoppingCart _shoppingCart;
    private readonly IOrderService _ordersService;

    public OrdersController(
        ICourseService courseService,
        ShoppingCart shoppingCart,
        IOrderService ordersService)
    {
        _courseService = courseService;
        _shoppingCart = shoppingCart;
        _ordersService = ordersService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCartItemCount()
    {
        var items = await _shoppingCart.GetShoppingCartItemsAsync();
        return Json(new { count = items.Sum(i => i.Amount) });
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var isAdmin = User.IsInRole(UserRoles.Admin);
        var orders = await _ordersService.GetOrdersByUserIdAndRoleAsync(
            userId,
            isAdmin ? UserRoles.Admin : UserRoles.Student);

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> ShoppingCart()
    {
        var items = await _shoppingCart.GetShoppingCartItemsAsync();

        var response = new ShoppingCartViewModel
        {
            ShoppingCart = _shoppingCart,
            ShoppingCartItems = items,
            ShoppingCartTotal = await _shoppingCart.GetShoppingCartTotalAsync()
        };

        return View(response);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItemToShoppingCart(int id)
    {
        var item = await _courseService.GetCourseByIdAsync(id);

        if (item == null)
        {
            return NotFound();
        }

        await _shoppingCart.AddItemToCartAsync(item);
        TempData["Success"] = "Course added to your cart.";
        return RedirectToAction(nameof(ShoppingCart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItemFromShoppingCart(int id)
    {
        var item = await _courseService.GetCourseByIdAsync(id);

        if (item == null)
        {
            return NotFound();
        }

        await _shoppingCart.RemoveItemFromCartAsync(item);
        TempData["Success"] = "Course removed from your cart.";
        return RedirectToAction(nameof(ShoppingCart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteOrder()
    {
        var items = await _shoppingCart.GetShoppingCartItemsAsync();
        if (items.Count == 0)
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction(nameof(ShoppingCart));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmailAddress = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(userEmailAddress))
        {
            return Challenge();
        }

        await _ordersService.StoreOrderAsync(items, userId, userEmailAddress);
        await _shoppingCart.ClearShoppingCartAsync();

        TempData["Success"] = "Your order has been recorded successfully.";
        return View("OrderCompleted");
    }
}
