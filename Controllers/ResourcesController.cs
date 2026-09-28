using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OnlineLearningApp.Controllers;

[AllowAnonymous]
public class ResourcesController : Controller
{
    public IActionResult Index() => View();
}
