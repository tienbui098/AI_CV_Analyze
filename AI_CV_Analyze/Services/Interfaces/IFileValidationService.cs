using Microsoft.AspNetCore.Http;

namespace AI_CV_Analyze.Services.Interfaces
{
    public interface IFileValidationService
    {
        bool IsValidFileType(IFormFile file);
        bool IsValidFileSize(IFormFile file, long maxSizeInBytes = 10485760); // 10MB default
        string GetFileType(IFormFile file);
        byte[] GetFileBytes(IFormFile file);
    }
} 