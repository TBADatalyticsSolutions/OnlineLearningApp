using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineLearningApp.Models;

public class Quiz
{
    [Key]
        public int QuizId { get; set; }
        public string QuizName { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal PassMark { get; set; } = 70m;
        public DateTime DateCreated { get; set; }
        public int ModuleId { get; set; }
        [ForeignKey("ModuleId")]
        public virtual Module Module { get; set; } = default!;
        // Navigation properties
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}