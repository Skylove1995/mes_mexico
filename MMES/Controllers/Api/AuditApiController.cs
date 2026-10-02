using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMES.Data;
using MMES.Models;

namespace MMES.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/audit")]
public class AuditApiController : ControllerBase
{
    private readonly MMesDbContext _context;

    public AuditApiController(MMesDbContext context)
    {
        _context = context;
    }

    #region Meta Options
    [HttpGet("meta/types")]
    public async Task<IActionResult> GetAuditTypes()
    {
        var list = await _context.TbAuditTypes
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Select(t => new { id = t.Id, name = t.AuditType })
            .ToListAsync();
        return Ok(new { success = true, data = list });
    }

    [HttpGet("meta/statuses")]
    public async Task<IActionResult> GetAuditStatuses()
    {
        var list = await _context.TbAuditStatuses
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .Select(s => new { id = s.Id, name = s.Status })
            .ToListAsync();
        return Ok(new { success = true, data = list });
    }

    [HttpGet("meta/depts")]
    public async Task<IActionResult> GetDepts()
    {
        try
        {
            var list = await _context.TbDepts
                .AsNoTracking()
                .Where(d => d.IsActive == null || d.IsActive == 1)
                .OrderBy(d => d.Dept)
                .Select(d => new { id = d.Id, code = d.Dept, name = d.Dept ?? $"Dept #{d.Id}" })
                .ToListAsync();
            return Ok(new { success = true, data = list });
        }
        catch (Exception)
        {
            // Fallback: If isActive column does not exist in MySQL tb_dept table yet
            var list = await _context.TbDepts
                .AsNoTracking()
                .OrderBy(d => d.Dept)
                .Select(d => new { id = d.Id, code = d.Dept, name = d.Dept ?? $"Dept #{d.Id}" })
                .ToListAsync();
            return Ok(new { success = true, data = list });
        }
    }

    [HttpGet("meta/users")]
    public async Task<IActionResult> GetUsers()
    {
        var list = await _context.TbUsers
            .AsNoTracking()
            .OrderBy(u => u.UserName)
            .Select(u => new { id = u.Id, name = u.FullName ?? u.UserName ?? $"User #{u.Id}" })
            .ToListAsync();
        return Ok(new { success = true, data = list });
    }
    #endregion

    #region Sub-Module 1: Audit Schedule & Sessions
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions([FromQuery] int? deptId, [FromQuery] int? auditTypeId, [FromQuery] int? year, [FromQuery] int? month)
    {
        var query = _context.TbAuditSessions
            .Include(s => s.AuditType)
            .Include(s => s.Dept)
            .Include(s => s.Auditor)
            .Include(s => s.CheckedBy)
            .Include(s => s.ApprovedBy)
            .Include(s => s.Status)
            .AsNoTracking();

        if (deptId.HasValue && deptId.Value > 0)
            query = query.Where(s => s.DeptId == deptId.Value);

        if (auditTypeId.HasValue && auditTypeId.Value > 0)
            query = query.Where(s => s.AuditTypeId == auditTypeId.Value);

        if (year.HasValue && year.Value > 0)
            query = query.Where(s => s.AuditMonth.HasValue && s.AuditMonth.Value.Year == year.Value);

        if (month.HasValue && month.Value > 0)
            query = query.Where(s => s.AuditMonth.HasValue && s.AuditMonth.Value.Month == month.Value);

        var sessions = await query
            .OrderByDescending(s => s.AuditMonth)
            .ThenByDescending(s => s.Id)
            .ToListAsync();

        var result = sessions.Select(s => new AuditSessionDto
        {
            Id = s.Id,
            AuditTypeId = s.AuditTypeId,
            AuditTypeName = s.AuditType?.AuditType ?? "LPA",
            DeptId = s.DeptId,
            DeptName = s.Dept?.Dept ?? "N/A",
            AuditMonth = s.AuditMonth,
            Shift = s.Shift ?? "Day",
            AuditorId = s.AuditorId,
            AuditorName = s.Auditor?.FullName ?? s.Auditor?.UserName ?? "Unassigned",
            CheckedById = s.CheckedById,
            CheckedByName = s.CheckedBy?.FullName ?? s.CheckedBy?.UserName,
            CheckedAt = s.CheckedAt,
            ApprovedById = s.ApprovedById,
            ApprovedByName = s.ApprovedBy?.FullName ?? s.ApprovedBy?.UserName,
            ApprovedAt = s.ApprovedAt,
            StatusId = s.StatusId,
            StatusName = s.Status?.Status ?? "Draft",
            TotalScore = s.TotalScore,
            TotalScoreTarget = s.TotalScoreTarget ?? 10.0m,
            ScorePercent = s.ScorePercent,
            Grade = s.Grade ?? "A",
            Note = s.Note,
            CreatedAt = s.CreatedAt
        }).ToList();

        return Ok(new { success = true, data = result });
    }

