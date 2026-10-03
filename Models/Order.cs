using System.ComponentModel.DataAnnotations;
using OnlineLearningApp.Models;

namespace OnlineLearningApp;

public class Order
{
    public int Id { get; set; }
    [Required]
    public string Email { get; set; } = default!;
    [Required]
    public string AccountId { get; set; } = default!;
    [Required]
    public DateTime OrderDate { get; set; }
    [Required]
    public decimal TotalAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    [MaxLength(120)]
    public string? PaymentReference { get; set; }
    public DateTime? PaidAt { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual Account Account { get; set; } = default!;
}
