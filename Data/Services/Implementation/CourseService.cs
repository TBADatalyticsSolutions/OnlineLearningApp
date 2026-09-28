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
        var newCourse = new Course
        {
            CourseName = data.CourseName.Trim(),
            Description = data.Description.Trim(),
            Price = data.Price,
            ImageURL = data.ImageURL.Trim(),
            Category = data.Category,
            StartDate = data.StartDate,
            EndDate = data.EndDate,
            Status = data.Status,
            InstructorId = data.InstructorId,
            RequireAllQuizzesPassed = data.RequireAllQuizzesPassed
        };

        await _context.Courses.AddAsync(newCourse);
        await _context.SaveChangesAsync();

        // CourseId is a legacy duplicate key retained for compatibility with older code.
        newCourse.CourseId = newCourse.Id;

        var moduleIds = data.ModuleIds.Distinct().ToList();
        if (moduleIds.Count > 0)
        {
            var validModuleIds = await _context.Modules
                .Where(m => moduleIds.Contains(m.ModuleId))
                .Select(m => m.ModuleId)
                .ToListAsync();

            foreach (var moduleId in validModuleIds)
            {
                _context.Courses_Modules.Add(new Course_Module
                {
                    CourseId = newCourse.Id,
                    ModuleId = moduleId
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<Course?> GetCourseByIdAsync(int id)
    {
        return await _context.Courses
            .AsNoTracking()
            .Include(c => c.Instructor)
            .Include(c => c.Courses_Modules)
                .ThenInclude(cm => cm.Module)
            .FirstOrDefaultAsync(c => c.Id == id);
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

        var categories = Enum.GetValues<CourseCategory>()
            .OrderBy(c => c)
            .ToList();

        return new NewCourseDropdownViewModel
        {
            Instructors = instructors,
            Categories = categories
        };
    }

    public async Task UpdateCourseAsync(NewCourseViewModel data)
    {
        var dbCourse = await _context.Courses.FirstOrDefaultAsync(c => c.Id == data.Id);
        if (dbCourse is null)
        {
            throw new KeyNotFoundException($"Course with id {data.Id} was not found.");
        }

        dbCourse.CourseName = data.CourseName.Trim();
        dbCourse.Description = data.Description.Trim();
        dbCourse.Price = data.Price;
        dbCourse.ImageURL = data.ImageURL.Trim();
        dbCourse.Category = data.Category;
        dbCourse.StartDate = data.StartDate;
        dbCourse.EndDate = data.EndDate;
        dbCourse.Status = data.Status;
        dbCourse.InstructorId = data.InstructorId;
        dbCourse.RequireAllQuizzesPassed = data.RequireAllQuizzesPassed;
        dbCourse.CourseId = dbCourse.Id;

        var existingModules = await _context.Courses_Modules
            .Where(cm => cm.CourseId == data.Id)
            .ToListAsync();

        _context.Courses_Modules.RemoveRange(existingModules);

        var moduleIds = data.ModuleIds.Distinct().ToList();
        if (moduleIds.Count > 0)
        {
            var validModuleIds = await _context.Modules
                .Where(m => moduleIds.Contains(m.ModuleId))
                .Select(m => m.ModuleId)
                .ToListAsync();

            foreach (var moduleId in validModuleIds)
            {
                _context.Courses_Modules.Add(new Course_Module
                {
                    CourseId = data.Id,
                    ModuleId = moduleId
                });
            }
        }

        await _context.SaveChangesAsync();
    }
}
