using System.ComponentModel.DataAnnotations;

namespace OnlineLearningApp.Models;

public class Certificate
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string CertificateNumber { get; set; } = default!;

    [Required]
    public string StudentId { get; set; } = default!;
    public Account Student { get; set; } = default!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;

    public DateTime IssuedAt { get; set; }
}
