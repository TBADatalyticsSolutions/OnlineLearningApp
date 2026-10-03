using Microsoft.AspNetCore.Mvc;

namespace OnlineLearningApp.Controllers;

public class LegalController : Controller
{
    [HttpGet]
    public IActionResult Terms() => View();

    [HttpGet]
    public IActionResult Privacy() => View();

    [HttpGet]
    public IActionResult Refunds() => View();
}
