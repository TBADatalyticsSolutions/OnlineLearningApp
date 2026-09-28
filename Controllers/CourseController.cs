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

        var enrolled = await _context.StudentCourses
            .AnyAsync(sc => sc.StudentId == studentId && sc.CourseId == id);

        if (!enrolled)
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

        return View(course);
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
