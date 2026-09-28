using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data.Services;
using OnlineLearningApp.Models;

namespace OnlineLearningApp.Data.Services.Implementation;

public sealed class CourseCompletionService : ICourseCompletionService
{
    private readonly OnlineLearningAppDbContext _context;

    public CourseCompletionService(OnlineLearningAppDbContext context)
    {
        _context = context;
    }

    public async Task<CourseCompletionResult> EvaluateAsync(
        string studentId,
        int courseId,
        bool issueCertificate = true)
    {
        var course = await _context.Courses
            .Include(c => c.Modules)
                .ThenInclude(m => m.Quizzes)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course is null)
        {
            throw new InvalidOperationException("Course was not found.");
        }

        var moduleIds = course.Modules.Select(m => m.ModuleId).ToList();

        var completedModules = await _context.StudentModuleProgress
            .Where(p => p.StudentId == studentId &&
                        p.CompletedAt != null &&
                        moduleIds.Contains(p.ModuleId))
            .Select(p => p.ModuleId)
            .Distinct()
            .CountAsync();

        var quizzes = course.Modules
            .SelectMany(m => m.Quizzes)
            .ToList();

        var quizIds = quizzes.Select(q => q.QuizId).ToList();

        var passedQuizIds = await _context.QuizAttempts
            .Where(a => a.StudentId == studentId &&
                        a.Passed &&
                        quizIds.Contains(a.QuizId))
            .Select(a => a.QuizId)
            .Distinct()
            .ToListAsync();

        var modulesCompleted = moduleIds.Count == 0 || completedModules == moduleIds.Count;
        var quizzesCompleted = !course.RequireAllQuizzesPassed ||
                               quizzes.Count == 0 ||
                               passedQuizIds.Count == quizzes.Count;
        var isCompleted = modulesCompleted && quizzesCompleted;

        Certificate? certificate = null;

        if (isCompleted)
        {
            var enrollment = await _context.StudentCourses
                .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

            if (enrollment is null)
            {
                throw new InvalidOperationException("Student is not enrolled in this course.");
            }

            enrollment.CompletedAt ??= DateTime.UtcNow;

            certificate = await _context.Certificates
                .FirstOrDefaultAsync(c => c.StudentId == studentId && c.CourseId == courseId);

            if (certificate is null && issueCertificate)
            {
                certificate = new Certificate
                {
                    CertificateNumber = $"TBA-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                    StudentId = studentId,
                    CourseId = courseId,
                    IssuedAt = DateTime.UtcNow
                };

                _context.Certificates.Add(certificate);
            }

            await _context.SaveChangesAsync();
        }

        return new CourseCompletionResult(
            isCompleted,
            modulesCompleted,
            quizzesCompleted,
            completedModules,
            moduleIds.Count,
            passedQuizIds.Count,
            quizzes.Count,
            certificate);
    }
}
