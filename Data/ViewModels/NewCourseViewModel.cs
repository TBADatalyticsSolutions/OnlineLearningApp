using System.ComponentModel.DataAnnotations;
using OnlineLearningApp.Data;

namespace OnlineLearningApp;

public class NewCourseViewModel : IValidatableObject
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string CourseName { get; set; } = string.Empty;
    [Required, StringLength(5000)] public string Description { get; set; } = string.Empty;
    [Required] public CourseCategory Category { get; set; }
    [Required, DataType(DataType.Date)] public DateTime StartDate { get; set; } = DateTime.Today;
    [Required, DataType(DataType.Date)] public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);
    [Required, Range(0, 999999999), DataType(DataType.Currency)] public decimal Price { get; set; }
    [Required, StringLength(500)] public string ImageURL { get; set; } = string.Empty;
    [Required] public string InstructorId { get; set; } = string.Empty;
    [Required] public CourseStatus Status { get; set; } = CourseStatus.Upcoming;
    public bool RequireAllQuizzesPassed { get; set; } = true;
    public bool RequirePaymentForAssessment { get; set; } = true;
    public bool RequireCapstone { get; set; } = true;
    [Required, StringLength(180)] public string CapstoneTitle { get; set; } = "Applied Capstone Project";
    [Required, StringLength(5000)] public string CapstoneDescription { get; set; } = string.Empty;
    [Required, StringLength(5000)] public string CapstoneRequirements { get; set; } = string.Empty;
    public List<User> Instructors { get; set; } = new(); public List<string> Categories { get; set; } = new(); public List<int> ModuleIds { get; set; } = new();
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext){if(EndDate<StartDate)yield return new ValidationResult("End date must be on or after the start date.",new[]{nameof(StartDate),nameof(EndDate)});}
}
