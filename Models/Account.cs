using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace OnlineLearningApp.Models;

public class Account : IdentityUser
{
    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    // Kept for backward compatibility with the existing schema.
    // Authorization is enforced through ASP.NET Core Identity roles.
    [Required]
    [StringLength(50)]
    public string Role { get; set; } = UserRoles.Student;

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    public virtual ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
    public virtual ICollection<StudentModuleProgress> ModuleProgress { get; set; } = new List<StudentModuleProgress>();
    public virtual ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
