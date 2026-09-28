namespace OnlineLearningApp.Models;

public class QuizHistoryViewModel
{
    public List<QuizHistoryItemViewModel> Attempts { get; set; } = new();
}

public class QuizHistoryItemViewModel
{
    public string QuizName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public decimal Percentage { get; set; }
    public bool Passed { get; set; }
    public DateTime AttemptedAt { get; set; }
}
