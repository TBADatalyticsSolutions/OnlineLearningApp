using Microsoft.AspNetCore.Mvc;
using OnlineLearningApp.Data.Cart;
using System.Threading.Tasks;
using OnlineLearningApp.Models;

namespace OnlineLearningApp;

public class ShoppingCartSummary : ViewComponent
{
    private readonly ShoppingCart _shoppingCart;

    public ShoppingCartSummary(ShoppingCart shoppingCart)
    {
        _shoppingCart = shoppingCart;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var items = await _shoppingCart.GetShoppingCartItemsAsync();
        _shoppingCart.ShoppingCartItems = items;

        var shoppingCartItemCount = items?.Sum(item => item.Amount) ?? 0;

        return View(shoppingCartItemCount); // returning an int
    }
}