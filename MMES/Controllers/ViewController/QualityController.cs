using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMES.Data;
using MMES.Models;

namespace MMES.Controllers.ViewController;

[Authorize]
public class QualityController : Controller
{
    private readonly MMesDbContext _context;

    public QualityController(MMesDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult BlockRelease()
    {
        return View();
    }

    public IActionResult OqcScanout()
    {
        return View();
    }

    [HttpGet("/api/quality/block-release/history")]
    public async Task<IActionResult> GetHistory([FromQuery] string? search)
    {
        var query = _context.TbProductionBlocks.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(b => b.Pid.Contains(term) || (b.UserId != null && b.UserId.Contains(term)) || (b.Reason != null && b.Reason.Contains(term)));
        }

        var list = await query
            .OrderByDescending(b => b.UpdatedAt)
            .Take(100)
            .Select(b => new
            {
                pid = b.Pid,
                status = (int)b.Status,
                userBlock = b.UserId ?? "N/A",
                reason = b.Reason ?? "",
                updatedAt = b.UpdatedAt.ToString("dd/MM/yyyy HH:mm"),
                createdAt = b.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            })
            .ToListAsync();

        return Json(new { success = true, data = list });
    }

    [HttpPost("/api/quality/block-release/execute")]
    public async Task<IActionResult> ExecuteBlockRelease([FromBody] BlockReleaseDto dto)
    {
        if (dto == null || dto.Pids == null || dto.Pids.Count == 0)
        {
            return BadRequest(new { success = false, message = "[EN] Warning: No PID provided! / [ES] Advertencia: ¡No se đã nhập PID!" });
        }

        var currentUserFullName = User.FindFirst("FullName")?.Value;
        if (string.IsNullOrWhiteSpace(currentUserFullName))
        {
            currentUserFullName = User.Identity?.Name ?? "QA Operator";
        }

        var action = (dto.Action ?? "block").ToLowerInvariant().Trim();
        var isBlockAction = action == "block";
        var userReason = string.IsNullOrWhiteSpace(dto.Reason) ? "N/A" : dto.Reason.Trim();

        // Unique non-empty PIDs
        var cleanPids = dto.Pids
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (cleanPids.Count == 0)
        {
            return BadRequest(new { success = false, message = "[EN] Warning: Valid PID list is empty! / [ES] Advertencia: ¡La lista de PID válidos está vacía!" });
        }

        var results = new List<BlockReleaseItemResultDto>();
        var now = DateTime.Now;

        foreach (var pid in cleanPids)
        {
            var record = await _context.TbProductionBlocks.FirstOrDefaultAsync(b => b.Pid == pid);

            if (isBlockAction)
            {
                if (record != null && record.Status == 1)
                {
                    results.Add(new BlockReleaseItemResultDto
                    {
                        Pid = pid,
                        Success = false,
                        IsWarning = true,
                        Status = 1,
                        Message = $"[EN] Warning: PID '{pid}' is currently BLOCKED! / [ES] Advertencia: ¡El PID '{pid}' ya está BLOQUEADO!"
                    });
                }
                else
                {
                    var logEntry = $"[{now:yyyy-MM-dd HH:mm:ss}] Block by {currentUserFullName}: {userReason}";
                    if (record == null)
                    {
                        record = new TbProductionBlock
                        {
                            Pid = pid,
                            Status = 1,
                            CreatedAt = now,
                            UpdatedAt = now,
                            UserId = currentUserFullName,
                            Reason = logEntry
                        };
                        _context.TbProductionBlocks.Add(record);
                    }
                    else
                    {
                        record.Status = 1;
                        record.UpdatedAt = now;
                        record.UserId = currentUserFullName;
                        record.Reason = string.IsNullOrWhiteSpace(record.Reason) ? logEntry : record.Reason.Trim() + " " + logEntry;
                    }

                    results.Add(new BlockReleaseItemResultDto
                    {
                        Pid = pid,
                        Success = true,
                        IsWarning = false,
                        Status = 1,
                        Message = $"[EN] PID '{pid}' successfully BLOCKED. / [ES] PID '{pid}' BLOQUEADO con éxito."
                    });
                }
            }
            else // Release Action
            {
                if (record == null)
                {
                    results.Add(new BlockReleaseItemResultDto
                    {
                        Pid = pid,
                        Success = false,
                        IsWarning = true,
                        Status = 0,
                        Message = $"[EN] Warning: PID '{pid}' is not in blocked state! / [ES] Advertencia: ¡El PID '{pid}' no está en estado bloqueado!"
                    });
                }
                else if (record.Status == 0)
                {
                    results.Add(new BlockReleaseItemResultDto
                    {
                        Pid = pid,
                        Success = false,
                        IsWarning = true,
                        Status = 0,
                        Message = $"[EN] Warning: PID '{pid}' is already RELEASED! / [ES] Advertencia: ¡El PID '{pid}' ya está LIBERADO!"
                    });
                }
                else
                {
                    var logEntry = $"[{now:yyyy-MM-dd HH:mm:ss}] Release by {currentUserFullName}: {userReason}";
                    record.Status = 0;
                    record.UpdatedAt = now;
                    record.UserId = currentUserFullName;
                    record.Reason = string.IsNullOrWhiteSpace(record.Reason) ? logEntry : record.Reason.Trim() + " " + logEntry;

                    results.Add(new BlockReleaseItemResultDto
                    {
                        Pid = pid,
                        Success = true,
                        IsWarning = false,
                        Status = 0,
                        Message = $"[EN] PID '{pid}' successfully RELEASED. / [ES] PID '{pid}' LIBERADO con éxito."
                    });
                }
            }
        }

        await _context.SaveChangesAsync();

        var successCount = results.Count(r => r.Success);
        var warningCount = results.Count(r => r.IsWarning);

        return Json(new
        {
            success = true,
            totalProcessed = results.Count,
            successCount,
            warningCount,
            results
        });
    }
}

public class BlockReleaseDto
{
    public string? Action { get; set; }
    public List<string>? Pids { get; set; }
    public string? Reason { get; set; }
}

public class BlockReleaseItemResultDto
{
    public string Pid { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool IsWarning { get; set; }
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;
}
