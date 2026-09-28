using OnlineLearningApp.Models;

namespace OnlineLearningApp.Data.Services;

public interface ICourseCompletionService
{
    Task<CourseCompletionResult> EvaluateAsync(string studentId, int courseId, bool issueCertificate = true);
}
