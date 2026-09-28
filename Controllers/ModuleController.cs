using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;

namespace OnlineLearningApp.Controllers;

[Authorize(Roles = UserRoles.Admin + "," + UserRoles.Instructor)]
public class ModuleController : Controller
{
    private readonly OnlineLearningAppDbContext _context;
    public ModuleController(OnlineLearningAppDbContext context) => _context = context;

    public async Task<IActionResult> Index() => View(await _context.Modules.AsNoTracking().Include(m => m.Course).OrderBy(m => m.CourseId).ThenBy(m => m.ModuleId).ToListAsync());
}
