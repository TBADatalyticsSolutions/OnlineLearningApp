using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;

namespace OnlineLearningApp;

public class CourseService : EntityBaseRepository<Course>, ICourseService
{
    private readonly OnlineLearningAppDbContext _context;

    public CourseService(OnlineLearningAppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task AddNewCourseAsync(NewCourseViewModel data)
    {
        var newCourse = new Course()
        {
            CourseName = data.CourseName,
            Description = data.Description,
            Price = data.Price,
            ImageURL = data.ImageURL,
            Category = data.Category,
            StartDate = data.StartDate,
            EndDate = data.EndDate,
            InstructorId = data.InstructorId
        };
        await _context.Courses.AddAsync(newCourse);
        await _context.SaveChangesAsync();

        // Add Course Modules
        foreach (var moduleId in data.ModuleIds)
        {
            var newCourseModule = new Course_Module()
            {
                CourseId = newCourse.Id,
                ModuleId = moduleId
            };
            await _context.Courses_Modules.AddAsync(newCourseModule);
        }
        await _context.SaveChangesAsync();
    }

    public async Task<Course> GetCourseByIdAsync(int id)
    {
        var courseDetails = await _context.Courses
            .Include(i => i.Instructor)
            .Include(cm => cm.Courses_Modules).ThenInclude(m => m.Module)
            .FirstOrDefaultAsync(n => n.Id == id);

        return courseDetails!;
    }

    public async Task<NewCourseDropdownViewModel> GetNewCourseDropdownsValues()
    {
        var instructorRoleId = await _context.Roles
            .Where(r => r.Name == UserRoles.Instructor)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var instructors = await _context.Accounts
            .Where(a => instructorRoleId != null &&
                        _context.UserRoles.Any(ur => ur.UserId == a.Id && ur.RoleId == instructorRoleId))
            .OrderBy(a => a.FullName)
            .Select(a => new User
            {
                UserId = a.Id,
                FullName = a.FullName
            })
            .ToListAsync();

        var categories = Enum.GetValues(typeof(CourseCategory))
                        .Cast<CourseCategory>()
                        .Select(c => c.GetDescription()) // Convert to string descriptions
                        .ToList();

        var response = new NewCourseDropdownViewModel
        {
            Instructors = instructors,
            Categories = categories
        };
        return response;
    }

    public async Task UpdateCourseAsync(NewCourseViewModel data)
    {
        var dbCourse = await _context.Courses.FirstOrDefaultAsync(n => n.Id == data.Id);

        if (dbCourse != null)
        {
            dbCourse.CourseName = data.CourseName;
            dbCourse.Description = data.Description;
            dbCourse.Price = data.Price;
            dbCourse.ImageURL = data.ImageURL;
            dbCourse.Category = data.Category;
            dbCourse.StartDate = data.StartDate;
            dbCourse.EndDate = data.EndDate;
            dbCourse.InstructorId = data.InstructorId;
            await _context.SaveChangesAsync();
        }

        // Remove existing modules
        var existingModulesDb = _context.Courses_Modules.Where(n => n.CourseId == data.Id).ToList();
        _context.Courses_Modules.RemoveRange(existingModulesDb);
        await _context.SaveChangesAsync();

        // Add Course Modules
        foreach (var moduleId in data.ModuleIds)
        {
            var newCourseModule = new Course_Module()
            {
                CourseId = data.Id,
                ModuleId = moduleId
            };
            await _context.Courses_Modules.AddAsync(newCourseModule);
        }
        await _context.SaveChangesAsync();
    }
}