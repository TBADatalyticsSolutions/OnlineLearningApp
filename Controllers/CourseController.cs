using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;
using System.Security.Claims;

namespace OnlineLearningApp.Controllers;

public class CourseController : Controller
{
    private readonly ICourseService _service;
    private readonly ICourseCompletionService _completionService;
    private readonly OnlineLearningAppDbContext _context;

    public CourseController(ICourseService service, OnlineLearningAppDbContext context)
    {
        _service = service;
        _completionService = completionService;
        _context = context;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var allCourses = await _service.GetAllAsync();
        return View(allCourses);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Filter(string? searchString)
    {
        var allCourses = await _service.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var term = searchString.Trim();

            allCourses = allCourses
                .Where(course =>
                    course.CourseName.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                    course.Description.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                    course.Category.GetDescription().Contains(term, StringComparison.CurrentCultureIgnoreCase))
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

        if (course is null)
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

        if (enrollment is null)
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

        if (course is null)
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

        if (enrollment is null)
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

        if (progress is null)
        {
            _context.StudentModuleProgress.Add(new StudentModuleProgress
            {
                StudentId = studentId,
                ModuleId = moduleId,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            });
        }
        else if (!progress.CompletedAt.HasValue)
        {
            progress.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var moduleCount = await _context.Modules.CountAsync(m => m.CourseId == courseId);
        var completedCount = await _context.StudentModuleProgress
            .Where(p => p.StudentId == studentId && p.CompletedAt != null)
            .Join(
                _context.Modules.Where(m => m.CourseId == courseId),
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

        if (course is null)
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
        await PopulateCourseDropdownsAsync();
        return View();
    }

    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Create(NewCourseViewModel course)
    {
        await ValidateInstructorAsync(course.InstructorId);

        if (!ModelState.IsValid)
        {
            await PopulateCourseDropdownsAsync(course.InstructorId, course.Category);
            return View(course);
        }

        await _service.AddNewCourseAsync(course);
        TempData["Success"] = $"Course '{course.CourseName}' was created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var courseDetails = await _service.GetCourseByIdAsync(id);
        if (courseDetails is null)
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
            Status = courseDetails.Status,
            InstructorId = courseDetails.InstructorId,
            ModuleIds = courseDetails.Courses_Modules.Select(n => n.ModuleId).ToList()
        };

        await PopulateCourseDropdownsAsync(response.InstructorId, response.Category);
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

        await ValidateInstructorAsync(course.InstructorId);

        if (!ModelState.IsValid)
        {
            await PopulateCourseDropdownsAsync(course.InstructorId, course.Category);
            return View(course);
        }

        await _service.UpdateCourseAsync(course);
        TempData["Success"] = $"Course '{course.CourseName}' was updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateInstructorAsync(string instructorId)
    {
        if (string.IsNullOrWhiteSpace(instructorId))
        {
            ModelState.AddModelError(nameof(NewCourseViewModel.InstructorId), "Please select an instructor.");
            return;
        }

        var instructorRoleId = await _context.Roles
            .Where(r => r.Name == UserRoles.Instructor)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var isInstructor = instructorRoleId is not null &&
            await _context.UserRoles.AnyAsync(ur => ur.UserId == instructorId && ur.RoleId == instructorRoleId);

        if (!isInstructor)
        {
            ModelState.AddModelError(nameof(NewCourseViewModel.InstructorId), "The selected account is not an instructor.");
        }
    }

    private async Task PopulateCourseDropdownsAsync(
        string? selectedInstructorId = null,
        CourseCategory? selectedCategory = null)
    {
        var dropdownData = await _service.GetNewCourseDropdownsValues();

        ViewBag.Categories = dropdownData.Categories
            .Select(category => new SelectListItem
            {
                Value = ((int)category).ToString(),
                Text = category.GetDescription(),
                Selected = selectedCategory.HasValue && category == selectedCategory.Value
            })
            .ToList();

        ViewBag.Instructors = dropdownData.Instructors
            .Select(instructor => new SelectListItem
            {
                Value = instructor.UserId,
                Text = instructor.FullName,
                Selected = instructor.UserId == selectedInstructorId
            })
            .ToList();
    }
}
