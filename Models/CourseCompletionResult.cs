namespace OnlineLearningApp.Models;

public sealed record CourseCompletionResult(
    bool IsCompleted,
    bool ModulesCompleted,
    bool QuizzesCompleted,
    int CompletedModules,
    int TotalModules,
    int PassedQuizzes,
    int RequiredQuizzes,
    Certificate? Certificate);
