using OnlineLearningApp.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineLearningApp.Models;

public class Course : IEntityBase
{
    [Key]
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = default!;
    public string Description { get; set; } = default!;
    public CourseCategory Category { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Price { get; set; }
    public string ImageURL { get; set; } = default!;
    public CourseStatus Status { get; set; }
    public bool RequireAllQuizzesPassed { get; set; } = true;

    public bool RequirePaymentForAssessment { get; set; } = true;
    public bool RequireCapstone { get; set; } = true;
    [MaxLength(180)]
    public string CapstoneTitle { get; set; } = "Applied Capstone Project";
    public string CapstoneDescription { get; set; } = "Complete a practical project demonstrating the skills developed throughout this course.";
    public string CapstoneRequirements { get; set; } = "Submit a clear problem statement, methodology, working artefact or analysis, results, and a short reflection.";

    public virtual ICollection<Module> Modules { get; set; } = new List<Module>();
    public virtual ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
    public string InstructorId { get; set; } = default!;
    [ForeignKey("InstructorId")]
    public Account Instructor { get; set; } = default!;
    public ICollection<Course_Module> Courses_Modules { get; set; } = new List<Course_Module>();
}
