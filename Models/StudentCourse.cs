namespace OnlineLearningApp.Models;

public class StudentCourse
{
    public string StudentId { get; set; } = default!;
    public Account Student { get; set; } = default!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;
    public DateTime EnrollmentDate { get; set; } = default!;
    public DateTime? CompletedAt { get; set; }
}
