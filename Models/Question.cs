using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineLearningApp.Models;

public class Question
{
    [Key]
    public int QuestionId { get; set; }

    [Required, StringLength(2000)]
    public string QuestionText { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string QuestionType { get; set; } = "Multiple Choice";

    [Required, StringLength(20)]
    public string Difficulty { get; set; } = "Intermediate";

    [StringLength(2000)]
    public string Explanation { get; set; } = string.Empty;

    public int QuizId { get; set; }

    [ForeignKey(nameof(QuizId))]
    public virtual Quiz Quiz { get; set; } = default!;

    public virtual ICollection<Option> Options { get; set; } = new List<Option>();
}
