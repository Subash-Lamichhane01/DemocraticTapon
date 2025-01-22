using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DemocraticTapON.Utilities
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(IFormFile file);
        void DeleteFile(string fileUrl);
        bool IsValidFile(IFormFile file);
    }

    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string[] allowedExtensions = { ".pdf", ".doc", ".docx", ".txt", ".rtf" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public FileService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was provided");

            if (!IsValidFile(file))
                throw new ArgumentException("Invalid file type or size");

            // Create unique filename
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "documents");

            // Create directory if it doesn't exist
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Return relative URL
            return $"/uploads/documents/{fileName}";
        }

        public void DeleteFile(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return;

            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "documents", fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public bool IsValidFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            // Check file size
            if (file.Length > MaxFileSize)
                return false;

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }
    }
}
