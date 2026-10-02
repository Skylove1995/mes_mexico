using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MMES.Services;

namespace MMES.Controllers.ViewController
{
    [Authorize]
    public class ClientUpdateController : Controller
    {
        private readonly IAppUpdateService _appUpdateService;
        private readonly ILogger<ClientUpdateController> _logger;

        public ClientUpdateController(
            IAppUpdateService appUpdateService,
            ILogger<ClientUpdateController> logger)
        {
            _appUpdateService = appUpdateService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await _appUpdateService.EnsureTableCreatedAsync();
            var releases = await _appUpdateService.GetAllReleasesAsync();
            return View(releases);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile zipFile, string version, string? releaseNotes)
        {
            if (zipFile == null || zipFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn file cập nhật (.zip, .rar, .7z, .exe, .dll).";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập số phiên bản (Ví dụ: 1.0.1).";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                string uploadedBy = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "Admin";
                var release = await _appUpdateService.UploadReleaseAsync(zipFile, version, releaseNotes, uploadedBy);
                TempData["SuccessMessage"] = $"Đã tải lên thành công bản cập nhật v{release.Version} ({release.OriginalFileName})!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload bản cập nhật Client.");
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            bool success = await _appUpdateService.ToggleActiveAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã thay đổi trạng thái bản cập nhật.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể cập nhật trạng thái.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            bool success = await _appUpdateService.DeleteReleaseAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã xóa bản cập nhật thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy bản cập nhật để xóa.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
