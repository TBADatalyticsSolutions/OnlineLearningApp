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

    public QuizController(OnlineLearningAppDbContext context) => _context = context;
    private static string QuestionSetKey(string studentId, int quizId) => $"{QuestionSetPrefix}{studentId}:{quizId}";
    private static string StartTimeKey(string studentId, int quizId) => $"{StartTimePrefix}{studentId}:{quizId}";

    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Instructor)]
    [HttpGet]
    public async Task<IActionResult> Index() => View(await _context.Quizzes.AsNoTracking().Include(q => q.Module).OrderBy(q => q.QuizId).ToListAsync());

    [Authorize(Roles = UserRoles.Student)]
    [HttpGet]
    public async Task<IActionResult> History()
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId)) return Challenge();
        var attempts = await _context.QuizAttempts.AsNoTracking().Where(a => a.StudentId == studentId).Include(a => a.Quiz).ThenInclude(q => q.Module).ThenInclude(m => m.Course).OrderByDescending(a => a.AttemptedAt).Select(a => new QuizHistoryItemViewModel { QuizName = a.Quiz.QuizName, CourseName = a.Quiz.Module.Course.CourseName, Score = a.Score, TotalQuestions = a.TotalQuestions, Percentage = a.Percentage, Passed = a.Passed, AttemptedAt = a.AttemptedAt }).ToListAsync();
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

        if (quiz.Module.Course.RequirePaymentForAssessment && !await HasPaidForCourseAsync(studentId, quiz.Module.CourseId))
        {
            TempData["Error"] = "Payment is required before you can take graded assessments. Please complete payment for this course first.";
            return RedirectToAction("ShoppingCart", "Orders");
        }

        var previousAttempts = await _context.QuizAttempts.AsNoTracking().CountAsync(a => a.StudentId == studentId && a.QuizId == id);
        if (quiz.AttemptLimit.HasValue && previousAttempts >= quiz.AttemptLimit.Value)
        {
            TempData["Error"] = $"You have reached the {quiz.AttemptLimit.Value}-attempt limit for this assessment.";
            return RedirectToAction("Learn", "Course", new { id = quiz.Module.CourseId });
        }

        var viewModel = BuildViewModel(quiz);
        HttpContext.Session.SetString(QuestionSetKey(studentId, id), string.Join(",", viewModel.SelectedQuestionIds));
        HttpContext.Session.SetString(StartTimeKey(studentId, id), DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
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
        if (!await IsEnrolledAsync(studentId, quiz.Module.CourseId)) return RedirectToAction("Details", "Course", new { id = quiz.Module.CourseId });
        if (quiz.Module.Course.RequirePaymentForAssessment && !await HasPaidForCourseAsync(studentId, quiz.Module.CourseId))
        {
            TempData["Error"] = "Payment is required before submitting a graded assessment.";
            return RedirectToAction("ShoppingCart", "Orders");
        }

        var previousAttempts = await _context.QuizAttempts.AsNoTracking().CountAsync(a => a.StudentId == studentId && a.QuizId == quiz.QuizId);
        if (quiz.AttemptLimit.HasValue && previousAttempts >= quiz.AttemptLimit.Value) return RedirectToAction("Learn", "Course", new { id = quiz.Module.CourseId });

        var selectedIds = ParseIds(HttpContext.Session.GetString(QuestionSetKey(studentId, quiz.QuizId)));
        var selectedQuestions = quiz.Questions.Where(q => selectedIds.Contains(q.QuestionId)).ToList();
        if (selectedQuestions.Count == 0)
        {
            ClearAssessmentSession(studentId, quiz.QuizId);
            TempData["Error"] = "Your assessment session has expired. Please start again.";
            return RedirectToAction(nameof(Take), new { id = quiz.QuizId });
        }

        var answers = model.Answers ?? new Dictionary<int, int>();
        var score = selectedQuestions.Count(q => answers.TryGetValue(q.QuestionId, out var optionId) && q.Options.Any(o => o.OptionId == optionId && o.IsCorrect));
        var totalQuestions = selectedQuestions.Count;
        var percentage = totalQuestions == 0 ? 0m : Math.Round(score * 100m / totalQuestions, 2);
        var timeExpired = HasTimeExpired(studentId, quiz);
        var passed = !timeExpired && totalQuestions > 0 && percentage >= quiz.PassMark;
        var attemptedAt = DateTime.UtcNow;

        _context.QuizAttempts.Add(new QuizAttempt { StudentId = studentId, QuizId = quiz.QuizId, Score = score, TotalQuestions = totalQuestions, Percentage = percentage, Passed = passed, AttemptedAt = attemptedAt });
        await _context.SaveChangesAsync();
        ClearAssessmentSession(studentId, quiz.QuizId);

        var result = BuildViewModel(quiz, selectedQuestions.Select(q => q.QuestionId), true);
        result.Answers = answers; result.SelectedQuestionIds = selectedQuestions.Select(q => q.QuestionId).ToList(); result.Submitted = true; result.Score = score; result.TotalQuestions = totalQuestions; result.AttemptNumber = previousAttempts + 1; result.LastScore = score; result.LastTotalQuestions = totalQuestions; result.LastPercentage = percentage; result.AttemptedAt = attemptedAt; result.TimeExpired = timeExpired;
        return View("Take", result);
    }

    private Task<Quiz?> LoadQuizAsync(int quizId) => _context.Quizzes.AsNoTracking().Include(q => q.Module).ThenInclude(m => m.Course).Include(q => q.Questions).ThenInclude(q => q.Options).FirstOrDefaultAsync(q => q.QuizId == quizId);
    private Task<bool> IsEnrolledAsync(string studentId, int courseId) => _context.StudentCourses.AnyAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);
    private Task<bool> HasPaidForCourseAsync(string studentId, int courseId) => _context.Orders.AnyAsync(o => o.AccountId == studentId && o.PaymentStatus == PaymentStatus.Paid && o.OrderItems.Any(i => i.CourseId == courseId));
    private async Task PopulateLatestAttemptAsync(QuizAttemptViewModel model, string studentId, int quizId)
    {
        var latest = await _context.QuizAttempts.AsNoTracking().Where(a => a.StudentId == studentId && a.QuizId == quizId).OrderByDescending(a => a.AttemptedAt).FirstOrDefaultAsync();
        if (latest is null) return;
        model.LastScore = latest.Score; model.LastTotalQuestions = latest.TotalQuestions; model.LastPercentage = latest.Percentage; model.AttemptedAt = latest.AttemptedAt;
    }
    private bool HasTimeExpired(string studentId, Quiz quiz)
    {
        if (!quiz.TimeLimitMinutes.HasValue) return false;
        var raw = HttpContext.Session.GetString(StartTimeKey(studentId, quiz.QuizId));
        if (!long.TryParse(raw, out var start)) return true;
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() - start > TimeSpan.FromMinutes(quiz.TimeLimitMinutes.Value).TotalSeconds;
    }
    private void ClearAssessmentSession(string studentId, int quizId) { HttpContext.Session.Remove(QuestionSetKey(studentId, quizId)); HttpContext.Session.Remove(StartTimeKey(studentId, quizId)); }
    private static HashSet<int> ParseIds(string? value) => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => int.TryParse(x, out var id) ? id : 0).Where(id => id > 0).ToHashSet();
    private static QuizAttemptViewModel BuildViewModel(Quiz quiz, IEnumerable<int>? selectedQuestionIds = null, bool revealAnswers = false)
    {
        IEnumerable<Question> questions = quiz.Questions;
        if (selectedQuestionIds is null)
        {
            questions = quiz.ShuffleQuestions ? questions.OrderBy(_ => Random.Shared.Next()) : questions.OrderBy(q => q.QuestionId);
            questions = questions.Take(Math.Min(QuestionsPerAttempt, quiz.Questions.Count));
        }
        else
        {
            var ids = selectedQuestionIds.ToHashSet();
            questions = questions.Where(q => ids.Contains(q.QuestionId)).OrderBy(q => quiz.ShuffleQuestions ? Random.Shared.Next() : q.QuestionId);
        }
        var selected = questions.ToList();
        return new QuizAttemptViewModel
        {
            QuizId = quiz.QuizId, QuizName = quiz.QuizName, Description = quiz.Description, CourseId = quiz.Module.CourseId, CourseName = quiz.Module.Course.CourseName, PassMark = quiz.PassMark, TimeLimitMinutes = quiz.TimeLimitMinutes, AttemptLimit = quiz.AttemptLimit, IsPractice = quiz.IsPractice, TotalQuestions = selected.Count, SelectedQuestionIds = selected.Select(q => q.QuestionId).ToList(),
            Questions = selected.Select(q => new QuizQuestionViewModel { QuestionId = q.QuestionId, QuestionText = q.QuestionText, Difficulty = q.Difficulty, Explanation = revealAnswers ? q.Explanation : string.Empty, CorrectOptionId = revealAnswers ? q.Options.FirstOrDefault(o => o.IsCorrect)?.OptionId : null, Options = (quiz.ShuffleOptions ? q.Options.OrderBy(_ => Random.Shared.Next()) : q.Options.OrderBy(o => o.OptionId)).Select(o => new QuizOptionViewModel { OptionId = o.OptionId, OptionText = o.OptionText, IsCorrect = revealAnswers && o.IsCorrect }).ToList() }).ToList()
        };
    }
}
