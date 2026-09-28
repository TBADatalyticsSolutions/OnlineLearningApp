using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;
using System.Security.Claims;

namespace OnlineLearningApp.Controllers;

[Authorize(Roles = UserRoles.Student)]
public class QuizController : Controller
{
    private readonly OnlineLearningAppDbContext _context;

    public QuizController(OnlineLearningAppDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var allQuizzes = await _context.Quizzes
            .AsNoTracking()
            .Include(q => q.Module)
            .OrderBy(q => q.QuizId)
            .ToListAsync();

        return View(allQuizzes);
    }

    public async Task<IActionResult> Take(int id)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Challenge();
        }

        var quiz = await _context.Quizzes
            .AsNoTracking()
            .Include(q => q.Module)
                .ThenInclude(m => m.Course)
            .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.QuizId == id);

        if (quiz == null)
        {
            return NotFound();
        }

        var enrolled = await _context.StudentCourses
            .AnyAsync(sc => sc.StudentId == studentId && sc.CourseId == quiz.Module.CourseId);

        if (!enrolled)
        {
            TempData["Error"] = "Please enroll in this course before taking its quizzes.";
            return RedirectToAction("Details", "Course", new { id = quiz.Module.CourseId });
        }

        var latestAttempt = await _context.QuizAttempts
            .AsNoTracking()
            .Where(a => a.StudentId == studentId && a.QuizId == id)
            .OrderByDescending(a => a.AttemptedAt)
            .FirstOrDefaultAsync();

        var viewModel = BuildViewModel(quiz);
        if (latestAttempt != null)
        {
            viewModel.LastScore = latestAttempt.Score;
            viewModel.LastTotalQuestions = latestAttempt.TotalQuestions;
            viewModel.LastPercentage = latestAttempt.Percentage;
            viewModel.AttemptedAt = latestAttempt.AttemptedAt;
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(QuizAttemptViewModel model)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Challenge();
        }

        var quiz = await _context.Quizzes
            .AsNoTracking()
            .Include(q => q.Module)
                .ThenInclude(m => m.Course)
            .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.QuizId == model.QuizId);

        if (quiz == null)
        {
            return NotFound();
        }

        var enrolled = await _context.StudentCourses
            .AnyAsync(sc => sc.StudentId == studentId && sc.CourseId == quiz.Module.CourseId);

        if (!enrolled)
        {
            TempData["Error"] = "You must be enrolled in this course to submit the quiz.";
            return RedirectToAction("Details", "Course", new { id = quiz.Module.CourseId });
        }

        var answers = model.Answers ?? new Dictionary<int, int>();
        var score = 0;

        foreach (var question in quiz.Questions)
        {
            if (answers.TryGetValue(question.QuestionId, out var selectedOptionId))
            {
                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                if (correctOption?.OptionId == selectedOptionId)
                {
                    score++;
                }
            }
        }

        var totalQuestions = quiz.Questions.Count;
        var percentage = totalQuestions == 0 ? 0 : Math.Round(score * 100m / totalQuestions, 2);
        var passed = totalQuestions > 0 && percentage >= quiz.PassMark;
        var attemptedAt = DateTime.UtcNow;

        _context.QuizAttempts.Add(new QuizAttempt
        {
            StudentId = studentId,
            QuizId = quiz.QuizId,
            Score = score,
            TotalQuestions = totalQuestions,
            Percentage = percentage,
            Passed = passed,
            AttemptedAt = attemptedAt
        });

        await _context.SaveChangesAsync();

        var result = BuildViewModel(quiz);
        result.Answers = answers;
        result.Score = score;
        result.TotalQuestions = totalQuestions;
        result.Submitted = true;
        result.LastScore = score;
        result.LastTotalQuestions = totalQuestions;
        result.LastPercentage = percentage;
        result.AttemptedAt = attemptedAt;

        return View("Take", result);
    }

    private static QuizAttemptViewModel BuildViewModel(Quiz quiz)
    {
        return new QuizAttemptViewModel
        {
            QuizId = quiz.QuizId,
            QuizName = quiz.QuizName,
            Description = quiz.Description,
            CourseId = quiz.Module.CourseId,
            CourseName = quiz.Module.Course.CourseName,
            PassMark = quiz.PassMark,
            TotalQuestions = quiz.Questions.Count,
            Questions = quiz.Questions
                .OrderBy(q => q.QuestionId)
                .Select(q => new QuizQuestionViewModel
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    Options = q.Options
                        .OrderBy(o => o.OptionId)
                        .Select(o => new QuizOptionViewModel
                        {
                            OptionId = o.OptionId,
                            OptionText = o.OptionText
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
