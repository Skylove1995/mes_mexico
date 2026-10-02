using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
    public class AppUpdateService : IAppUpdateService
    {
        private readonly MMesDbContext _dbContext;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<AppUpdateService> _logger;
        private readonly string _storagePath;

        public AppUpdateService(
            MMesDbContext dbContext,
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<AppUpdateService> logger)
        {
            _dbContext = dbContext;
            _env = env;
            _config = config;
            _logger = logger;

            string relativePath = _config["AppUpdate:StoragePath"] ?? "App_Data/AppUpdates";
            _storagePath = Path.IsPathRooted(relativePath)
                ? relativePath
                : Path.Combine(_env.ContentRootPath, relativePath);

            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        public async Task EnsureTableCreatedAsync()
        {
            try
            {
                string sql = @"
CREATE TABLE IF NOT EXISTS `tb_app_update_releases` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `version` VARCHAR(50) NOT NULL,
  `original_file_name` VARCHAR(255) NOT NULL,
  `stored_file_name` VARCHAR(255) NOT NULL,
  `file_path` VARCHAR(500) NOT NULL,
  `file_size` BIGINT NOT NULL,
  `file_hash_sha256` VARCHAR(64) NOT NULL,
  `release_notes` TEXT NULL,
  `uploaded_by` VARCHAR(100) NULL,
  `uploaded_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `is_active` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  INDEX `idx_version` (`version`),
  INDEX `idx_is_active` (`is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";

                await _dbContext.Database.ExecuteSqlRawAsync(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tự động khởi tạo bảng tb_app_update_releases trong MySQL.");
            }
        }

        public async Task<ClientUpdateRelease> UploadReleaseAsync(IFormFile zipFile, string version, string? releaseNotes, string? uploadedBy)
        {
            if (zipFile == null || zipFile.Length == 0)
            {
                throw new ArgumentException("File rỗng hoặc không hợp lệ.");
            }

            string extension = Path.GetExtension(zipFile.FileName);
            if (!string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".rar", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".7z", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Định dạng file ({extension}) không được hỗ trợ. Vui lòng upload file .zip, .rar, .7z, .exe hoặc .dll.");
            }

            string cleanVersion = version.Trim();

            // Store file
            string storedFileName = $"{cleanVersion}_{Guid.NewGuid():N}{extension}";
            string physicalPath = Path.Combine(_storagePath, storedFileName);

            string sha256Hash;
            using (var stream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await zipFile.CopyToAsync(stream);
            }

            // Calculate SHA256
            using (var stream = File.OpenRead(physicalPath))
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = await sha256.ComputeHashAsync(stream);
                var sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                sha256Hash = sb.ToString();
            }

            var release = new ClientUpdateRelease
            {
                Version = cleanVersion,
                OriginalFileName = Path.GetFileName(zipFile.FileName),
                StoredFileName = storedFileName,
                FilePath = physicalPath,
                FileSize = zipFile.Length,
                FileHashSha256 = sha256Hash,
                ReleaseNotes = releaseNotes,
                UploadedBy = string.IsNullOrWhiteSpace(uploadedBy) ? "Admin" : uploadedBy,
                UploadedAt = DateTime.Now,
                IsActive = true
            };

            _dbContext.ClientUpdateReleases.Add(release);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Đã lưu bản cập nhật version {Version} bởi {User} với SHA256: {Hash}", cleanVersion, uploadedBy, sha256Hash);

            return release;
        }

        public async Task<ClientUpdateRelease?> GetLatestReleaseAsync()
        {
            var releases = await _dbContext.ClientUpdateReleases
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.UploadedAt)
                .ToListAsync();

            if (!releases.Any()) return null;

            // Sort by System.Version if valid
            return releases
                .OrderByDescending(r => ParseVersion(r.Version))
                .ThenByDescending(r => r.UploadedAt)
                .FirstOrDefault();
        }

        public async Task<ClientUpdateRelease?> GetReleaseByVersionAsync(string version)
        {
            return await _dbContext.ClientUpdateReleases
                .FirstOrDefaultAsync(r => r.Version == version && r.IsActive);
        }

        public async Task<List<ClientUpdateRelease>> GetAllReleasesAsync()
        {
            var list = await _dbContext.ClientUpdateReleases
                .OrderByDescending(r => r.UploadedAt)
                .ToListAsync();

            return list
                .OrderByDescending(r => ParseVersion(r.Version))
                .ThenByDescending(r => r.UploadedAt)
                .ToList();
        }

        public async Task<(bool HasUpdate, ClientUpdateRelease? LatestRelease)> CheckForUpdateAsync(string currentVersion)
        {
            var latest = await GetLatestReleaseAsync();
            if (latest == null)
            {
                return (false, null);
            }

            var currentVerParsed = ParseVersion(currentVersion);
            var latestVerParsed = ParseVersion(latest.Version);

            if (latestVerParsed > currentVerParsed)
            {
                return (true, latest);
            }

            return (false, latest);
        }

        public async Task<(Stream FileStream, string ContentType, string DownloadFileName)?> GetDownloadStreamAsync(int releaseId)
        {
            var release = await _dbContext.ClientUpdateReleases.FirstOrDefaultAsync(r => r.Id == releaseId && r.IsActive);
            if (release == null || !File.Exists(release.FilePath))
            {
                return null;
            }

            var memoryStream = new MemoryStream();
            using (var fileStream = new System.IO.FileStream(release.FilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
            {
                await fileStream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;

            string ext = Path.GetExtension(release.OriginalFileName ?? "").ToLowerInvariant();
            string contentType = ext switch
            {
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".7z" => "application/x-7z-compressed",
                ".exe" => "application/octet-stream",
                ".dll" => "application/octet-stream",
                _ => "application/octet-stream"
            };

            return (memoryStream, contentType, release.OriginalFileName ?? "download");

        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var release = await _dbContext.ClientUpdateReleases.FindAsync(id);
            if (release == null) return false;

            release.IsActive = !release.IsActive;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteReleaseAsync(int id)
        {
            var release = await _dbContext.ClientUpdateReleases.FindAsync(id);
            if (release == null) return false;

            _dbContext.ClientUpdateReleases.Remove(release);
            await _dbContext.SaveChangesAsync();

            try
            {
                if (File.Exists(release.FilePath))
                {
                    File.Delete(release.FilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa file vật lý tại {Path}", release.FilePath);
            }

            return true;
        }

        private static Version ParseVersion(string versionStr)
        {
            if (string.IsNullOrWhiteSpace(versionStr))
                return new Version(0, 0, 0, 0);

            // Clean string e.g. "v1.0.2" -> "1.0.2"
            string cleaned = versionStr.TrimStart('v', 'V').Trim();
            if (Version.TryParse(cleaned, out var result))
            {
                return result;
            }

            // Fallback for simple integer or partial versions
            var parts = cleaned.Split('.');
            int major = parts.Length > 0 && int.TryParse(parts[0], out var ma) ? ma : 0;
            int minor = parts.Length > 1 && int.TryParse(parts[1], out var mi) ? mi : 0;
            int build = parts.Length > 2 && int.TryParse(parts[2], out var bu) ? bu : 0;
            int revision = parts.Length > 3 && int.TryParse(parts[3], out var re) ? re : 0;

            return new Version(major, minor, build, revision);
        }
    }
}
