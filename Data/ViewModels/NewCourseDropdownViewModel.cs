namespace OnlineLearningApp;

public class NewCourseDropdownViewModel
{
    public List<User> Instructors { get; set; } = new();
    public List<CourseCategory> Categories { get; set; } = new();
}

public class User
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
