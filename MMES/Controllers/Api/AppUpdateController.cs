using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MMES.Services;

namespace MMES.Controllers.Api
{
    [ApiController]
    [Route("api/app-update")]
    public class AppUpdateController : ControllerBase
    {
        private readonly IAppUpdateService _appUpdateService;
        private readonly ILogger<AppUpdateController> _logger;

        public AppUpdateController(
            IAppUpdateService appUpdateService,
            ILogger<AppUpdateController> logger)
        {
            _appUpdateService = appUpdateService;
            _logger = logger;
        }

        [HttpGet("check")]
        public async Task<IActionResult> CheckForUpdate([FromQuery] string currentVersion)
        {
            if (string.IsNullOrWhiteSpace(currentVersion))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng cung cấp tham số 'currentVersion' (ví dụ: ?currentVersion=1.0.0)."
                });
            }

            try
            {
                var (hasUpdate, latestRelease) = await _appUpdateService.CheckForUpdateAsync(currentVersion);

                if (latestRelease == null)
                {
                    return Ok(new
                    {
                        success = true,
                        hasUpdate = false,
                        currentVersion = currentVersion,
                        latestVersion = currentVersion,
                        downloadUrl = "",
                        sha256 = "",
                        fileSize = 0L,
                        fileName = "",
                        releaseNotes = "Chưa có bản cập nhật nào trên hệ thống.",
                        uploadedAt = (string?)null
                    });
                }

                string baseUrl = $"{Request.Scheme}://{Request.Host}";
                string downloadUrl = $"{baseUrl}/api/app-update/download/latest";

                return Ok(new
                {
                    success = true,
                    hasUpdate = hasUpdate,
                    currentVersion = currentVersion,
                    latestVersion = latestRelease.Version,
                    downloadUrl = downloadUrl,
                    sha256 = latestRelease.FileHashSha256,
                    fileSize = latestRelease.FileSize,
                    fileName = latestRelease.OriginalFileName,
                    releaseNotes = latestRelease.ReleaseNotes ?? "Bản cập nhật mới.",
                    uploadedAt = latestRelease.UploadedAt.ToString("yyyy-MM-ddTHH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra cập nhật ứng dụng.");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("download/latest")]
        public async Task<IActionResult> DownloadLatest()
        {
            try
            {
                var latest = await _appUpdateService.GetLatestReleaseAsync();
                if (latest == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy bản cập nhật nào." });
                }

                var fileResult = await _appUpdateService.GetDownloadStreamAsync(latest.Id);
                if (fileResult == null)
                {
                    return NotFound(new { success = false, message = "File cập nhật không tồn tại trên server." });
                }

                var (fileStream, contentType, downloadFileName) = fileResult.Value;
                return File(fileStream, contentType, downloadFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải bản cập nhật mới nhất.");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("download/{version}")]
        public async Task<IActionResult> DownloadByVersion(string version)
        {
            try
            {
                var release = await _appUpdateService.GetReleaseByVersionAsync(version);
                if (release == null)
                {
                    return NotFound(new { success = false, message = $"Không tìm thấy phiên bản {version}." });
                }

                var fileResult = await _appUpdateService.GetDownloadStreamAsync(release.Id);
                if (fileResult == null)
                {
                    return NotFound(new { success = false, message = "File cập nhật không tồn tại trên server." });
                }

                var (fileStream, contentType, downloadFileName) = fileResult.Value;
                return File(fileStream, contentType, downloadFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải phiên bản {Version}.", version);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
