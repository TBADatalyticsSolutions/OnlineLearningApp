using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;
using System.Security.Claims;

namespace OnlineLearningApp.Controllers;

public class QuizController : Controller
{
    private const int QuestionsPerAttempt = 5;
    private readonly OnlineLearningAppDbContext _context;

    public QuizController(OnlineLearningAppDbContext context)
    {
        _context = context;
    }

    private string QuestionSetKey(string studentId, int quizId)
        => $"QuizQuestionSet:{studentId}:{quizId}";

    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Instructor)]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var allQuizzes = await _context.Quizzes
            .AsNoTracking()
            .Include(q => q.Module)
            .OrderBy(q => q.QuizId)
            .ToListAsync();

        return View(allQuizzes);
    }

    [Authorize(Roles = UserRoles.Student)]
    [HttpGet]
    public async Task<IActionResult> History()
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Challenge();
        }

        var attempts = await _context.QuizAttempts
            .AsNoTracking()
            .Where(a => a.StudentId == studentId)
            .Include(a => a.Quiz)
                .ThenInclude(q => q.Module)
                    .ThenInclude(m => m.Course)
            .OrderByDescending(a => a.AttemptedAt)
            .Select(a => new QuizHistoryItemViewModel
            {
                QuizName = a.Quiz.QuizName,
                CourseName = a.Quiz.Module.Course.CourseName,
                Score = a.Score,
                TotalQuestions = a.TotalQuestions,
                Percentage = a.Percentage,
                Passed = a.Passed,
                AttemptedAt = a.AttemptedAt
            })
            .ToListAsync();

        return View(new QuizHistoryViewModel { Attempts = attempts });
    }

    [Authorize(Roles = UserRoles.Student)]
    [HttpGet]
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

        if (quiz is null)
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
        var questionSetKey = QuestionSetKey(studentId, id);
        HttpContext.Session.SetString(
            questionSetKey,
            string.Join(",", viewModel.SelectedQuestionIds));

        if (latestAttempt is not null)
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
    [Authorize(Roles = UserRoles.Student)]
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

        if (quiz is null)
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
        var storedQuestionSet = HttpContext.Session.GetString(
            QuestionSetKey(studentId, quiz.QuizId));

        var selectedIds = (storedQuestionSet ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => int.TryParse(value, out var questionId) ? questionId : 0)
            .Where(questionId => questionId > 0)
            .ToHashSet();

        var selectedQuestions = quiz.Questions
            .Where(q => selectedIds.Contains(q.QuestionId))
            .ToList();

        if (selectedQuestions.Count == 0)
        {
            TempData["Error"] = "Your assessment session has expired. Please start the assessment again.";
            return RedirectToAction(nameof(Take), new { id = quiz.QuizId });
        }

        HttpContext.Session.Remove(QuestionSetKey(studentId, quiz.QuizId));

        var score = 0;

        foreach (var question in selectedQuestions)
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

        var totalQuestions = selectedQuestions.Count;
        var percentage = totalQuestions == 0
            ? 0m
            : Math.Round(score * 100m / totalQuestions, 2);
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

        var result = BuildViewModel(quiz, selectedQuestions.Select(q => q.QuestionId));
        result.Answers = answers;
        result.SelectedQuestionIds = selectedQuestions.Select(q => q.QuestionId).ToList();
        result.Submitted = true;
        result.Score = score;
        result.TotalQuestions = totalQuestions;
        result.LastScore = score;
        result.LastTotalQuestions = totalQuestions;
        result.LastPercentage = percentage;
        result.AttemptedAt = attemptedAt;

        return View("Take", result);
    }

    private static QuizAttemptViewModel BuildViewModel(Quiz quiz, IEnumerable<int>? selectedQuestionIds = null)
    {
        var questions = quiz.Questions.AsEnumerable();

        if (selectedQuestionIds is null)
        {
            questions = questions
                .OrderBy(_ => Random.Shared.Next())
                .Take(Math.Min(QuestionsPerAttempt, quiz.Questions.Count));
        }
        else
        {
            var ids = selectedQuestionIds.ToHashSet();
            questions = questions
                .Where(q => ids.Contains(q.QuestionId))
                .OrderBy(_ => Random.Shared.Next());
        }

        var selectedQuestions = questions.ToList();

        return new QuizAttemptViewModel
        {
            QuizId = quiz.QuizId,
            QuizName = quiz.QuizName,
            Description = quiz.Description,
            CourseId = quiz.Module.CourseId,
            CourseName = quiz.Module.Course.CourseName,
            PassMark = quiz.PassMark,
            TotalQuestions = selectedQuestions.Count,
            SelectedQuestionIds = selectedQuestions.Select(q => q.QuestionId).ToList(),
            Questions = selectedQuestions
                .Select(q => new QuizQuestionViewModel
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    CorrectOptionId = q.Options.FirstOrDefault(o => o.IsCorrect)?.OptionId,
                    Options = q.Options
                        .OrderBy(o => Random.Shared.Next())
                        .Select(o => new QuizOptionViewModel
                        {
                            OptionId = o.OptionId,
                            OptionText = o.OptionText,
                            IsCorrect = o.IsCorrect
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
