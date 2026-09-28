using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;
using System.Security.Claims;

namespace OnlineLearningApp.Controllers;

[Authorize(Roles = UserRoles.Student)]
public class CertificateController : Controller
{
    private readonly OnlineLearningAppDbContext _context;

    public CertificateController(OnlineLearningAppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> View(int courseId)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId)) return Challenge();

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
