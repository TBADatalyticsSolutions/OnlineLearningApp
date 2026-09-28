using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;
using System.Security.Claims;

namespace OnlineLearningApp.Controllers;

[Authorize(Roles = UserRoles.Student)]
public class LearningController : Controller
{
    private readonly OnlineLearningAppDbContext _context;

    public LearningController(OnlineLearningAppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Challenge();
        }

        var enrollments = await _context.StudentCourses
            .AsNoTracking()
            .Where(sc => sc.StudentId == studentId)
            .Include(sc => sc.Course)
            .OrderByDescending(sc => sc.EnrollmentDate)
            .ToListAsync();

        var courseIds = enrollments.Select(e => e.CourseId).ToList();

        var completedModuleIds = await _context.StudentModuleProgress
            .AsNoTracking()
            .Where(p => p.StudentId == studentId && p.CompletedAt != null)
            .Join(
                _context.Modules.Where(m => courseIds.Contains(m.CourseId)),
                p => p.ModuleId,
                m => m.ModuleId,
                (_, m) => new { m.CourseId, m.ModuleId })
            .ToListAsync();

        var quizStats = await _context.QuizAttempts
            .AsNoTracking()
            .Where(a => a.StudentId == studentId)
            .GroupBy(a => a.QuizId)
            .Select(g => new
            {
                Attempts = g.Count(),
                Average = g.Average(a => a.Percentage)
            })
            .ToListAsync();

        var totalAttempts = await _context.QuizAttempts
            .AsNoTracking()
            .CountAsync(a => a.StudentId == studentId);

        var averageQuizPercentage = totalAttempts == 0
            ? 0m
            : await _context.QuizAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == studentId)
                .AverageAsync(a => a.Percentage);

        var model = new LearningDashboardViewModel
        {
            TotalQuizAttempts = totalAttempts,
            AverageQuizPercentage = Math.Round(averageQuizPercentage, 2)
        };

        foreach (var enrollment in enrollments)
        {
            var course = enrollment.Course;
            var totalModules = await _context.Modules.CountAsync(m => m.CourseId == course.Id);
            var completedModules = completedModuleIds.Count(x => x.CourseId == course.Id);
            var progress = totalModules == 0
                ? 0
                : (int)Math.Round(completedModules * 100m / totalModules);

            model.Courses.Add(new LearningCourseProgressViewModel
            {
                CourseId = course.Id,
                CourseName = course.CourseName,
                Description = course.Description,
                ImageURL = course.ImageURL,
                Category = course.Category.ToString(),
                TotalModules = totalModules,
                CompletedModules = completedModules,
                ProgressPercentage = progress,
                QuizCount = await _context.Quizzes
                    .AsNoTracking()
                    .Where(q => q.Module.CourseId == course.Id)
                    .CountAsync(),
                IsCompleted = enrollment.CompletedAt.HasValue,
                EnrollmentDate = enrollment.EnrollmentDate,
                CompletedAt = enrollment.CompletedAt
            });
        }

        return View(model);
    }
}
