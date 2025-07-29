# Services Structure - AI CV Analyze

## Overview
Code đã được chia nhỏ thành các service riêng biệt theo chức năng để tăng tính module hóa và dễ bảo trì.

## Cấu trúc Services

### 1. Repository Layer
```
Repositories/
├── Interfaces/
│   ├── IResumeRepository.cs
│   ├── IUserRepository.cs
│   └── IResumeAnalysisResultRepository.cs
└── Implementation/
    ├── ResumeRepository.cs
    ├── UserRepository.cs
    └── ResumeAnalysisResultRepository.cs
```

### 2. Service Layer (Chia theo chức năng)

#### File Processing Services
```
Services/Interfaces/
├── IFileValidationService.cs
└── IPdfProcessingService.cs

Services/Implementation/
├── FileValidationService.cs
└── PdfProcessingService.cs
```

#### AI Analysis Services
```
Services/Interfaces/
├── IDocumentAnalysisService.cs
├── IScoreAnalysisService.cs
├── ICVEditService.cs
└── IJobRecommendationService.cs

Services/Implementation/
├── DocumentAnalysisService.cs
├── ScoreAnalysisService.cs
├── CVEditService.cs
└── JobRecommendationService.cs
```

#### Orchestration Service
```
Services/Interfaces/
└── IResumeAnalysisService.cs

Services/Implementation/
└── ResumeAnalysisService.cs
```

### 3. Utilities
```
Services/Utilities/
├── HttpRetryHelper.cs
└── OpenAIConfiguration.cs
```

### 4. Configuration
```
Configuration/
├── AzureAIConfiguration.cs
└── JobRecommendationConfiguration.cs
```

### 5. DTOs
```
Models/DTOs/
├── ResumeAnalysisDto.cs
└── ScoreAnalysisDto.cs
```

## Chi tiết từng Service

### FileValidationService
- **Chức năng**: Validation file upload (type, size)
- **Methods**: 
  - `IsValidFileType(IFormFile file)`
  - `IsValidFileSize(IFormFile file, long maxSizeInBytes)`
  - `GetFileType(IFormFile file)`
  - `GetFileBytes(IFormFile file)`

### PdfProcessingService
- **Chức năng**: Xử lý PDF conversion
- **Methods**:
  - `ConvertMultiPagePdfToSingleImageAsync(Stream pdfStream)`

### DocumentAnalysisService
- **Chức năng**: Phân tích document bằng Azure Form Recognizer
- **Methods**:
  - `AnalyzeDocumentAsync(Stream documentStream)`
- **Dependencies**: Azure Form Recognizer

### ScoreAnalysisService
- **Chức năng**: Chấm điểm CV bằng OpenAI
- **Methods**:
  - `AnalyzeScoreAsync(string cvContent)`
- **Dependencies**: OpenAI API

### CVEditService
- **Chức năng**: Đưa ra gợi ý và tạo CV mới
- **Methods**:
  - `GetCVEditSuggestionsAsync(string cvContent)`
  - `GenerateFinalCVAsync(string cvContent, string suggestions)`
- **Dependencies**: OpenAI API

### JobRecommendationService
- **Chức năng**: Đề xuất công việc phù hợp
- **Methods**:
  - `GetJobSuggestionsAsync(string skills, string workExperience)`
- **Dependencies**: OpenAI API

### ResumeAnalysisService (Orchestration)
- **Chức năng**: Điều phối các service khác
- **Methods**:
  - `AnalyzeResume(IFormFile cvFile, int userId, bool skipDb)`
  - `AnalyzeScoreWithOpenAI(string cvContent)`
  - `GetJobSuggestionsAsync(string skills, string workExperience)`
  - `GetCVEditSuggestions(string cvContent)`
  - `GenerateFinalCV(string cvContent, string suggestions)`

## Utilities

### HttpRetryHelper
- **Chức năng**: Retry logic cho HTTP requests
- **Methods**:
  - `SendWithRetryAsync(Func<Task<HttpResponseMessage>> sendRequest, int delayMs)`

### OpenAIConfiguration
- **Chức năng**: Helper cho OpenAI configuration
- **Methods**:
  - `GetOpenAIConfig(IConfiguration configuration)`
  - `IsOpenAIConfigured(IConfiguration configuration)`

## Dependency Injection

### Program.cs Registration
```csharp
// Register Repositories
builder.Services.AddScoped<IResumeRepository, ResumeRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IResumeAnalysisResultRepository, ResumeAnalysisResultRepository>();

// Register Services
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<IPdfProcessingService, PdfProcessingService>();
builder.Services.AddScoped<IDocumentAnalysisService, DocumentAnalysisService>();
builder.Services.AddScoped<IScoreAnalysisService, ScoreAnalysisService>();
builder.Services.AddScoped<ICVEditService, CVEditService>();
builder.Services.AddScoped<IJobRecommendationService, JobRecommendationService>();
builder.Services.AddScoped<IResumeAnalysisService, ResumeAnalysisService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
```

## Lợi ích của cấu trúc mới

### 1. Single Responsibility Principle (SRP)
- Mỗi service chỉ có một trách nhiệm duy nhất
- Dễ dàng hiểu và maintain

### 2. Separation of Concerns
- File processing tách biệt với AI analysis
- Mỗi loại AI service riêng biệt

### 3. Reusability
- Các service có thể được sử dụng độc lập
- Dễ dàng test từng component

### 4. Maintainability
- Sửa lỗi chỉ cần focus vào service cụ thể
- Thêm tính năng mới không ảnh hưởng service khác

### 5. Testability
- Dễ dàng mock từng service
- Unit test cho từng chức năng riêng biệt

## Testing Strategy

### Unit Tests
```csharp
[Test]
public async Task FileValidationService_ValidPdfFile_ReturnsTrue()
{
    // Test file validation
}

[Test]
public async Task ScoreAnalysisService_ValidContent_ReturnsScore()
{
    // Test score analysis
}

[Test]
public async Task DocumentAnalysisService_ValidStream_ReturnsAnalysis()
{
    // Test document analysis
}
```

### Integration Tests
```csharp
[Test]
public async Task ResumeAnalysisService_CompleteFlow_ReturnsSuccess()
{
    // Test complete workflow
}
```

## Migration Notes
- Tất cả logic cũ vẫn được giữ nguyên
- Không có thay đổi về functionality
- Controllers không cần thay đổi
- Chỉ thay đổi về cấu trúc code

## Next Steps
1. Thêm logging cho từng service
2. Implement caching cho AI responses
3. Thêm health checks cho từng service
4. Implement circuit breaker pattern
5. Thêm metrics và monitoring 