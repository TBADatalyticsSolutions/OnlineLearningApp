namespace OnlineLearningApp.Models;

public class LearningDashboardViewModel
{
    public List<LearningCourseProgressViewModel> Courses { get; set; } = new();
    public int EnrolledCount => Courses.Count;
    public int CompletedCount => Courses.Count(c => c.IsCompleted);
    public int TotalModulesCompleted => Courses.Sum(c => c.CompletedModules);
    public int TotalModules => Courses.Sum(c => c.TotalModules);
    public int TotalQuizAttempts { get; set; }
    public decimal AverageQuizPercentage { get; set; }
}

public class LearningCourseProgressViewModel
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageURL { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int TotalModules { get; set; }
    public int CompletedModules { get; set; }
    public int ProgressPercentage { get; set; }
    public int QuizCount { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public DateTime? CompletedAt { get; set; }
}
