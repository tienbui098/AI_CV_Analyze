using System.IO;
using System.Threading.Tasks;

namespace AI_CV_Analyze.Services.Interfaces
{
    public interface IPdfProcessingService
    {
        Task<Stream> ConvertMultiPagePdfToSingleImageAsync(Stream pdfStream);
    }
} 