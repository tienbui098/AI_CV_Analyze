using AI_CV_Analyze.Services.Interfaces;
using PdfiumViewer;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AI_CV_Analyze.Services.Implementation
{
    public class PdfProcessingService : IPdfProcessingService
    {
        public async Task<Stream> ConvertMultiPagePdfToSingleImageAsync(Stream pdfStream)
        {
            try
            {
                // Load PDF
                using var pdfDocument = PdfDocument.Load(pdfStream);
                int pageCount = pdfDocument.PageCount;

                if (pageCount == 1)
                {
                    // Nếu chỉ có 1 trang, render thành 1 ảnh luôn để xử lý thống nhất
                    using var bitmap = pdfDocument.Render(0, 2480, 3508, true);
                    using var ms = new MemoryStream();
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;
                    return new MemoryStream(ms.ToArray());  // return bản sao
                }

                var renderedImages = new List<Image<Rgba32>>();

                // Render từng trang thành ảnh
                for (int i = 0; i < pageCount; i++)
                {
                    using var bitmap = pdfDocument.Render(i, 2480, 3508, true); // A4 300dpi
                    using var ms = new MemoryStream();
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;

                    var image = await Image.LoadAsync<Rgba32>(ms);
                    renderedImages.Add(image);
                }

                // Tính kích thước ảnh tổng (chiều dọc)
                int totalHeight = renderedImages.Sum(img => img.Height);
                int maxWidth = renderedImages.Max(img => img.Width);

                // Tạo ảnh trống để ghép
                using var finalImage = new Image<Rgba32>(maxWidth, totalHeight);
                int yOffset = 0;

                foreach (var image in renderedImages)
                {
                    finalImage.Mutate(ctx => ctx.DrawImage(image, new Point(0, yOffset), 1f));
                    yOffset += image.Height;
                }

                // Lưu ảnh ghép thành stream
                var outputStream = new MemoryStream();
                await finalImage.SaveAsync(outputStream, new PngEncoder());
                outputStream.Position = 0;

                return outputStream;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error processing PDF: {ex.Message}", ex);
            }
        }
    }
} 