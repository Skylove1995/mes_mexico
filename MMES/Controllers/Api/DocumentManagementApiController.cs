using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MMES.Models.Dms;
using MMES.Services;

namespace MMES.Controllers.Api
{
    [ApiController]
    [Route("api/dms")]
    public class DocumentManagementApiController : ControllerBase
    {
        private readonly IDmsService _dmsService;

        public DocumentManagementApiController(IDmsService dmsService)
        {
            _dmsService = dmsService;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var data = await _dmsService.GetCategoriesWithCountsAsync();
            return Ok(data);
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            var data = await _dmsService.GetDepartmentsAsync();
            return Ok(data);
        }

        [HttpGet("processes")]
        public async Task<IActionResult> GetProcesses()
        {
            var data = await _dmsService.GetProcessesAsync();
            return Ok(data);
        }

        [HttpGet("machines")]
        public async Task<IActionResult> GetMachines()
        {
            var data = await _dmsService.GetMachinesAsync();
            return Ok(data);
        }

        [HttpGet("models")]
        public async Task<IActionResult> GetModels()
        {
            var data = await _dmsService.GetModelsAsync();
            return Ok(data);
        }

        [HttpGet("documents")]
        public async Task<IActionResult> GetDocuments(
            [FromQuery] string? search,
            [FromQuery] int? typeId,
            [FromQuery] int? deptId,
            [FromQuery] string? processIds,
            [FromQuery] string? machineIds,
            [FromQuery] string? modelIds,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var filter = new DmsFilterRequest
            {
                Search = search,
                TypeId = typeId,
                DeptId = deptId,
                Status = status,
                Page = page,
                PageSize = pageSize
            };

            if (!string.IsNullOrWhiteSpace(processIds))
            {
                filter.ProcessIds = ParseIntList(processIds);
            }

            if (!string.IsNullOrWhiteSpace(machineIds))
            {
                filter.MachineIds = ParseIntList(machineIds);
            }

            if (!string.IsNullOrWhiteSpace(modelIds))
            {
                filter.ModelIds = ParseIntList(modelIds);
            }

            var result = await _dmsService.GetDocumentsPagedAsync(filter);
            return Ok(result);
        }

