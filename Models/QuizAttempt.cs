using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineLearningApp.Models;

public class QuizAttempt
{
    [Key]
    public int Id { get; set; }

    public string StudentId { get; set; } = default!;
    public Account Student { get; set; } = default!;

    public int QuizId { get; set; }
    [ForeignKey(nameof(QuizId))]
    public Quiz Quiz { get; set; } = default!;

    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public decimal Percentage { get; set; }
    public DateTime AttemptedAt { get; set; }
}
