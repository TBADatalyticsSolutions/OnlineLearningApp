using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;

namespace OnlineLearningApp.Controllers;

[AllowAnonymous]
public class VerificationController : Controller
{
    private readonly OnlineLearningAppDbContext _context;

    public VerificationController(OnlineLearningAppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? certificateNumber)
    {
        if (string.IsNullOrWhiteSpace(certificateNumber))
        {
            return View();
        }

        var normalized = certificateNumber.Trim();

        var certificate = await _context.Certificates
            .AsNoTracking()
            .Include(c => c.Student)
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.CertificateNumber == normalized);

        ViewBag.Certificate = certificate;
        ViewBag.SearchNumber = normalized;
        return View();
    }
}
