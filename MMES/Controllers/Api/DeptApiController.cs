using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMES.Data;

namespace MMES.Controllers.Api;

[ApiController]
[Route("api/dept")]
public class DeptApiController : ControllerBase
{
    private readonly MMesDbContext _context;

    public DeptApiController(MMesDbContext context)
    {
        _context = context;
    }

    // Danh sach dept dung de chon khi tu dang ky tai khoan.
    // Loai bo moi dept co Authority = "admin" (khong phan biet hoa/thuong)
    // de khong ai tu tao duoc tai khoan quyen admin qua form Register.
    [HttpGet("selectable")]
    public async Task<ActionResult<IEnumerable<DeptOption>>> GetSelectable()
    {
        var depts = await _context.TbDepts
            .Where(d => d.Dept != null)
            .OrderBy(d => d.Dept)
            .Select(d => new { d.Id, d.Dept, d.Authority })
            .ToListAsync();

        var selectable = depts
            .Where(d => !string.Equals(d.Authority, "admin", StringComparison.OrdinalIgnoreCase))
            .Select(d => new DeptOption(d.Id, d.Dept!))
            .ToList();

        return Ok(selectable);
    }
}

public record DeptOption(int Id, string Dept);
