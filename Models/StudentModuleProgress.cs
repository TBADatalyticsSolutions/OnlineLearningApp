using System.ComponentModel.DataAnnotations;

namespace OnlineLearningApp.Models;

public class StudentModuleProgress
{
    [Key]
    public int Id { get; set; }

    public string StudentId { get; set; } = default!;
    public Account Student { get; set; } = default!;

    public int ModuleId { get; set; }
    public Module Module { get; set; } = default!;

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
