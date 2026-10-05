using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;

namespace OnlineLearningApp;

public class OrderService : IOrderService
{
    private readonly OnlineLearningAppDbContext _context;
    private readonly TimeProvider _clock;

    public OrderService(OnlineLearningAppDbContext context, TimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<List<Order>> GetOrdersByUserIdAndRoleAsync(string userId, string userRole)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(item => item.Course)
            .Include(o => o.Account)
            .AsQueryable();

        if (!string.Equals(userRole, UserRoles.Admin, StringComparison.Ordinal))
        {
            query = query.Where(o => o.AccountId == userId);
        }

        return await query
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task StoreOrderAsync(
        List<ShoppingCartItem> items,
        string userId,
        string userEmailAddress)
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("Cannot create an order from an empty cart.");
        }

        var totalAmount = items.Sum(item => item.Course.Price * item.Amount);

        var order = new Order
        {
            AccountId = userId,
            Email = userEmailAddress,
            OrderDate = _clock.GetUtcNow().UtcDateTime,
            TotalAmount = totalAmount
        };

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        foreach (var item in items)
        {
            await _context.OrderItems.AddAsync(new OrderItem
            {
                Amount = item.Amount,
                Quantity = item.Amount,
                CourseId = item.Course.Id,
                OrderId = order.Id,
                Price = item.Course.Price
            });
        }

        await _context.SaveChangesAsync();
    }
}
