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
    private const string QuestionSetPrefix = "QuizQuestionSet:";
    private const string StartTimePrefix = "QuizStartTime:";
    private readonly OnlineLearningAppDbContext _context;

    public QuizController(OnlineLearningAppDbContext context)
    {
        _context = context;
    }

    private static string QuestionSetKey(string studentId, int quizId)
        => $"{QuestionSetPrefix}{studentId}:{quizId}";

    private static string StartTimeKey(string studentId, int quizId)
        => $"{StartTimePrefix}{studentId}:{quizId}";

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
        if (string.IsNullOrWhiteSpace(studentId)) return Challenge();

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
        if (string.IsNullOrWhiteSpace(studentId)) return Challenge();

        var quiz = await LoadQuizAsync(id);
        if (quiz is null) return NotFound();

        if (!await IsEnrolledAsync(studentId, quiz.Module.CourseId))
        {
            TempData["Error"] = "Please enroll in this course before taking its assessments.";
            return RedirectToAction("Details", "Course", new { id = quiz.Module.CourseId });
        }

        var previousAttempts = await _context.QuizAttempts
            .AsNoTracking()
            .CountAsync(a => a.StudentId == studentId && a.QuizId == id);

        if (quiz.AttemptLimit.HasValue && previousAttempts >= quiz.AttemptLimit.Value)
        {
            TempData["Error"] = $"You have reached the {quiz.AttemptLimit.Value}-attempt limit for this assessment.";
            return RedirectToAction("Learn", "Course", new { id = quiz.Module.CourseId });
        }

        var viewModel = BuildViewModel(quiz);
        HttpContext.Session.SetString(
            QuestionSetKey(studentId, id),
            string.Join(",", viewModel.SelectedQuestionIds));
        HttpContext.Session.SetString(
            StartTimeKey(studentId, id),
            DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

        await PopulateLatestAttemptAsync(viewModel, studentId, id);
        viewModel.AttemptNumber = previousAttempts + 1;
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Student)]
    public async Task<IActionResult> Submit(QuizAttemptViewModel model)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId)) return Challenge();

        var quiz = await LoadQuizAsync(model.QuizId);
        if (quiz is null) return NotFound();

        if (!await IsEnrolledAsync(studentId, quiz.Module.CourseId))
        {
            TempData["Error"] = "You must be enrolled in this course to submit the assessment.";
            return RedirectToAction("Details", "Course", new { id = quiz.Module.CourseId });
        }

        var previousAttempts = await _context.QuizAttempts
            .AsNoTracking()
            .CountAsync(a => a.StudentId == studentId && a.QuizId == quiz.QuizId);

        if (quiz.AttemptLimit.HasValue && previousAttempts >= quiz.AttemptLimit.Value)
        {
            TempData["Error"] = $"You have reached the {quiz.AttemptLimit.Value}-attempt limit for this assessment.";
            return RedirectToAction("Learn", "Course", new { id = quiz.Module.CourseId });
        }

        // The server-side session is authoritative. Posted SelectedQuestionIds are never trusted for scoring.
        var storedQuestionSet = HttpContext.Session.GetString(QuestionSetKey(studentId, quiz.QuizId));
        var selectedIds = ParseIds(storedQuestionSet);
        var selectedQuestions = quiz.Questions
            .Where(q => selectedIds.Contains(q.QuestionId))
            .ToList();

        if (selectedQuestions.Count == 0)
        {
            ClearAssessmentSession(studentId, quiz.QuizId);
            TempData["Error"] = "Your assessment session has expired. Please start the assessment again.";
            return RedirectToAction(nameof(Take), new { id = quiz.QuizId });
        }

        var answers = model.Answers ?? new Dictionary<int, int>();
        var score = selectedQuestions.Count(question =>
            answers.TryGetValue(question.QuestionId, out var selectedOptionId) &&
            question.Options.Any(option => option.OptionId == selectedOptionId && option.IsCorrect));

        var totalQuestions = selectedQuestions.Count;
        var percentage = totalQuestions == 0 ? 0m : Math.Round(score * 100m / totalQuestions, 2);
        var passed = totalQuestions > 0 && percentage >= quiz.PassMark;
        var attemptedAt = DateTime.UtcNow;
        var timeExpired = HasTimeExpired(studentId, quiz);

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
        ClearAssessmentSession(studentId, quiz.QuizId);

        var result = BuildViewModel(quiz, selectedQuestions.Select(q => q.QuestionId), revealAnswers: true);
        result.Answers = answers;
        result.SelectedQuestionIds = selectedQuestions.Select(q => q.QuestionId).ToList();
        result.Submitted = true;
        result.Score = score;
        result.TotalQuestions = totalQuestions;
        result.AttemptNumber = previousAttempts + 1;
        result.LastScore = score;
        result.LastTotalQuestions = totalQuestions;
        result.LastPercentage = percentage;
        result.AttemptedAt = attemptedAt;
        result.TimeExpired = timeExpired;

        return View("Take", result);
    }

    private async Task<Quiz?> LoadQuizAsync(int quizId)
    {
        return await _context.Quizzes
            .AsNoTracking()
            .Include(q => q.Module)
                .ThenInclude(m => m.Course)
            .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.QuizId == quizId);
    }

    private Task<bool> IsEnrolledAsync(string studentId, int courseId)
        => _context.StudentCourses.AnyAsync(sc =>
            sc.StudentId == studentId && sc.CourseId == courseId);

    private async Task PopulateLatestAttemptAsync(QuizAttemptViewModel model, string studentId, int quizId)
    {
        var latestAttempt = await _context.QuizAttempts
            .AsNoTracking()
            .Where(a => a.StudentId == studentId && a.QuizId == quizId)
            .OrderByDescending(a => a.AttemptedAt)
            .FirstOrDefaultAsync();

        if (latestAttempt is null) return;

        model.LastScore = latestAttempt.Score;
        model.LastTotalQuestions = latestAttempt.TotalQuestions;
        model.LastPercentage = latestAttempt.Percentage;
        model.AttemptedAt = latestAttempt.AttemptedAt;
    }

    private bool HasTimeExpired(string studentId, Quiz quiz)
    {
        if (!quiz.TimeLimitMinutes.HasValue) return false;

        var rawStart = HttpContext.Session.GetString(StartTimeKey(studentId, quiz.QuizId));
        if (!long.TryParse(rawStart, out var startSeconds)) return true;

        var elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - startSeconds;
        return elapsed > TimeSpan.FromMinutes(quiz.TimeLimitMinutes.Value).TotalSeconds;
    }

    private void ClearAssessmentSession(string studentId, int quizId)
    {
        HttpContext.Session.Remove(QuestionSetKey(studentId, quizId));
        HttpContext.Session.Remove(StartTimeKey(studentId, quizId));
    }

    private static HashSet<int> ParseIds(string? value)
        => (value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => int.TryParse(id, out var parsed) ? parsed : 0)
            .Where(id => id > 0)
            .ToHashSet();

    private static QuizAttemptViewModel BuildViewModel(
        Quiz quiz,
        IEnumerable<int>? selectedQuestionIds = null,
        bool revealAnswers = false)
    {
        IEnumerable<Question> questions = quiz.Questions;

        if (selectedQuestionIds is null)
        {
            questions = quiz.ShuffleQuestions
                ? questions.OrderBy(_ => Random.Shared.Next())
                : questions.OrderBy(q => q.QuestionId);
            questions = questions.Take(Math.Min(QuestionsPerAttempt, quiz.Questions.Count));
        }
        else
        {
            var ids = selectedQuestionIds.ToHashSet();
            questions = questions
                .Where(q => ids.Contains(q.QuestionId))
                .OrderBy(q => quiz.ShuffleQuestions ? Random.Shared.Next() : q.QuestionId);
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
            TimeLimitMinutes = quiz.TimeLimitMinutes,
            AttemptLimit = quiz.AttemptLimit,
            IsPractice = quiz.IsPractice,
            TotalQuestions = selectedQuestions.Count,
            SelectedQuestionIds = selectedQuestions.Select(q => q.QuestionId).ToList(),
            Questions = selectedQuestions.Select(q => new QuizQuestionViewModel
            {
                QuestionId = q.QuestionId,
                QuestionText = q.QuestionText,
                Difficulty = q.Difficulty,
                Explanation = revealAnswers ? q.Explanation : string.Empty,
                CorrectOptionId = revealAnswers ? q.Options.FirstOrDefault(o => o.IsCorrect)?.OptionId : null,
                Options = (quiz.ShuffleOptions
                        ? q.Options.OrderBy(_ => Random.Shared.Next())
                        : q.Options.OrderBy(o => o.OptionId))
                    .Select(o => new QuizOptionViewModel
                    {
                        OptionId = o.OptionId,
                        OptionText = o.OptionText,
                        IsCorrect = revealAnswers && o.IsCorrect
                    })
                    .ToList()
            }).ToList()
        };
    }
}
