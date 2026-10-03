using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Data.Services;
using OnlineLearningApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Security.Claims;

namespace OnlineLearningApp.Controllers;

[Authorize(Roles = UserRoles.Student)]
public class CertificateController : Controller
{
    private readonly OnlineLearningAppDbContext _context;
    private readonly ICourseCompletionService _completionService;
    public CertificateController(OnlineLearningAppDbContext context, ICourseCompletionService completionService) { _context = context; _completionService = completionService; }

    private async Task<Certificate?> GetCertificate(int courseId, string studentId)
    {
        var completion = await _completionService.EvaluateAsync(studentId, courseId, issueCertificate: true);
        if (!completion.IsCompleted) return null;
        return await _context.Certificates.AsNoTracking().Include(c => c.Student).Include(c => c.Course).FirstOrDefaultAsync(c => c.StudentId == studentId && c.CourseId == courseId);
    }

    public async Task<IActionResult> View(int courseId)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId)) return Challenge();
        var certificate = await GetCertificate(courseId, studentId);
        if (certificate == null) { TempData["Error"] = "Complete all required learning, assessments, payment and capstone rules before certification."; return RedirectToAction("Learn", "Course", new { id = courseId }); }
        return View(certificate);
    }

    [HttpGet]
    public async Task<IActionResult> Pdf(int courseId)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId)) return Challenge();
        var certificate = await GetCertificate(courseId, studentId);
        if (certificate == null) return Forbid();

        var pdf = Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(45);
            page.DefaultTextStyle(x => x.FontFamily("Arial"));
            page.Header().AlignCenter().Text("TBA DATALYTICS SOLUTIONS").Bold().FontSize(24).FontColor(Colors.Blue.Darken2);
            page.Content().AlignCenter().Column(column =>
            {
                column.Spacing(18);
                column.Item().Text("CERTIFICATE OF COMPLETION").Bold().FontSize(30);
                column.Item().Text("This certificate is proudly presented to").FontSize(14);
                column.Item().Text(certificate.Student.FullName).Bold().FontSize(28);
                column.Item().Text("for successfully completing").FontSize(14);
                column.Item().Text(certificate.Course.CourseName).Bold().FontSize(22);
                column.Item().Text($"Issued on {certificate.IssuedAt:dd MMMM yyyy}  •  Certificate No. {certificate.CertificateNumber}").FontSize(11).FontColor(Colors.Grey.Darken1);
            });
            page.Footer().AlignCenter().Text("Verify this certificate through the TBA Online Learning certificate verification service.").FontSize(9);
        })).GeneratePdf();
        return File(pdf, "application/pdf", $"certificate-{certificate.CertificateNumber}.pdf");
    }
}
