using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace OnlineLearningApp;

public class MaterialUploadViewModel
{
    public int ModuleId { get; set; }

    [Required]
    [StringLength(180)]
    [Display(Name = "Material title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Url]
    [Display(Name = "External resource URL")]
    public string? ResourceUrl { get; set; }

    [Display(Name = "Upload file")]
    public IFormFile? File { get; set; }
}
