using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineLearningApp.Models;

public class CourseMaterial
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int ModuleId { get; set; }

    [ForeignKey(nameof(ModuleId))]
    public Module Module { get; set; } = default!;

    [StringLength(20)]
    public string MaterialType { get; set; } = "Link";

    [StringLength(1000)]
    public string? ResourceUrl { get; set; }

    [StringLength(255)]
    public string? OriginalFileName { get; set; }

    [StringLength(255)]
    public string? StoredFileName { get; set; }

    [StringLength(150)]
    public string? ContentType { get; set; }

    public long? FileSize { get; set; }

    [Required]
    /// <summary>Stored as UTC. Assigned by the application using TimeProvider.</summary>
    public DateTime CreatedAt { get; set; }

    public string? UploadedById { get; set; }

    [ForeignKey(nameof(UploadedById))]
    public Account? UploadedBy { get; set; }
}
