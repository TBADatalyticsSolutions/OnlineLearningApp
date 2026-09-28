using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineLearningApp.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class CourseController : Controller
{
    private readonly ICourseService _service;

    public CourseController(ICourseService service)
    {
        _service = service;
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

    // GET: Courses/Details/1
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var courseDetail = await _service.GetCourseByIdAsync(id);
        if (courseDetail == null)
        {
            return View("NotFound");
        }

        return View(courseDetail);
    }

    // GET: Courses/Create
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

    // GET: Courses/Edit/1
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
