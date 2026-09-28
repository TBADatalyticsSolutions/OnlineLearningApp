namespace OnlineLearningApp.Models;

public class QuizAttemptViewModel
{
    public int QuizId { get; set; }
    public string QuizName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public List<QuizQuestionViewModel> Questions { get; set; } = new();
    public Dictionary<int, int> Answers { get; set; } = new();
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public bool Submitted { get; set; }
    public int? LastScore { get; set; }
    public int? LastTotalQuestions { get; set; }
    public decimal? LastPercentage { get; set; }
    public DateTime? AttemptedAt { get; set; }
}

public class QuizQuestionViewModel
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<QuizOptionViewModel> Options { get; set; } = new();
}

public class QuizOptionViewModel
{
    public int OptionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
}
