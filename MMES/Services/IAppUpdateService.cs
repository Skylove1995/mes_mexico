using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MMES.Models;

namespace MMES.Services
{
    public interface IAppUpdateService
    {
        Task EnsureTableCreatedAsync();
        Task<ClientUpdateRelease> UploadReleaseAsync(IFormFile zipFile, string version, string? releaseNotes, string? uploadedBy);
        Task<ClientUpdateRelease?> GetLatestReleaseAsync();
        Task<ClientUpdateRelease?> GetReleaseByVersionAsync(string version);
        Task<List<ClientUpdateRelease>> GetAllReleasesAsync();
        Task<(bool HasUpdate, ClientUpdateRelease? LatestRelease)> CheckForUpdateAsync(string currentVersion);
        Task<(Stream FileStream, string ContentType, string DownloadFileName)?> GetDownloadStreamAsync(int releaseId);
        Task<bool> ToggleActiveAsync(int id);
        Task<bool> DeleteReleaseAsync(int id);
    }
}
