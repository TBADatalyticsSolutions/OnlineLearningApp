using OnlineLearningApp.Models;

namespace OnlineLearningApp;

public interface ICourseService : IEntityBaseRepository<Course>
{
    Task<Course?> GetCourseByIdAsync(int id);
    Task<NewCourseDropdownViewModel> GetNewCourseDropdownsValues();
    Task AddNewCourseAsync(NewCourseViewModel data);
    Task UpdateCourseAsync(NewCourseViewModel data);
}
