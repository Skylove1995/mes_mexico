using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MMES.Controllers.ViewController;

[Authorize]
public class CsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult AuditManagement()
    {
        return View();
    }
}
