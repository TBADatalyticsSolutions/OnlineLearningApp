using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;

namespace OnlineLearningApp.Controllers;

[Authorize(Roles = UserRoles.Admin + "," + UserRoles.Instructor)]
public class OptionController : Controller
{
    private readonly OnlineLearningAppDbContext _context;
    public OptionController(OnlineLearningAppDbContext context) => _context = context;

    public async Task<IActionResult> Index() => View(await _context.Options.AsNoTracking().Include(o => o.Question).OrderBy(o => o.QuestionId).ThenBy(o => o.OptionId).ToListAsync());
}
