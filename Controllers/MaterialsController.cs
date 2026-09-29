using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;
using System.Security.Claims;

namespace OnlineLearningApp.Controllers;

[Authorize]
public class MaterialsController : Controller
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".csv",
        ".txt", ".zip", ".mp4", ".webm", ".png", ".jpg", ".jpeg"
    };

    private const long MaxFileSize = 25 * 1024 * 1024;

    private readonly OnlineLearningAppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public MaterialsController(OnlineLearningAppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Index(int? moduleId = null)
    {
        var query = _context.CourseMaterials
            .AsNoTracking()
            .Include(m => m.Module)
                .ThenInclude(m => m.Course)
            .OrderByDescending(m => m.CreatedAt)
            .AsQueryable();

        if (moduleId.HasValue)
        {
            query = query.Where(m => m.ModuleId == moduleId.Value)
                .OrderByDescending(m => m.CreatedAt);
        }

        ViewBag.SelectedModuleId = moduleId;
        return View(await query.ToListAsync());
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet]
    public async Task<IActionResult> Create(int moduleId)
    {
        var module = await _context.Modules
            .AsNoTracking()
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.ModuleId == moduleId);

        if (module is null)
        {
            return NotFound();
        }

        return View(new MaterialUploadViewModel
        {
            ModuleId = module.ModuleId
        });
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
    public async Task<IActionResult> Create(MaterialUploadViewModel model)
    {
        var module = await _context.Modules
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.ModuleId == model.ModuleId);

        if (module is null)
        {
            return NotFound();
        }

        if (model.File is null && string.IsNullOrWhiteSpace(model.ResourceUrl))
        {
            ModelState.AddModelError(string.Empty, "Provide an external resource URL or upload a file.");
        }

        if (model.File is not null && !string.IsNullOrWhiteSpace(model.ResourceUrl))
        {
            ModelState.AddModelError(string.Empty, "Choose either an external resource URL or a file, not both.");
        }

        if (model.File is not null)
        {
            var extension = Path.GetExtension(model.File.FileName);
            if (!AllowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(model.File), "This file type is not allowed.");
            }

            if (model.File.Length <= 0 || model.File.Length > MaxFileSize)
            {
                ModelState.AddModelError(nameof(model.File), "Files must be between 1 byte and 25 MB.");
            }
        }

        if (!string.IsNullOrWhiteSpace(model.ResourceUrl))
        {
            if (!Uri.TryCreate(model.ResourceUrl.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                ModelState.AddModelError(nameof(model.ResourceUrl), "Enter a valid HTTP or HTTPS URL.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var uploaderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(uploaderId))
        {
            return Challenge();
        }

        var material = new CourseMaterial
        {
            ModuleId = module.ModuleId,
            Title = model.Title.Trim(),
            Description = model.Description?.Trim() ?? string.Empty,
            ResourceUrl = string.IsNullOrWhiteSpace(model.ResourceUrl) ? null : model.ResourceUrl.Trim(),
            MaterialType = model.File is not null ? "File" : "Link",
            UploadedById = uploaderId,
            CreatedAt = DateTime.UtcNow
        };

        if (model.File is not null)
        {
            var uploadRoot = Path.Combine(_environment.ContentRootPath, "App_Data", "materials");
            Directory.CreateDirectory(uploadRoot);

            var extension = Path.GetExtension(model.File.FileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadRoot, storedFileName);

            await using var stream = System.IO.File.Create(filePath);
            await model.File.CopyToAsync(stream);

            material.StoredFileName = storedFileName;
            material.OriginalFileName = Path.GetFileName(model.File.FileName);
            material.ContentType = string.IsNullOrWhiteSpace(model.File.ContentType)
                ? "application/octet-stream"
                : model.File.ContentType;
            material.FileSize = model.File.Length;
        }

        _context.CourseMaterials.Add(material);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Material '{material.Title}' was added to {module.ModuleName}.";
        return RedirectToAction(nameof(Index), new { moduleId = module.ModuleId });
    }

    [Authorize(Roles = UserRoles.Student + "," + UserRoles.Instructor + "," + UserRoles.Admin)]
    public async Task<IActionResult> Open(int id)
    {
        var material = await _context.CourseMaterials
            .AsNoTracking()
            .Include(m => m.Module)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (material is null)
        {
            return NotFound();
        }

        if (User.IsInRole(UserRoles.Student))
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var enrolled = !string.IsNullOrWhiteSpace(studentId) &&
                await _context.StudentCourses.AnyAsync(
                    sc => sc.StudentId == studentId && sc.CourseId == material.Module.CourseId);

            if (!enrolled)
            {
                return Forbid();
            }
        }

        if (!string.IsNullOrWhiteSpace(material.StoredFileName))
        {
            var uploadRoot = Path.Combine(_environment.ContentRootPath, "App_Data", "materials");
            var safeFileName = Path.GetFileName(material.StoredFileName);
            var filePath = Path.Combine(uploadRoot, safeFileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            return PhysicalFile(
                filePath,
                material.ContentType ?? "application/octet-stream",
                material.OriginalFileName ?? safeFileName,
                enableRangeProcessing: true);
        }

        if (!string.IsNullOrWhiteSpace(material.ResourceUrl))
        {
            return Redirect(material.ResourceUrl);
        }

        return NotFound();
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var material = await _context.CourseMaterials.FirstOrDefaultAsync(m => m.Id == id);
        if (material is null)
        {
            return NotFound();
        }

        var moduleId = material.ModuleId;

        if (!string.IsNullOrWhiteSpace(material.StoredFileName))
        {
            var uploadRoot = Path.Combine(_environment.ContentRootPath, "App_Data", "materials");
            var filePath = Path.Combine(uploadRoot, Path.GetFileName(material.StoredFileName));

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        _context.CourseMaterials.Remove(material);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Material deleted.";
        return RedirectToAction(nameof(Index), new { moduleId });
    }
}
