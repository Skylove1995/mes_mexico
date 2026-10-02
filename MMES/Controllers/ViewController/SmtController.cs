using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MMES.Controllers.ViewController;

[Authorize]
public class SmtController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult ProductionRate()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpGet("view/smt/dashboard")]
    [HttpGet("Smt/SmtDashboard")]
    public IActionResult SmtDashboard()
    {
        return View("SmtDashboard");
    }
}
