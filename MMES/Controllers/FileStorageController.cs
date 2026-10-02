using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MMES.Services;

namespace MMES.Controllers
{
    public class FileStorageController : Controller
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<FileStorageController> _logger;

        public FileStorageController(
            IFileStorageService fileStorageService,
            ILogger<FileStorageController> logger)
        {
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var files = await _fileStorageService.GetAllFilesAsync();
            return View(files);
        }

        [HttpGet]
        public async Task<IActionResult> ListJson()
        {
            var files = await _fileStorageService.GetAllFilesAsync();
            return Json(new { success = true, data = files });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn file cần tải lên.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                string uploadedBy = User.Identity?.Name ?? "Anonymous";
                var result = await _fileStorageService.UploadFileAsync(file, uploadedBy);
                TempData["SuccessMessage"] = $"Tải file '{result.OriginalFileName}' thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload file.");
                TempData["ErrorMessage"] = $"Lỗi khi tải file lên: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UploadApi(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Vui lòng chọn file cần tải lên." });
            }

            try
            {
                string uploadedBy = User.Identity?.Name ?? "Anonymous";
                var result = await _fileStorageService.UploadFileAsync(file, uploadedBy);
                return Ok(new { success = true, message = "Upload thành công!", file = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi API upload file.");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var fileData = await _fileStorageService.GetFileForDownloadAsync(id);
            if (fileData == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy file hoặc file đã bị xóa.";
                return RedirectToAction(nameof(Index));
            }

            var (fileStream, contentType, downloadFileName) = fileData.Value;
            return File(fileStream, contentType, downloadFileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            bool success = await _fileStorageService.DeleteFileAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Xóa file thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể xóa file.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
