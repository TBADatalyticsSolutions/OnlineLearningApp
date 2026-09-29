using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Data.Services;
using OnlineLearningApp.Models;
using System.Security.Claims;

namespace OnlineLearningApp.Controllers;

[Authorize(Roles = UserRoles.Student)]
public class CertificateController : Controller
{
    private readonly OnlineLearningAppDbContext _context;
    private readonly ICourseCompletionService _completionService;

    public CertificateController(OnlineLearningAppDbContext context, ICourseCompletionService completionService)
    {
        _context = context;
        _completionService = completionService;
    }

    public async Task<IActionResult> View(int courseId)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId)) return Challenge();

        var completion = await _completionService.EvaluateAsync(studentId, courseId, issueCertificate: false);
        if (!completion.IsCompleted)
        {
            TempData["Error"] = "The certificate is available only after all required course completion rules are satisfied.";
            return RedirectToAction("Learn", "Course", new { id = courseId });
        }

        var certificate = await _context.Certificates
            .AsNoTracking()
            .Include(c => c.Student)
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.StudentId == studentId && c.CourseId == courseId);

        if (certificate == null)
        {
            TempData["Error"] = "Complete every module in the course to unlock your certificate.";
            return RedirectToAction("Learn", "Course", new { id = courseId });
        }

        return View(certificate);
    }
}
