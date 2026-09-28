using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace OnlineLearningApp.Models;

public class Account : IdentityUser
{
    [Key]
    public string FullName { get; set; } = default!;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Role { get; set; } = default!;

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    public List<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
    public virtual ICollection<StudentModuleProgress> ModuleProgress { get; set; } = new List<StudentModuleProgress>();
    public virtual ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
