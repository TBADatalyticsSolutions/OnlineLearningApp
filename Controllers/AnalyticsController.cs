using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;
using System.Security.Claims;

namespace OnlineLearningApp.Controllers;

[Authorize]
public class AnalyticsController : Controller
{
    private readonly OnlineLearningAppDbContext _context;
    public AnalyticsController(OnlineLearningAppDbContext context) => _context = context;

    [Authorize(Roles = UserRoles.Student)]
    [HttpGet]
    public async Task<IActionResult> Assessments()
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier); if (string.IsNullOrWhiteSpace(studentId)) return Challenge();
        var attempts=await _context.QuizAttempts.AsNoTracking().Where(a=>a.StudentId==studentId).Include(a=>a.Quiz).ThenInclude(q=>q.Module).ThenInclude(m=>m.Course).ToListAsync();
        var rows=attempts.GroupBy(a=>new{a.QuizId,CourseName=a.Quiz.Module.Course.CourseName,QuizName=a.Quiz.QuizName}).Select(g=>{var latest=g.OrderByDescending(a=>a.AttemptedAt).First();return new AssessmentAnalyticsRow{CourseName=g.Key.CourseName,QuizName=g.Key.QuizName,Attempts=g.Count(),BestPercentage=g.Max(a=>a.Percentage),LatestPercentage=latest.Percentage,Passed=g.Any(a=>a.Passed),LastAttemptedAt=latest.AttemptedAt};}).OrderBy(r=>r.CourseName).ThenBy(r=>r.QuizName).ToList();
        return View(new StudentAssessmentAnalyticsViewModel{TotalAttempts=attempts.Count,PassedAttempts=attempts.Where(a=>a.Passed).Select(a=>a.QuizId).Distinct().Count(),AveragePercentage=attempts.Count==0?0:Math.Round(attempts.Average(a=>a.Percentage),2),BestPercentage=attempts.Count==0?0:attempts.Max(a=>a.Percentage),CoursesAssessed=attempts.Select(a=>a.Quiz.Module.CourseId).Distinct().Count(),Assessments=rows});
    }

    [Authorize(Roles = UserRoles.Student)]
    [HttpGet]
    public async Task<IActionResult> CourseMastery()
    {
        var studentId=User.FindFirstValue(ClaimTypes.NameIdentifier); if(string.IsNullOrWhiteSpace(studentId)) return Challenge();
        var enrollments=await _context.StudentCourses.AsNoTracking().Where(x=>x.StudentId==studentId).Include(x=>x.Course).ThenInclude(c=>c.Modules).ThenInclude(m=>m.Quizzes).ToListAsync();
        var courseIds=enrollments.Select(x=>x.CourseId).ToList();
        var progress=await _context.StudentModuleProgress.AsNoTracking().Where(p=>p.StudentId==studentId).ToListAsync();
        var attempts=await _context.QuizAttempts.AsNoTracking().Include(a=>a.Quiz).ThenInclude(q=>q.Module).Where(a=>courseIds.Contains(a.Quiz.Module.CourseId)&&a.StudentId==studentId).ToListAsync();
        var capstones=await _context.CapstoneSubmissions.AsNoTracking().Where(s=>s.StudentId==studentId&&courseIds.Contains(s.CourseId)).ToListAsync();
        var orders=await _context.Orders.AsNoTracking().Include(o=>o.OrderItems).Where(o=>o.AccountId==studentId).ToListAsync();
        var paidCourses=orders.Where(o=>o.PaymentStatus.ToString().Equals("Paid",StringComparison.OrdinalIgnoreCase)).SelectMany(o=>o.OrderItems).Select(i=>i.CourseId).ToHashSet();
        var result=enrollments.Select(e=>{var modules=e.Course.Modules.ToList();var moduleIds=modules.Select(m=>m.ModuleId).ToHashSet();var completed=progress.Count(p=>moduleIds.Contains(p.ModuleId)&&p.CompletedAt.HasValue);var quizzes=modules.SelectMany(m=>m.Quizzes).ToList();var quizIds=quizzes.Select(q=>q.QuizId).ToHashSet();var ca=attempts.Where(a=>quizIds.Contains(a.QuizId)).ToList();var passed=quizzes.Count(q=>ca.Any(a=>a.QuizId==q.QuizId&&a.Passed));var mastery=quizzes.Count==0?0:Math.Round(passed*100m/quizzes.Count,2);var completion=modules.Count==0?0:Math.Round(completed*100m/modules.Count,2);return new CourseMasteryViewModel{CourseId=e.CourseId,CourseName=e.Course.CourseName,CompletionPercentage=completion,AssessmentMastery=mastery,OverallMastery=Math.Round((completion+mastery)/2m,2),CompletedModules=completed,TotalModules=modules.Count,PassedAssessments=passed,TotalAssessments=quizzes.Count,CapstoneApproved=capstones.Any(s=>s.CourseId==e.CourseId&&s.Status==CapstoneSubmissionStatus.Approved),Paid=paidCourses.Contains(e.CourseId)};}).ToList();
        return View(result);
    }

    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Instructor)]
    [HttpGet]
    public async Task<IActionResult> InstructorPerformance()
    {
        var userId=User.FindFirstValue(ClaimTypes.NameIdentifier);if(string.IsNullOrWhiteSpace(userId))return Challenge();var query=_context.Courses.AsNoTracking();if(User.IsInRole(UserRoles.Instructor))query=query.Where(c=>c.InstructorId==userId);var courses=await query.AsSplitQuery().Include(c=>c.StudentCourses).Include(c=>c.Modules).ThenInclude(m=>m.Quizzes).ToListAsync();var ids=courses.Select(c=>c.Id).ToList();var attempts=await _context.QuizAttempts.AsNoTracking().Include(a=>a.Quiz).ThenInclude(q=>q.Module).Where(a=>ids.Contains(a.Quiz.Module.CourseId)).ToListAsync();var rows=courses.Select(c=>{var ca=attempts.Where(a=>a.Quiz.Module.CourseId==c.Id).ToList();var learners=c.StudentCourses.Select(x=>x.StudentId).ToHashSet();var completed=c.StudentCourses.Count(x=>x.CompletedAt!=null);return new InstructorCoursePerformance{CourseId=c.Id,CourseName=c.CourseName,Learners=learners.Count,CompletedLearners=completed,QuizAttempts=ca.Count,AverageScore=ca.Count==0?0:Math.Round(ca.Average(a=>a.Percentage),2),PassRate=ca.Count==0?0:Math.Round(ca.Count(a=>a.Passed)*100m/ca.Count,2),CompletionRate=learners.Count==0?0:Math.Round(completed*100m/learners.Count,2)};}).ToList();return View(new InstructorPerformanceViewModel{CourseCount=courses.Count,ActiveLearners=courses.SelectMany(c=>c.StudentCourses).Select(x=>x.StudentId).Distinct().Count(),CompletedLearners=courses.SelectMany(c=>c.StudentCourses).Count(x=>x.CompletedAt!=null),TotalAttempts=attempts.Count,AverageAssessmentScore=attempts.Count==0?0:Math.Round(attempts.Average(a=>a.Percentage),2),PassRate=attempts.Count==0?0:Math.Round(attempts.Count(a=>a.Passed)*100m/attempts.Count,2),Courses=rows});
    }
}
