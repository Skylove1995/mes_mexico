using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMES.Data;
using MMES.Models;

namespace MMES.Controllers.ViewController;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly MMesDbContext _context;

    public HomeController(ILogger<HomeController> logger, MMesDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        var tiles = new List<DashboardDeptTile>
        {
            new("Quality", "Quality Assurance", "/images/quality.jpg"),
            new("CS", "Customer Service", "/images/CS.jpeg"),
            new("SMT", "Surface Mount", "/images/smt.jpg"),
            new("PCBA", "PCB Assembly", "/images/pcba.jpg"),
            new("SCM", "Supply Chain", "/images/SCM.avif"),
            new("WH", "Warehouse", "/images/WH.jpg"),
        };

        return View(tiles);
    }

    public IActionResult TestDb()
    {
        var canConnect = _context.Database.CanConnect();
        return Content(canConnect ? "MySQL OK: ket noi thanh cong" : "MySQL FAIL: khong ket noi duoc");
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
