using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;
using System.Security.Claims;

namespace OnlineLearningApp.Controllers;

[Authorize]
public class AnalyticsController : Controller
{
    private readonly OnlineLearningAppDbContext _context;

    public AnalyticsController(OnlineLearningAppDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = UserRoles.Student)]
    [HttpGet]
    public async Task<IActionResult> Assessments()
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId))
            return Challenge();

        var attempts = await _context.QuizAttempts
            .AsNoTracking()
            .Where(a => a.StudentId == studentId)
            .Include(a => a.Quiz)
                .ThenInclude(q => q.Module)
                    .ThenInclude(m => m.Course)
            .ToListAsync();

        var rows = attempts
            .GroupBy(a => new { a.QuizId, CourseName = a.Quiz.Module.Course.CourseName, QuizName = a.Quiz.QuizName })
            .Select(g =>
            {
                var latest = g.OrderByDescending(a => a.AttemptedAt).First();
                return new AssessmentAnalyticsRow
                {
                    CourseName = g.Key.CourseName,
                    QuizName = g.Key.QuizName,
                    Attempts = g.Count(),
                    BestPercentage = g.Max(a => a.Percentage),
                    LatestPercentage = latest.Percentage,
                    Passed = g.Any(a => a.Passed),
                    LastAttemptedAt = latest.AttemptedAt
                };
            })
            .OrderBy(r => r.CourseName)
            .ThenBy(r => r.QuizName)
            .ToList();

        var model = new StudentAssessmentAnalyticsViewModel
        {
            TotalAttempts = attempts.Count,
            PassedAttempts = attempts.Count(a => a.Passed),
            AveragePercentage = attempts.Count == 0 ? 0 : Math.Round(attempts.Average(a => a.Percentage), 2),
            BestPercentage = attempts.Count == 0 ? 0 : attempts.Max(a => a.Percentage),
            CoursesAssessed = attempts.Select(a => a.Quiz.Module.CourseId).Distinct().Count(),
            Assessments = rows
        };

        return View(model);
    }

    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Instructor)]
    [HttpGet]
    public async Task<IActionResult> InstructorPerformance()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var query = _context.Courses.AsNoTracking();

        if (User.IsInRole(UserRoles.Instructor))
        {
            query = query.Where(c => c.InstructorId == userId);
        }

        var courses = await query
            .Include(c => c.StudentCourses)
            .Include(c => c.Modules)
                .ThenInclude(m => m.Quizzes)
            .ToListAsync();

        var courseIds = courses.Select(c => c.Id).ToList();

        var attempts = await _context.QuizAttempts
            .AsNoTracking()
            .Include(a => a.Quiz)
                .ThenInclude(q => q.Module)
            .Where(a => courseIds.Contains(a.Quiz.Module.CourseId))
            .ToListAsync();

        var progress = await _context.StudentModuleProgress
            .AsNoTracking()
            .Where(p => courseIds.Contains(p.Module.CourseId))
            .ToListAsync();

        var rows = courses.Select(course =>
        {
            var courseAttempts = attempts.Where(a => a.Quiz.Module.CourseId == course.Id).ToList();
            var moduleIds = course.Modules.Select(m => m.ModuleId).ToHashSet();
            var learnerIds = course.StudentCourses.Select(sc => sc.StudentId).ToHashSet();

            var completedByLearner = learnerIds.Count == 0
                ? 0
                : learnerIds.Count(studentId =>
                    moduleIds.Count == 0 ||
                    moduleIds.All(moduleId => progress.Any(p =>
                        p.StudentId == studentId &&
                        p.ModuleId == moduleId &&
                        p.CompletedAt != null)));

            return new InstructorCoursePerformance
            {
                CourseId = course.Id,
                CourseName = course.CourseName,
                Learners = learnerIds.Count,
                CompletedLearners = completedByLearner,
                QuizAttempts = courseAttempts.Count,
                AverageScore = courseAttempts.Count == 0 ? 0 : Math.Round(courseAttempts.Average(a => a.Percentage), 2),
                PassRate = courseAttempts.Count == 0 ? 0 : Math.Round(courseAttempts.Count(a => a.Passed) * 100m / courseAttempts.Count, 2),
                CompletionRate = learnerIds.Count == 0 ? 0 : Math.Round(completedByLearner * 100m / learnerIds.Count, 2)
            };
        }).ToList();

        var allAttempts = attempts;
        var model = new InstructorPerformanceViewModel
        {
            CourseCount = courses.Count,
            ActiveLearners = courses.SelectMany(c => c.StudentCourses).Select(sc => sc.StudentId).Distinct().Count(),
            CompletedLearners = courses.SelectMany(c => c.StudentCourses).Count(sc => sc.CompletedAt != null),
            TotalAttempts = allAttempts.Count,
            AverageAssessmentScore = allAttempts.Count == 0 ? 0 : Math.Round(allAttempts.Average(a => a.Percentage), 2),
            PassRate = allAttempts.Count == 0 ? 0 : Math.Round(allAttempts.Count(a => a.Passed) * 100m / allAttempts.Count, 2),
            Courses = rows
        };

        return View(model);
    }
}