        [HttpPost("documents")]
        public async Task<IActionResult> CreateDocument([FromForm] CreateDocumentRequest request)
        {
            try
            {
                var doc = await _dmsService.CreateDocumentAsync(request);
                return Ok(new { success = true, message = "New document uploaded successfully!", data = doc });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("documents/update-version")]
        public async Task<IActionResult> UpdateVersion([FromForm] UpdateVersionRequest request)
        {
            try
            {
                var doc = await _dmsService.UpdateVersionAsync(request);
                return Ok(new { success = true, message = "Document version updated successfully!", data = doc });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("documents/{id}/approve")]
        public async Task<IActionResult> ApproveDocument(int id, [FromQuery] string? approvedBy)
        {
            var ok = await _dmsService.ApproveDocumentAsync(id, approvedBy ?? "Admin");
            if (ok) return Ok(new { success = true, message = "Document approved successfully!" });
            return NotFound(new { success = false, message = "Document not found." });
        }

        [HttpPost("documents/{id}/reject")]
        public async Task<IActionResult> RejectDocument(int id, [FromQuery] string? rejectedBy)
        {
            var ok = await _dmsService.RejectDocumentAsync(id, rejectedBy ?? "Admin");
            if (ok) return Ok(new { success = true, message = "Document rejected successfully!" });
            return NotFound(new { success = false, message = "Document not found." });
        }

        [HttpGet("documents/check-exists")]
        public async Task<IActionResult> CheckDocNumberExists([FromQuery] string docNumber)
        {
            if (string.IsNullOrWhiteSpace(docNumber))
            {
                return Ok(new { exists = false });
            }
            var exists = await _dmsService.CheckDocumentNumberExistsAsync(docNumber);
            return Ok(new { exists = exists, docNumber = docNumber.Trim() });
        }

        [HttpGet("documents/{id}")]
        public async Task<IActionResult> GetDocumentById(int id)
        {
            var doc = await _dmsService.GetDocumentByIdAsync(id);
            if (doc == null) return NotFound(new { success = false, message = "Document not found." });
            return Ok(doc);
        }

        [HttpGet("documents/{id}/versions")]
        public async Task<IActionResult> GetDocumentVersions(int id)
        {
            var versions = await _dmsService.GetDocumentVersionsAsync(id);
            return Ok(versions);
        }

        [HttpGet("documents/{id}/download")]
        public async Task<IActionResult> DownloadDocument(int id, [FromQuery] int? versionId)
        {
            var file = await _dmsService.GetFileDownloadAsync(id, versionId);
            if (file == null) return NotFound("File does not exist or has been removed.");
            return File(file.Value.Stream, file.Value.ContentType, file.Value.FileName);
        }

        [HttpGet("documents/{id}/preview")]
        public async Task<IActionResult> PreviewDocument(int id, [FromQuery] int? versionId)
        {
            var file = await _dmsService.GetFilePreviewAsync(id, versionId);
            if (file == null) return NotFound("File does not exist or has been removed.");
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{file.Value.FileName}\"");
            return File(file.Value.Stream, file.Value.ContentType);
        }

        // Master Data Endpoints
        [HttpPost("master/department")]
        public async Task<IActionResult> AddDepartment([FromBody] DmsDepartment dept)
        {
            var res = await _dmsService.AddDepartmentAsync(dept);
            return Ok(res);
        }

        [HttpPut("master/department/{id}")]
        public async Task<IActionResult> UpdateDepartment(int id, [FromBody] DmsDepartment dept)
        {
            dept.Id = id;
            var ok = await _dmsService.UpdateDepartmentAsync(dept);
            return Ok(new { success = ok });
        }

        [HttpDelete("master/department/{id}")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var ok = await _dmsService.DeleteDepartmentAsync(id);
            return Ok(new { success = ok });
        }

        [HttpPost("master/process")]
        public async Task<IActionResult> AddProcess([FromBody] DmsProcess proc)
        {
            var res = await _dmsService.AddProcessAsync(proc);
            return Ok(res);
        }

        [HttpPut("master/process/{id}")]
        public async Task<IActionResult> UpdateProcess(int id, [FromBody] DmsProcess proc)
        {
            proc.Id = id;
            var ok = await _dmsService.UpdateProcessAsync(proc);
            return Ok(new { success = ok });
        }

        [HttpDelete("master/process/{id}")]
        public async Task<IActionResult> DeleteProcess(int id)
        {
            var ok = await _dmsService.DeleteProcessAsync(id);
            return Ok(new { success = ok });
        }

        [HttpPost("master/machine")]
        public async Task<IActionResult> AddMachine([FromBody] DmsMachine mach)
        {
            var res = await _dmsService.AddMachineAsync(mach);
            return Ok(res);
        }

        [HttpPut("master/machine/{id}")]
        public async Task<IActionResult> UpdateMachine(int id, [FromBody] DmsMachine mach)
        {
            mach.Id = id;
            var ok = await _dmsService.UpdateMachineAsync(mach);
            return Ok(new { success = ok });
        }

        [HttpDelete("master/machine/{id}")]
        public async Task<IActionResult> DeleteMachine(int id)
        {
            var ok = await _dmsService.DeleteMachineAsync(id);
            return Ok(new { success = ok });
        }

        [HttpPost("master/model")]
        public async Task<IActionResult> AddModel([FromBody] DmsModelItem model)
        {
            var res = await _dmsService.AddModelAsync(model);
            return Ok(res);
        }

        [HttpPut("master/model/{id}")]
        public async Task<IActionResult> UpdateModel(int id, [FromBody] DmsModelItem model)
        {
            model.Id = id;
            var ok = await _dmsService.UpdateModelAsync(model);
            return Ok(new { success = ok });
        }

        [HttpDelete("master/model/{id}")]
        public async Task<IActionResult> DeleteModel(int id)
        {
            var ok = await _dmsService.DeleteModelAsync(id);
            return Ok(new { success = ok });
        }

        [HttpPost("master/type")]
        public async Task<IActionResult> AddType([FromBody] DmsDocumentType type)
        {
            var res = await _dmsService.AddTypeAsync(type);
            return Ok(res);
        }

        [HttpPut("master/type/{id}")]
        public async Task<IActionResult> UpdateType(int id, [FromBody] DmsDocumentType type)
        {
            type.Id = id;
            var ok = await _dmsService.UpdateTypeAsync(type);
            return Ok(new { success = ok });
        }

        [HttpDelete("master/type/{id}")]
        public async Task<IActionResult> DeleteType(int id)
        {
            var ok = await _dmsService.DeleteTypeAsync(id);
            return Ok(new { success = ok });
        }

        private static List<int> ParseIntList(string str)
        {
            var list = new List<int>();
            if (string.IsNullOrWhiteSpace(str)) return list;
            foreach (var s in str.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(s.Trim(), out int val)) list.Add(val);
            }
            return list;
        }
    }
}
