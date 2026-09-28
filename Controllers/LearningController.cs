using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
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

        var courses = await _context.StudentCourses
            .AsNoTracking()
            .Where(sc => sc.StudentId == studentId)
            .Include(sc => sc.Course)
                .ThenInclude(c => c.Instructor)
            .Include(sc => sc.Course)
                .ThenInclude(c => c.Modules)
                    .ThenInclude(m => m.Quizzes)
            .OrderByDescending(sc => sc.EnrollmentDate)
            .Select(sc => sc.Course)
            .ToListAsync();

        return View(courses);
    }
}
