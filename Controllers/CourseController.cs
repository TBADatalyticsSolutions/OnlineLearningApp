using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;
using System.Security.Claims;

namespace OnlineLearningApp.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class CourseController : Controller
{
    private readonly ICourseService _service;
    private readonly OnlineLearningAppDbContext _context;

    public CourseController(ICourseService service, OnlineLearningAppDbContext context)
    {
        _service = service;
        _context = context;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var allCourses = await _service.GetAllAsync();
        return View(allCourses);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Filter(string searchString)
    {
        var allCourses = await _service.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var term = searchString.Trim();

            allCourses = allCourses
                .Where(course =>
                    course.CourseName.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                    course.Description.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                    course.Category.ToString().Contains(term, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
        }

        return View("Index", allCourses);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Instructor)
            .Include(c => c.Modules)
                .ThenInclude(m => m.Quizzes)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
        {
            return View("NotFound");
        }

        var isEnrolled = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(studentId))
            {
                isEnrolled = await _context.StudentCourses
                    .AnyAsync(sc => sc.StudentId == studentId && sc.CourseId == course.Id);
            }
        }

        ViewBag.IsEnrolled = isEnrolled;
        return View(course);
    }

    [Authorize(Roles = UserRoles.Student)]
    public async Task<IActionResult> Learn(int id)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Challenge();
        }

        var enrollment = await _context.StudentCourses
            .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == id);

        if (enrollment == null)
        {
            TempData["Error"] = "Please enroll in this course before starting the lessons.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var course = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Instructor)
            .Include(c => c.Modules.OrderBy(m => m.ModuleId))
                .ThenInclude(m => m.Quizzes)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
        {
            return View("NotFound");
        }

        var completedModuleIds = await _context.StudentModuleProgress
            .AsNoTracking()
            .Where(p => p.StudentId == studentId && p.CompletedAt != null)
            .Select(p => p.ModuleId)
            .ToListAsync();

        var completedCount = course.Modules.Count(m => completedModuleIds.Contains(m.ModuleId));

        ViewBag.CompletedModuleIds = completedModuleIds;
        ViewBag.CompletedModuleCount = completedCount;
        ViewBag.ProgressPercentage = course.Modules.Count == 0
            ? 0
            : (int)Math.Round(completedCount * 100m / course.Modules.Count);
        ViewBag.IsCourseCompleted = enrollment.CompletedAt.HasValue;

        return View(course);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Student)]
    public async Task<IActionResult> CompleteModule(int courseId, int moduleId)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Challenge();
        }

        var enrollment = await _context.StudentCourses
            .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

        if (enrollment == null)
        {
            TempData["Error"] = "You must be enrolled in this course before completing lessons.";
            return RedirectToAction(nameof(Details), new { id = courseId });
        }

        var moduleExists = await _context.Modules
            .AnyAsync(m => m.ModuleId == moduleId && m.CourseId == courseId);

        if (!moduleExists)
        {
            return NotFound();
        }

        var progress = await _context.StudentModuleProgress
            .FirstOrDefaultAsync(p => p.StudentId == studentId && p.ModuleId == moduleId);

        if (progress == null)
        {
            progress = new StudentModuleProgress
            {
                StudentId = studentId,
                ModuleId = moduleId,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            _context.StudentModuleProgress.Add(progress);
        }
        else if (!progress.CompletedAt.HasValue)
        {
            progress.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var moduleCount = await _context.Modules.CountAsync(m => m.CourseId == courseId);
        var completedCount = await _context.StudentModuleProgress
            .Where(p => p.StudentId == studentId && p.CompletedAt != null)
            .Join(_context.Modules.Where(m => m.CourseId == courseId),
                progressRow => progressRow.ModuleId,
                module => module.ModuleId,
                (_, _) => 1)
            .CountAsync();

        if (moduleCount > 0 && completedCount >= moduleCount)
        {
            enrollment.CompletedAt ??= DateTime.UtcNow;

            var certificateExists = await _context.Certificates
                .AnyAsync(c => c.StudentId == studentId && c.CourseId == courseId);

            if (!certificateExists)
            {
                _context.Certificates.Add(new Certificate
                {
                    CertificateNumber = $"TBA-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                    StudentId = studentId,
                    CourseId = courseId,
                    IssuedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Course completed. Your certificate is now available.";
        }
        else
        {
            TempData["Success"] = "Module marked as complete. Keep going!";
        }

        return RedirectToAction(nameof(Learn), new { id = courseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Student)]
    public async Task<IActionResult> Enroll(int id)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Challenge();
        }

        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
        {
            return View("NotFound");
        }

        var alreadyEnrolled = await _context.StudentCourses
            .AnyAsync(sc => sc.StudentId == studentId && sc.CourseId == id);

        if (!alreadyEnrolled)
        {
            _context.StudentCourses.Add(new StudentCourse
            {
                StudentId = studentId,
                CourseId = id,
                EnrollmentDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"You are now enrolled in {course.CourseName}.";
        }
        else
        {
            TempData["Success"] = "You are already enrolled in this course.";
        }

        return RedirectToAction(nameof(Learn), new { id });
    }

    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Create()
    {
        var courseDropdownsData = await _service.GetNewCourseDropdownsValues();

        ViewBag.Categories = new SelectList(courseDropdownsData.Categories, "Id", "Name");
        ViewBag.Instructors = new SelectList(courseDropdownsData.Instructors, "UserId", "FullName");

        return View();
    }

    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Create(NewCourseViewModel course)
    {
        if (!ModelState.IsValid)
        {
            var courseDropdownsData = await _service.GetNewCourseDropdownsValues();

            ViewBag.Categories = new SelectList(courseDropdownsData.Categories, "Id", "Name");
            ViewBag.Instructors = new SelectList(courseDropdownsData.Instructors, "UserId", "FullName");

            return View(course);
        }

        await _service.AddNewCourseAsync(course);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var courseDetails = await _service.GetCourseByIdAsync(id);
        if (courseDetails == null)
        {
            return View("NotFound");
        }

        var response = new NewCourseViewModel
        {
            Id = courseDetails.Id,
            CourseName = courseDetails.CourseName,
            Description = courseDetails.Description,
            Price = courseDetails.Price,
            StartDate = courseDetails.StartDate,
            EndDate = courseDetails.EndDate,
            ImageURL = courseDetails.ImageURL,
            Category = courseDetails.Category,
            InstructorId = courseDetails.InstructorId,
            ModuleIds = courseDetails.Courses_Modules.Select(n => n.ModuleId).ToList(),
        };

        var courseDropdownsData = await _service.GetNewCourseDropdownsValues();
        ViewBag.Categories = new SelectList(courseDropdownsData.Categories, "Id", "Name");
        ViewBag.Instructors = new SelectList(courseDropdownsData.Instructors, "Id", "FullName");

        return View(response);
    }

    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Edit(int id, NewCourseViewModel course)
    {
        if (id != course.Id)
        {
            return View("NotFound");
        }

        if (!ModelState.IsValid)
        {
            var courseDropdownsData = await _service.GetNewCourseDropdownsValues();

            ViewBag.Categories = new SelectList(courseDropdownsData.Categories, "Id", "Name");
            ViewBag.Instructors = new SelectList(courseDropdownsData.Instructors, "Id", "FullName");

            return View(course);
        }

        await _service.UpdateCourseAsync(course);
        return RedirectToAction(nameof(Index));
    }
}
