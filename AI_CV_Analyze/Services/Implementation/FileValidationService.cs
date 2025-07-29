using AI_CV_Analyze.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;

namespace AI_CV_Analyze.Services.Implementation
{
    public class FileValidationService : IFileValidationService
    {
        private readonly string[] _supportedTypes = { "application/pdf", "image/png", "image/jpeg" };

        public bool IsValidFileType(IFormFile file)
        {
            if (file == null || string.IsNullOrEmpty(file.ContentType))
                return false;

            return _supportedTypes.Contains(file.ContentType.ToLowerInvariant());
        }

        public bool IsValidFileSize(IFormFile file, long maxSizeInBytes = 10485760)
        {
            if (file == null)
                return false;

            return file.Length <= maxSizeInBytes;
        }

        public string GetFileType(IFormFile file)
        {
            if (file == null || string.IsNullOrEmpty(file.FileName))
                return "UNKNOWN";

            string ext = Path.GetExtension(file.FileName)?.ToLower();
            return ext switch
            {
                ".pdf" => "PDF",
                ".jpg" or ".jpeg" => "JPG",
                ".png" => "PNG",
                _ => "UNKNOWN"
            };
        }

        public byte[] GetFileBytes(IFormFile file)
        {
            if (file == null)
                return new byte[0];

            using var ms = new MemoryStream();
            file.CopyTo(ms);
            return ms.ToArray();
        }
    }
} 