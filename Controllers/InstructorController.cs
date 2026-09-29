using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;
using System.Security.Claims;

namespace OnlineLearningApp.Controllers;

[Authorize(Roles = UserRoles.Instructor)]
public class InstructorController : Controller
{
    private readonly OnlineLearningAppDbContext _context;

    public InstructorController(OnlineLearningAppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(instructorId))
        {
            return Challenge();
        }

        var courses = await _context.Courses
            .AsNoTracking()
            .Where(c => c.InstructorId == instructorId)
            .Include(c => c.Modules)
                .ThenInclude(m => m.Quizzes)
            .OrderByDescending(c => c.StartDate)
            .Select(c => new
            {
                c.Id,
                c.CourseName,
                c.Category,
                c.Status,
                ModuleCount = c.Modules.Count,
                QuizCount = c.Modules.SelectMany(m => m.Quizzes).Count(),
                StudentCount = _context.StudentCourses.Count(sc => sc.CourseId == c.Id)
            })
            .ToListAsync();

        var model = new InstructorDashboardViewModel
        {
            Courses = courses.Select(c => new InstructorCourseViewModel
            {
                CourseId = c.Id,
                CourseName = c.CourseName,
                Category = c.Category.GetDescription(),
                Status = c.Status.ToString(),
                ModuleCount = c.ModuleCount,
                QuizCount = c.QuizCount,
                StudentCount = c.StudentCount
            }).ToList()
        };

        return View(model);
    }
}
