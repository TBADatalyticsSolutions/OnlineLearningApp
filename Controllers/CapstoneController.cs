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
    private readonly TimeProvider _clock;
    public CapstoneController(OnlineLearningAppDbContext context, TimeProvider clock){_context=context;_clock=clock;}

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
        if (string.IsNullOrWhiteSpace(model.ProjectTitle) || !Uri.TryCreate(model.SubmissionUrl, UriKind.Absolute, out _))
        {
            ModelState.AddModelError(string.Empty, "Project title and a valid submission URL are required.");
            ViewBag.Course = course;
            return View(model);
        }
        var existing = await _context.CapstoneSubmissions.FirstOrDefaultAsync(s => s.StudentId == studentId && s.CourseId == model.CourseId);
        var now = _clock.GetUtcNow().UtcDateTime;
        if (existing is null)
        {
            model.StudentId = studentId!; model.Status = CapstoneSubmissionStatus.Submitted; model.SubmittedAt = now;
            _context.CapstoneSubmissions.Add(model);
        }
        else
        {
            existing.ProjectTitle = model.ProjectTitle; existing.SubmissionUrl = model.SubmissionUrl; existing.Summary = model.Summary;
            existing.Status = CapstoneSubmissionStatus.Submitted; existing.SubmittedAt = now; existing.ReviewerFeedback = null; existing.ReviewedAt = null;
        }
        await _context.SaveChangesAsync();
        TempData["Success"] = "Capstone submitted for review.";
        return RedirectToAction("Learn", "Course", new { id = model.CourseId });
    }

    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Instructor)]
    [HttpGet]
    public async Task<IActionResult> Review()
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = _context.CapstoneSubmissions.AsNoTracking().Include(s => s.Student).Include(s => s.Course).AsQueryable();
        if (User.IsInRole(UserRoles.Instructor)) query = query.Where(s => s.Course.InstructorId == reviewerId);
        return View(await query.OrderByDescending(s => s.SubmittedAt).ToListAsync());
    }

    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Instructor)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, CapstoneSubmission model)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var submission = await _context.CapstoneSubmissions.Include(s => s.Course).FirstOrDefaultAsync(s => s.Id == id);
        if (submission is null) return NotFound();
        if (User.IsInRole(UserRoles.Instructor) && submission.Course.InstructorId != reviewerId) return Forbid();
        submission.TechnicalScore = model.TechnicalScore;
        submission.ProblemSolvingScore = model.ProblemSolvingScore;
        submission.CommunicationScore = model.CommunicationScore;
        submission.ProfessionalismScore = model.ProfessionalismScore;
        submission.CalculateOverallScore();
        submission.Status = model.Status;
        submission.ReviewerFeedback = model.ReviewerFeedback;
        submission.ReviewerId = reviewerId;
        submission.ReviewedAt = _clock.GetUtcNow().UtcDateTime;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Capstone rubric and review saved.";
        return RedirectToAction(nameof(Review));
    }
}
