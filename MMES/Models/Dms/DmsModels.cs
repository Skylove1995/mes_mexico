using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace MMES.Models.Dms
{
    public class DmsDocumentType
    {
        public int Id { get; set; }
        public string TypeCode { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string IconClass { get; set; } = "bi-file-earmark-text";
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    public class DmsDepartment
    {
        public int Id { get; set; }
        public string DeptCode { get; set; } = string.Empty;
        public string DeptName { get; set; } = string.Empty;
        public string BadgeColor { get; set; } = "#0B72B9";
        public bool IsActive { get; set; } = true;
    }

    public class DmsProcess
    {
        public int Id { get; set; }
        public string ProcessCode { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class DmsMachine
    {
        public int Id { get; set; }
        public string MachineCode { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public int? ProcessId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DmsModelItem
    {
        public int Id { get; set; }
        public string ModelCode { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DmsDocument
    {
        public int Id { get; set; }
        public string DocNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int TypeId { get; set; }
        public int DeptId { get; set; }
        public string CurrentVersion { get; set; } = "v1.0";
        public string Status { get; set; } = "Draft"; // Draft, Pending, Approved, Rejected, Obsolete
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? Description { get; set; }

        public DmsDocumentType? Type { get; set; }
        public DmsDepartment? Department { get; set; }
        public List<DmsDocumentVersion> Versions { get; set; } = new();
        public List<DmsDocProcess> Processes { get; set; } = new();
        public List<DmsDocMachine> Machines { get; set; } = new();
        public List<DmsDocModel> Models { get; set; } = new();
    }

    public class DmsDocumentVersion
    {
        public int Id { get; set; }
        public int DocId { get; set; }
        public string VersionNumber { get; set; } = "v1.0";
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileExtension { get; set; } = string.Empty;
        public string? FileHash { get; set; }
        public string? ChangeSummary { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }

    public class DmsDocProcess
    {
        public int DocId { get; set; }
        public int ProcessId { get; set; }
        public DmsProcess? Process { get; set; }
    }

    public class DmsDocMachine
    {
        public int DocId { get; set; }
        public int MachineId { get; set; }
        public DmsMachine? Machine { get; set; }
    }

    public class DmsDocModel
    {
        public int DocId { get; set; }
        public int ModelId { get; set; }
        public DmsModelItem? Model { get; set; }
    }

    // --- DTOs ---

    public class DmsCategoryCountDto
    {
        public int TypeId { get; set; }
        public string TypeCode { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class DmsDocumentDto
    {
        public int Id { get; set; }
        public string DocNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int TypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public int DeptId { get; set; }
        public string DeptCode { get; set; } = string.Empty;
        public string DeptBadgeColor { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = "v1.0";
        public string Status { get; set; } = "Draft";
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? Description { get; set; }
        public string FileExtension { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }

        public List<string> ProcessNames { get; set; } = new();
        public List<string> MachineNames { get; set; } = new();
        public List<string> ModelNames { get; set; } = new();
    }

    public class CreateDocumentRequest
    {
        public string DocNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int TypeId { get; set; }
        public int DeptId { get; set; }
        public string? Description { get; set; }
        public string CreatedBy { get; set; } = "Admin";
        public List<int>? ProcessIds { get; set; }
        public List<int>? MachineIds { get; set; }
        public List<int>? ModelIds { get; set; }
        public IFormFile? File { get; set; }
    }

    public class UpdateVersionRequest
    {
        public int DocId { get; set; }
        public string NewVersionNumber { get; set; } = "v1.1";
        public string? ChangeSummary { get; set; }
        public string UploadedBy { get; set; } = "Admin";
        public IFormFile? File { get; set; }
    }

    public class DmsFilterRequest
    {
        public string? Search { get; set; }
        public int? TypeId { get; set; }
        public int? DeptId { get; set; }
        public List<int>? ProcessIds { get; set; }
        public List<int>? MachineIds { get; set; }
        public List<int>? ModelIds { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class DmsPagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    }
}
