namespace OnlineLearningApp.Models;

public class InstructorDashboardViewModel
{
    public List<InstructorCourseViewModel> Courses { get; set; } = new();

    public int CourseCount => Courses.Count;
    public int ModuleCount => Courses.Sum(c => c.ModuleCount);
    public int QuizCount => Courses.Sum(c => c.QuizCount);
    public int StudentCount => Courses.Sum(c => c.StudentCount);
}

public class InstructorCourseViewModel
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ModuleCount { get; set; }
    public int QuizCount { get; set; }
    public int StudentCount { get; set; }
}