    [HttpGet("active-sessions")]
    public async Task<IActionResult> GetActiveSessions()
    {
        var list = await _context.TbAuditSessions
            .Include(s => s.AuditType)
            .Include(s => s.Dept)
            .Include(s => s.Status)
            .AsNoTracking()
            .OrderByDescending(s => s.Id)
            .Take(100)
            .Select(s => new
            {
                id = s.Id,
                auditTypeName = s.AuditType != null ? s.AuditType.AuditType : "LPA",
                deptName = s.Dept != null ? s.Dept.Dept : "N/A",
                auditMonth = s.AuditMonth,
                shift = s.Shift ?? "Day",
                statusName = s.Status != null ? s.Status.Status : "Draft"
            })
            .ToListAsync();

        return Ok(new { success = true, data = list });
    }

    [HttpGet("sessions/{id}")]
    public async Task<IActionResult> GetSessionById(int id)
    {
        var session = await _context.TbAuditSessions
            .Include(s => s.AuditType)
            .Include(s => s.Dept)
            .Include(s => s.Auditor)
            .Include(s => s.CheckedBy)
            .Include(s => s.ApprovedBy)
            .Include(s => s.Status)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null)
            return NotFound(new { success = false, message = "Audit session not found." });

        var dto = new AuditSessionDto
        {
            Id = session.Id,
            AuditTypeId = session.AuditTypeId,
            AuditTypeName = session.AuditType?.AuditType ?? "LPA",
            DeptId = session.DeptId,
            DeptName = session.Dept?.Dept ?? "N/A",
            AuditMonth = session.AuditMonth,
            Shift = session.Shift ?? "Day",
            AuditorId = session.AuditorId,
            AuditorName = session.Auditor?.FullName ?? session.Auditor?.UserName ?? "Unassigned",
            CheckedById = session.CheckedById,
            CheckedByName = session.CheckedBy?.FullName,
            CheckedAt = session.CheckedAt,
            ApprovedById = session.ApprovedById,
            ApprovedByName = session.ApprovedBy?.FullName,
            ApprovedAt = session.ApprovedAt,
            StatusId = session.StatusId,
            StatusName = session.Status?.Status ?? "Draft",
            TotalScore = session.TotalScore,
            ScorePercent = session.ScorePercent,
            Grade = session.Grade ?? "A",
            Note = session.Note,
            CreatedAt = session.CreatedAt
        };

