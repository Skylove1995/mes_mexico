using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MMES.Models;

namespace MMES.Services
{
    public interface IFileStorageService
    {
        Task<StoredFile> UploadFileAsync(IFormFile file, string? uploadedBy);
        Task<(Stream FileStream, string ContentType, string DownloadFileName)?> GetFileForDownloadAsync(int fileId);
        Task<List<StoredFile>> GetAllFilesAsync();
        Task<StoredFile?> GetFileByIdAsync(int fileId);
        Task<bool> DeleteFileAsync(int fileId);
    }
}
