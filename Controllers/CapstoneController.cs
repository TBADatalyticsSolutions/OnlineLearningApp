using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;
using System.Security.Claims;

namespace OnlineLearningApp.Controllers;

[Authorize]
public class CapstoneController : Controller
{
    private readonly OnlineLearningAppDbContext _context;
    public CapstoneController(OnlineLearningAppDbContext context) => _context = context;

    [Authorize(Roles = UserRoles.Student)]
    [HttpGet]
    public async Task<IActionResult> Submit(int courseId)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is null) return NotFound();
        if (!await _context.StudentCourses.AnyAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId)) return Forbid();
        var existing = await _context.CapstoneSubmissions.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == studentId && s.CourseId == courseId);
        ViewBag.Course = course;
        return View(existing ?? new CapstoneSubmission { CourseId = courseId, ProjectTitle = course.CapstoneTitle });
    }

    [Authorize(Roles = UserRoles.Student)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(CapstoneSubmission model)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == model.CourseId);
        if (course is null) return NotFound();
        if (!await _context.StudentCourses.AnyAsync(sc => sc.StudentId == studentId && sc.CourseId == model.CourseId)) return Forbid();
        if (string.IsNullOrWhiteSpace(model.ProjectTitle) || string.IsNullOrWhiteSpace(model.SubmissionUrl))
        {
            ModelState.AddModelError(string.Empty, "Project title and a submission URL are required.");
            ViewBag.Course = course;
            return View(model);
        }
        var existing = await _context.CapstoneSubmissions.FirstOrDefaultAsync(s => s.StudentId == studentId && s.CourseId == model.CourseId);
        if (existing is null)
        {
            model.StudentId = studentId!;
            model.Status = CapstoneSubmissionStatus.Submitted;
            model.SubmittedAt = DateTime.UtcNow;
            _context.CapstoneSubmissions.Add(model);
        }
        else
        {
            existing.ProjectTitle = model.ProjectTitle;
            existing.SubmissionUrl = model.SubmissionUrl;
            existing.Summary = model.Summary;
            existing.Status = CapstoneSubmissionStatus.Submitted;
            existing.SubmittedAt = DateTime.UtcNow;
            existing.ReviewerFeedback = null;
            existing.ReviewedAt = null;
        }
        await _context.SaveChangesAsync();
        TempData["Success"] = "Capstone submitted. Your instructor will review it before certification is unlocked.";
        return RedirectToAction("Learn", "Course", new { id = model.CourseId });
    }

    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Instructor)]
    [HttpGet]
    public async Task<IActionResult> Review()
    {
        var submissions = await _context.CapstoneSubmissions.AsNoTracking().Include(s => s.Student).Include(s => s.Course).OrderByDescending(s => s.SubmittedAt).ToListAsync();
        return View(submissions);
    }

    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Instructor)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, CapstoneSubmissionStatus status, string? reviewerFeedback)
    {
        var submission = await _context.CapstoneSubmissions.FindAsync(id);
        if (submission is null) return NotFound();
        submission.Status = status;
        submission.ReviewerFeedback = reviewerFeedback;
        submission.ReviewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Capstone review saved.";
        return RedirectToAction(nameof(Review));
    }
}