        return Ok(new { success = true, data = dto });
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateAuditSessionRequest req)
    {
        if (req == null || req.AuditTypeId <= 0 || req.DeptId <= 0)
            return BadRequest(new { success = false, message = "Invalid audit type or department selection." });

        var draftStatus = await _context.TbAuditStatuses.FirstOrDefaultAsync(s => s.Status == "Draft");
        var statusId = draftStatus?.Id ?? 1;

        var session = new TbAuditSession
        {
            AuditTypeId = req.AuditTypeId,
            DeptId = req.DeptId,
            AuditMonth = req.AuditMonth == default ? DateTime.Today : req.AuditMonth,
            Shift = string.IsNullOrWhiteSpace(req.Shift) ? "Day" : req.Shift.Trim(),
            AuditorId = req.AuditorId,
            StatusId = statusId,
            Note = req.Note,
            TotalScoreTarget = 10.0m,
            CreatedAt = DateTime.Now
        };

        _context.TbAuditSessions.Add(session);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, id = session.Id, message = "Audit session created successfully." });
    }

    [HttpDelete("sessions/{id}")]
    public async Task<IActionResult> DeleteSession(int id)
    {
        var session = await _context.TbAuditSessions
            .Include(s => s.TbAuditResults)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null)
            return NotFound(new { success = false, message = "Audit session not found." });

        if (session.TbAuditResults != null && session.TbAuditResults.Any())
        {
            _context.TbAuditResults.RemoveRange(session.TbAuditResults);
        }

        _context.TbAuditSessions.Remove(session);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Audit session deleted successfully." });
    }
    #endregion

    #region Sub-Module 2: Conduct Audit
    [HttpGet("sessions/{sessionId}/checksheet")]
    public async Task<IActionResult> GetConductChecksheet(int sessionId)
    {
        var session = await _context.TbAuditSessions
            .Include(s => s.AuditType)
            .Include(s => s.Dept)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            return NotFound(new { success = false, message = "Audit session not found." });

        // Fetch checksheet items matching audit type & department (with fallback if dept-specific items are empty)
        var checksheetItems = await _context.TbAuditChecksheets
            .Where(c => c.AuditTypeId == session.AuditTypeId && (c.ForDept == session.DeptId || c.ForDept == null || c.ForDept == 0) && c.Active != "N")
            .OrderBy(c => c.SeqNo.HasValue ? 0 : 1)
            .ThenBy(c => c.SeqNo)
            .ThenBy(c => c.Id)
            .AsNoTracking()
            .ToListAsync();

        if (!checksheetItems.Any())
        {
            checksheetItems = await _context.TbAuditChecksheets
                .Where(c => c.AuditTypeId == session.AuditTypeId && c.Active != "N")
                .OrderBy(c => c.SeqNo.HasValue ? 0 : 1)
                .ThenBy(c => c.SeqNo)
                .ThenBy(c => c.Id)
                .AsNoTracking()
                .ToListAsync();
        }

        // Existing saved results for this session
        var existingResults = await _context.TbAuditResults
            .Where(r => r.SessionId == sessionId)
            .AsNoTracking()
            .ToDictionaryAsync(r => r.AuditItemId ?? 0);

        var list = checksheetItems.Select(c =>
        {
            existingResults.TryGetValue(c.Id, out var res);
            return new AuditChecksheetItemDto
            {
                Id = c.Id,
                AuditTypeId = c.AuditTypeId,
                Category = c.Category ?? "General",
                SeqNo = c.SeqNo,
                AuditItem = c.AuditItem,
                AuditItemTranslate = c.AuditItemTranslate,
                MaxScore = c.MaxScore ?? 10.0m,
                Critical = c.Critical ?? "N",
                TimeLimit = c.TimeLimit,
                Active = c.Active,
                ForDept = c.ForDept,
                ResultId = res?.Id,
                Score = res?.Score,
                Judge = res?.Judge,
                Note = res?.Note
            };
        }).ToList();

        return Ok(new
        {
            success = true,
            session = new
            {
                id = session.Id,
                auditTypeId = session.AuditTypeId,
                auditTypeName = session.AuditType?.AuditType,
                deptId = session.DeptId,
                deptName = session.Dept?.Dept,
                auditMonth = session.AuditMonth,
                shift = session.Shift,
                statusId = session.StatusId,
                totalScore = session.TotalScore,
                scorePercent = session.ScorePercent,
                grade = session.Grade
            },
            items = list
        });
    }

    [HttpPost("sessions/{sessionId}/conduct")]
    public async Task<IActionResult> SubmitConductScore(int sessionId, [FromBody] SubmitAuditConductRequest req)
    {
        var session = await _context.TbAuditSessions
            .Include(s => s.TbAuditResults)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            return NotFound(new { success = false, message = "Audit session not found." });

        var currentUserIdStr = User.FindFirst("UserId")?.Value ?? User.FindFirst("Id")?.Value;
        int.TryParse(currentUserIdStr, out var currentUserId);

        var now = DateTime.Now;

        // Upsert results
        foreach (var item in req.Items)
        {
            if (item.AuditItemId <= 0) continue;

            var result = session.TbAuditResults.FirstOrDefault(r => r.AuditItemId == item.AuditItemId);
            if (result == null)
            {
                result = new TbAuditResult
                {
                    SessionId = sessionId,
                    AuditItemId = item.AuditItemId,
                    Score = item.Score,
                    Judge = item.Judge,
                    Note = item.Note,
                    AuditById = currentUserId > 0 ? currentUserId : null,
                    AuditDate = now
                };
                _context.TbAuditResults.Add(result);
            }
            else
            {
                result.Score = item.Score;
                result.Judge = item.Judge;
                result.Note = item.Note;
                result.AuditDate = now;
                if (currentUserId > 0) result.AuditById = currentUserId;
            }
        }

        // Validate full evaluation completion when submitting/checking/approving
        var actionLower = (req.Action ?? "draft").ToLowerInvariant();
        if (actionLower != "draft")
        {
            var totalRequiredItems = await _context.TbAuditChecksheets
                .CountAsync(c => c.AuditTypeId == session.AuditTypeId && (c.ForDept == session.DeptId || c.ForDept == null || c.ForDept == 0) && c.Active != "N");

            if (totalRequiredItems == 0)
            {
                totalRequiredItems = await _context.TbAuditChecksheets
                    .CountAsync(c => c.AuditTypeId == session.AuditTypeId && c.Active != "N");
            }

            var answeredCount = req.Items.Count(i => i.Score.HasValue && i.AuditItemId > 0);
            if (answeredCount < totalRequiredItems)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Cannot submit audit session: All {totalRequiredItems} items must be evaluated before submitting ({answeredCount}/{totalRequiredItems} completed)."
                });
            }
        }

        // Calculate summary score
        var scoredItems = req.Items.Where(i => i.Score.HasValue).ToList();
        if (scoredItems.Any())
        {
            var avgScore = scoredItems.Average(i => i.Score!.Value);
            session.TotalScore = Math.Round(avgScore, 2);
            session.ScorePercent = Math.Round((avgScore / 10.0m) * 100.0m, 2);

            if (session.ScorePercent >= 90) session.Grade = "A";
            else if (session.ScorePercent >= 80) session.Grade = "B";
            else if (session.ScorePercent >= 70) session.Grade = "C";
            else session.Grade = "D";
        }

        session.UpdatedAt = now;

        // Status action handling
        if (actionLower == "submit")
        {
            var submittedStatus = await _context.TbAuditStatuses.FirstOrDefaultAsync(s => s.Status == "Submitted");
            if (submittedStatus != null) session.StatusId = submittedStatus.Id;
            session.AuditorId = currentUserId > 0 ? currentUserId : session.AuditorId;
        }
        else if (actionLower == "check")
        {
            var checkedStatus = await _context.TbAuditStatuses.FirstOrDefaultAsync(s => s.Status == "Checked");
            if (checkedStatus != null) session.StatusId = checkedStatus.Id;
            session.CheckedById = currentUserId > 0 ? currentUserId : session.CheckedById;
            session.CheckedAt = now;
        }
        else if (actionLower == "approve")
        {
            // Admin sign-off approval
            var userRole = User.FindFirst("Role")?.Value ?? User.FindFirst("auth")?.Value;
            var isAdmin = User.IsInRole("Admin") || string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                return BadRequest(new { success = false, message = "Only Admin role is authorized to approve audit sessions." });
            }

            var approvedStatus = await _context.TbAuditStatuses.FirstOrDefaultAsync(s => s.Status == "Approved");
            if (approvedStatus != null) session.StatusId = approvedStatus.Id;
            session.ApprovedById = currentUserId > 0 ? currentUserId : session.ApprovedById;
            session.ApprovedAt = now;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            totalScore = session.TotalScore,
            scorePercent = session.ScorePercent,
            grade = session.Grade,
            message = "Audit conduct evaluation saved successfully."
        });
    }
    #endregion

    #region Sub-Module 3: Issue Management (CAPA)
    [HttpGet("actions")]
    public async Task<IActionResult> GetAuditActions([FromQuery] int? statusId, [FromQuery] int? picId, [FromQuery] int? deptId)
    {
        var query = _context.TbAuditActions
            .Include(a => a.AuditResult)
                .ThenInclude(r => r!.AuditItem)
            .Include(a => a.AuditResult)
                .ThenInclude(r => r!.Session)
                    .ThenInclude(s => s!.Dept)
            .Include(a => a.Pic)
            .Include(a => a.Status)
            .Include(a => a.CreatedBy)
            .Include(a => a.TbAuditActionEvidences)
            .AsNoTracking();

        if (statusId.HasValue && statusId.Value > 0)
            query = query.Where(a => a.StatusId == statusId.Value);

        if (picId.HasValue && picId.Value > 0)
            query = query.Where(a => a.PicId == picId.Value);

        if (deptId.HasValue && deptId.Value > 0)
            query = query.Where(a => a.AuditResult != null && a.AuditResult.Session != null && a.AuditResult.Session.DeptId == deptId.Value);

        var actions = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var list = actions.Select(a => new AuditActionDto
        {
            Id = a.Id,
            AuditResultId = a.AuditResultId,
            SessionId = a.AuditResult?.SessionId ?? 0,
            AuditTypeName = a.AuditResult?.Session?.AuditType?.AuditType ?? "LPA",
            DeptName = a.AuditResult?.Session?.Dept?.Dept ?? "N/A",
            Category = a.AuditResult?.AuditItem?.Category ?? "General",
            AuditItem = a.AuditResult?.AuditItem?.AuditItem ?? "Item NC",
            ActionType = a.ActionType ?? "NC Corrective Action",
            Description = a.Description,
            PicId = a.PicId,
            PicName = a.Pic?.FullName ?? a.Pic?.UserName ?? "Unassigned",
            DueDate = a.DueDate,
            StatusId = a.StatusId,
            StatusName = a.Status?.Status ?? "Open",
            CompletedAt = a.CompletedAt,
            CreatedById = a.CreatedById,
            CreatedByName = a.CreatedBy?.FullName ?? a.CreatedBy?.UserName,
            CreatedAt = a.CreatedAt,
            Evidences = a.TbAuditActionEvidences.Select(e => e.Url).ToList()
        }).ToList();

        return Ok(new { success = true, data = list });
    }

    [HttpPost("actions")]
    public async Task<IActionResult> CreateAction([FromBody] CreateAuditActionRequest req)
    {
        if (req == null || req.AuditResultId <= 0 || string.IsNullOrWhiteSpace(req.Description))
            return BadRequest(new { success = false, message = "Description and valid AuditResultId are required." });

        var openStatus = await _context.TbAuditStatuses.FirstOrDefaultAsync(s => s.Status == "Open");
        var statusId = openStatus?.Id ?? 6;

        var currentUserIdStr = User.FindFirst("UserId")?.Value ?? User.FindFirst("Id")?.Value;
        int.TryParse(currentUserIdStr, out var currentUserId);

        var action = new TbAuditAction
        {
            AuditResultId = req.AuditResultId,
            ActionType = string.IsNullOrWhiteSpace(req.ActionType) ? "CAPA" : req.ActionType.Trim(),
            Description = req.Description.Trim(),
            PicId = req.PicId,
            DueDate = req.DueDate,
            StatusId = statusId,
            CreatedById = currentUserId > 0 ? currentUserId : null,
            CreatedAt = DateTime.Now
        };

        if (req.EvidenceUrls != null && req.EvidenceUrls.Any())
        {
            foreach (var url in req.EvidenceUrls)
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    action.TbAuditActionEvidences.Add(new TbAuditActionEvidence
                    {
                        Url = url.Trim(),
                        CreatedAt = DateTime.Now
                    });
                }
            }
        }

        _context.TbAuditActions.Add(action);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, id = action.Id, message = "Audit issue created successfully." });
    }

    [HttpPut("actions/{id}/status")]
    public async Task<IActionResult> UpdateActionStatus(int id, [FromBody] UpdateAuditActionStatusRequest req)
    {
        var action = await _context.TbAuditActions
            .Include(a => a.TbAuditActionEvidences)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (action == null)
            return NotFound(new { success = false, message = "Audit action issue not found." });

        action.StatusId = req.StatusId;
        action.UpdatedAt = DateTime.Now;

        var statusObj = await _context.TbAuditStatuses.FirstOrDefaultAsync(s => s.Id == req.StatusId);
        if (statusObj != null && (statusObj.Status == "Closed" || statusObj.Status == "Rectified"))
        {
            action.CompletedAt = DateTime.Now;
        }

        if (req.EvidenceUrls != null && req.EvidenceUrls.Any())
        {
            foreach (var url in req.EvidenceUrls)
            {
                if (!string.IsNullOrWhiteSpace(url) && !action.TbAuditActionEvidences.Any(e => e.Url == url.Trim()))
                {
                    action.TbAuditActionEvidences.Add(new TbAuditActionEvidence
                    {
                        Url = url.Trim(),
                        CreatedAt = DateTime.Now
                    });
                }
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Audit action status updated successfully." });
    }
    #endregion

    #region Sub-Module 4: Audit Summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummaryMetrics([FromQuery] int? year)
    {
        var filterYear = year.HasValue && year.Value > 0 ? year.Value : DateTime.Today.Year;

        var sessions = await _context.TbAuditSessions
            .Include(s => s.Dept)
            .Include(s => s.Status)
            .Where(s => s.AuditMonth.HasValue && s.AuditMonth.Value.Year == filterYear)
            .AsNoTracking()
            .ToListAsync();

        var totalSessions = sessions.Count;
        var completedSessions = sessions.Count(s => s.Status?.Status == "Approved" || s.Status?.Status == "Closed");
        var draftSessions = sessions.Count(s => s.Status?.Status == "Draft");
        var pendingSessions = sessions.Count(s => s.Status?.Status == "Submitted" || s.Status?.Status == "Checked");

        var avgScorePercent = sessions.Where(s => s.ScorePercent.HasValue).Select(s => s.ScorePercent!.Value).DefaultIfEmpty(0).Average();

        var actions = await _context.TbAuditActions
            .Include(a => a.Status)
            .AsNoTracking()
            .ToListAsync();

        var totalIssues = actions.Count;
        var openIssues = actions.Count(a => a.Status?.Status == "Open" || a.Status?.Status == "In Progress");
        var rectifiedIssues = actions.Count(a => a.Status?.Status == "Rectified");
        var closedIssues = actions.Count(a => a.Status?.Status == "Closed");

        var deptGroup = sessions
            .Where(s => s.DeptId.HasValue)
            .GroupBy(s => new { s.DeptId, DeptName = s.Dept?.Dept ?? "Unknown" })
            .Select(g => new DeptAuditPerformanceDto
            {
                DeptId = g.Key.DeptId!.Value,
                DeptName = g.Key.DeptName,
                SessionCount = g.Count(),
                AvgScorePercent = Math.Round(g.Where(s => s.ScorePercent.HasValue).Select(s => s.ScorePercent!.Value).DefaultIfEmpty(0).Average(), 2),
                OpenIssueCount = actions.Count(a => a.AuditResult != null && a.AuditResult.Session != null && a.AuditResult.Session.DeptId == g.Key.DeptId && a.Status?.Status == "Open"),
                Grade = g.Where(s => s.ScorePercent.HasValue).Select(s => s.ScorePercent!.Value).DefaultIfEmpty(0).Average() >= 90 ? "A" : "B"
            })
            .ToList();

        var summary = new AuditSummaryKPIsDto
        {
            TotalSessions = totalSessions,
            CompletedSessions = completedSessions,
            DraftSessions = draftSessions,
            PendingApprovalSessions = pendingSessions,
            AverageScorePercent = Math.Round(avgScorePercent, 2),
            TotalIssues = totalIssues,
            OpenIssues = openIssues,
            RectifiedIssues = rectifiedIssues,
            ClosedIssues = closedIssues,
            DepartmentPerformance = deptGroup
        };

        return Ok(new { success = true, data = summary });
    }
    #endregion

    #region Sub-Module 5: Evidence File Upload
    [HttpPost("evidence/upload")]
    public async Task<IActionResult> UploadEvidence(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "No image file provided." });

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "audit");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/audit/{fileName}";
        return Ok(new { success = true, url = relativeUrl, fileName = file.FileName });
    }
    #endregion
}
