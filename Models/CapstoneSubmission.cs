using System.ComponentModel.DataAnnotations;

namespace OnlineLearningApp.Models;

public enum CapstoneSubmissionStatus
{
    Draft,
    Submitted,
    Approved,
    NeedsRevision,
    Rejected
}

public class CapstoneSubmission
{
    public int Id { get; set; }
    [Required]
    public string StudentId { get; set; } = default!;
    public Account Student { get; set; } = default!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;
    [Required, MaxLength(200)]
    public string ProjectTitle { get; set; } = default!;
    [Required]
    public string SubmissionUrl { get; set; } = default!;
    public string? Summary { get; set; }
    public CapstoneSubmissionStatus Status { get; set; } = CapstoneSubmissionStatus.Submitted;
    public string? ReviewerFeedback { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}
