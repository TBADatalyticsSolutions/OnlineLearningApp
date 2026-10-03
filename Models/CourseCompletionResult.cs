namespace OnlineLearningApp.Models;

public sealed record CourseCompletionResult(
    bool IsCompleted,
    bool ModulesCompleted,
    bool QuizzesCompleted,
    bool PaymentCompleted,
    bool CapstoneCompleted,
    int CompletedModules,
    int TotalModules,
    int PassedQuizzes,
    int RequiredQuizzes,
    Certificate? Certificate);
