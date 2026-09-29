using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;

namespace OnlineLearningApp.Controllers;

[Authorize(Roles = UserRoles.Admin + "," + UserRoles.Instructor)]
public class QuestionController : Controller
{
    private readonly OnlineLearningAppDbContext _context;
    public QuestionController(OnlineLearningAppDbContext context) => _context = context;

    public async Task<IActionResult> Index() => View(await _context.Questions.AsNoTracking().Include(q => q.Quiz).OrderBy(q => q.QuizId).ThenBy(q => q.QuestionId).ToListAsync());
}
