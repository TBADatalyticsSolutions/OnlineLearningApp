namespace OnlineLearningApp.Models;

public class CourseMasteryViewModel
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = default!;
    public decimal CompletionPercentage { get; set; }
    public decimal AssessmentMastery { get; set; }
    public decimal OverallMastery { get; set; }
    public int CompletedModules { get; set; }
    public int TotalModules { get; set; }
    public int PassedAssessments { get; set; }
    public int TotalAssessments { get; set; }
    public bool CapstoneApproved { get; set; }
    public bool Paid { get; set; }
}
