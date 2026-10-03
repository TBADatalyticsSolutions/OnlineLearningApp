using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineLearningApp.Models;

public class Quiz
{
    [Key]
    public int QuizId { get; set; }

    [Required, StringLength(200)]
    public string QuizName { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 100)]
    public decimal PassMark { get; set; } = 70m;

    /// <summary>Optional time limit. Null means the assessment is untimed.</summary>
    [Range(1, 180)]
    public int? TimeLimitMinutes { get; set; }

    /// <summary>Maximum number of attempts. Null means unlimited attempts.</summary>
    [Range(1, 100)]
    public int? AttemptLimit { get; set; }

    /// <summary>Practice assessments do not contribute to completion eligibility.</summary>
    public bool IsPractice { get; set; }

    public bool ShuffleQuestions { get; set; } = true;
    public bool ShuffleOptions { get; set; } = true;

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public int ModuleId { get; set; }

    [ForeignKey(nameof(ModuleId))]
    public virtual Module Module { get; set; } = default!;

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
