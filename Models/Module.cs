using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OnlineLearningApp.Models;

public class Module
{
    [Key]
        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = default!;
        public string Content { get; set; } = default!;
        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public virtual Course Course { get; set; } = default!;
        // Navigation properties
        public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
        public ICollection<Course_Module> Courses_Modules { get; set; } = new List<Course_Module>();

}