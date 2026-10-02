using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MMES.Models.Dms;

namespace MMES.Services
{
    public interface IDmsService
    {
        Task EnsureDatabaseTablesAsync();

        Task<List<DmsCategoryCountDto>> GetCategoriesWithCountsAsync();
        Task<List<DmsDepartment>> GetDepartmentsAsync();
        Task<List<DmsProcess>> GetProcessesAsync();
        Task<List<DmsMachine>> GetMachinesAsync();
        Task<List<DmsModelItem>> GetModelsAsync();

        Task<DmsPagedResult<DmsDocumentDto>> GetDocumentsPagedAsync(DmsFilterRequest filter);
        Task<DmsDocumentDto?> GetDocumentByIdAsync(int id);
        Task<DmsDocumentDto> CreateDocumentAsync(CreateDocumentRequest request);
        Task<DmsDocumentDto> UpdateVersionAsync(UpdateVersionRequest request);
        Task<bool> ApproveDocumentAsync(int docId, string approvedBy);
        Task<bool> RejectDocumentAsync(int docId, string rejectedBy);
        Task<bool> CheckDocumentNumberExistsAsync(string docNumber);
        Task<List<DmsDocumentVersion>> GetDocumentVersionsAsync(int docId);
        Task<(Stream Stream, string ContentType, string FileName)?> GetFileDownloadAsync(int docId, int? versionId = null);
        Task<(Stream Stream, string ContentType, string FileName)?> GetFilePreviewAsync(int docId, int? versionId = null);

        // Master Data CRUD for Admin
        Task<DmsDepartment> AddDepartmentAsync(DmsDepartment dept);
        Task<bool> UpdateDepartmentAsync(DmsDepartment dept);
        Task<bool> DeleteDepartmentAsync(int id);

        Task<DmsProcess> AddProcessAsync(DmsProcess proc);
        Task<bool> UpdateProcessAsync(DmsProcess proc);
        Task<bool> DeleteProcessAsync(int id);

        Task<DmsMachine> AddMachineAsync(DmsMachine mach);
        Task<bool> UpdateMachineAsync(DmsMachine mach);
        Task<bool> DeleteMachineAsync(int id);

        Task<DmsModelItem> AddModelAsync(DmsModelItem model);
        Task<bool> UpdateModelAsync(DmsModelItem model);
        Task<bool> DeleteModelAsync(int id);

        Task<DmsDocumentType> AddTypeAsync(DmsDocumentType type);
        Task<bool> UpdateTypeAsync(DmsDocumentType type);
        Task<bool> DeleteTypeAsync(int id);
    }
}
