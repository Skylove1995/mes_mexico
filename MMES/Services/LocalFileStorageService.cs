using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MMES.Data;
using MMES.Models;

namespace MMES.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly MMesDbContext _dbContext;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<LocalFileStorageService> _logger;
        private readonly string _storagePath;
        private readonly long _maxFileSizeBytes;
        private readonly HashSet<string> _allowedExtensions;

        public LocalFileStorageService(
            MMesDbContext dbContext,
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<LocalFileStorageService> logger)
        {
            _dbContext = dbContext;
            _env = env;
            _config = config;
            _logger = logger;

            string relativePath = _config["FileStorage:StoragePath"] ?? "App_Data/Uploads";
            _storagePath = Path.IsPathRooted(relativePath)
                ? relativePath
                : Path.Combine(_env.ContentRootPath, relativePath);

            int maxMB = _config.GetValue<int>("FileStorage:MaxFileSizeMB", 50);
            _maxFileSizeBytes = (long)maxMB * 1024 * 1024;

            var extArray = _config.GetSection("FileStorage:AllowedExtensions").Get<string[]>()
                ?? new[] { ".pdf", ".docx", ".xlsx", ".png", ".jpg", ".jpeg", ".gif", ".zip", ".rar", ".7z", ".csv", ".txt" };
            
            _allowedExtensions = new HashSet<string>(extArray, StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        public async Task<StoredFile> UploadFileAsync(IFormFile file, string? uploadedBy)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File rỗng hoặc không hợp lệ.");
            }

            if (file.Length > _maxFileSizeBytes)
            {
                throw new InvalidOperationException($"Dung lượng file vượt quá giới hạn tối đa ({_maxFileSizeBytes / (1024 * 1024)}MB).");
            }

            string originalFileName = Path.GetFileName(file.FileName);
            string extension = Path.GetExtension(originalFileName);

            if (!string.IsNullOrEmpty(extension) && !_allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException($"Định dạng file ({extension}) không được hỗ trợ.");
            }

            string storedFileName = $"{Guid.NewGuid():N}{extension}";
            string physicalPath = Path.Combine(_storagePath, storedFileName);

            using (var stream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write))
            {
                await file.CopyToAsync(stream);
            }

            var storedFile = new StoredFile
            {
                OriginalFileName = originalFileName,
                StoredFileName = storedFileName,
                FilePath = physicalPath,
                FileSize = file.Length,
                ContentType = file.ContentType ?? "application/octet-stream",
                UploadedBy = string.IsNullOrWhiteSpace(uploadedBy) ? "System" : uploadedBy,
                UploadedAt = DateTime.Now,
                IsDeleted = false
            };

            _dbContext.StoredFiles.Add(storedFile);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("File {FileName} uploaded successfully as {StoredName} by {User}", originalFileName, storedFileName, uploadedBy);

            return storedFile;
        }

        public async Task<(Stream FileStream, string ContentType, string DownloadFileName)?> GetFileForDownloadAsync(int fileId)
        {
            var storedFile = await _dbContext.StoredFiles.FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted);
            if (storedFile == null)
            {
                return null;
            }

            if (!File.Exists(storedFile.FilePath))
            {
                _logger.LogWarning("File physical path not found: {Path}", storedFile.FilePath);
                return null;
            }

            var memoryStream = new MemoryStream();
            using (var fileStream = new FileStream(storedFile.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await fileStream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;

            return (memoryStream, storedFile.ContentType, storedFile.OriginalFileName);
        }

        public async Task<List<StoredFile>> GetAllFilesAsync()
        {
            return await _dbContext.StoredFiles
                .Where(f => !f.IsDeleted)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();
        }

        public async Task<StoredFile?> GetFileByIdAsync(int fileId)
        {
            return await _dbContext.StoredFiles.FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted);
        }

        public async Task<bool> DeleteFileAsync(int fileId)
        {
            var storedFile = await _dbContext.StoredFiles.FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted);
            if (storedFile == null)
            {
                return false;
            }

            storedFile.IsDeleted = true;
            await _dbContext.SaveChangesAsync();

            try
            {
                if (File.Exists(storedFile.FilePath))
                {
                    File.Delete(storedFile.FilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting physical file at {Path}", storedFile.FilePath);
            }

            return true;
        }
    }
}
