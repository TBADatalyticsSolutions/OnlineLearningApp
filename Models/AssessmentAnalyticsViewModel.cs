namespace OnlineLearningApp.Models;

public class StudentAssessmentAnalyticsViewModel
{
    public int TotalAttempts { get; set; }
    public int PassedAttempts { get; set; }
    public int FailedAttempts => TotalAttempts - PassedAttempts;
    public decimal AveragePercentage { get; set; }
    public decimal BestPercentage { get; set; }
    public int CoursesAssessed { get; set; }
    public List<AssessmentAnalyticsRow> Assessments { get; set; } = new();
}

public class AssessmentAnalyticsRow
{
    public string CourseName { get; set; } = string.Empty;
    public string QuizName { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public decimal BestPercentage { get; set; }
    public decimal LatestPercentage { get; set; }
    public bool Passed { get; set; }
    public DateTime? LastAttemptedAt { get; set; }
}

public class InstructorPerformanceViewModel
{
    public int CourseCount { get; set; }
    public int ActiveLearners { get; set; }
    public int CompletedLearners { get; set; }
    public int TotalAttempts { get; set; }
    public decimal AverageAssessmentScore { get; set; }
    public decimal PassRate { get; set; }
    public List<InstructorCoursePerformance> Courses { get; set; } = new();
}

public class InstructorCoursePerformance
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int Learners { get; set; }
    public int CompletedLearners { get; set; }
    public int QuizAttempts { get; set; }
    public decimal AverageScore { get; set; }
    public decimal PassRate { get; set; }
    public decimal CompletionRate { get; set; }
}
