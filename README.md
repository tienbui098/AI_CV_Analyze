# AI CV Analysis System

Hệ thống phân tích CV tự động sử dụng AI, tích hợp nhiều công nghệ Azure AI để phân tích và đánh giá CV một cách toàn diện.

## Tính năng chính

- Phân tích hình ảnh CV sử dụng Azure Computer Vision
- Trích xuất thông tin từ CV sử dụng Azure Form Recognizer
- Phân tích ngôn ngữ và từ khóa với Azure Text Analytics
- Phân tích nội dung CV với OpenAI GPT-4
- Đánh giá và đề xuất phù hợp với các vị trí công việc
- Lưu trữ và quản lý lịch sử phân tích CV

## Yêu cầu hệ thống

- .NET 7.0 SDK hoặc mới hơn
- SQL Server 2019 hoặc mới hơn
- Visual Studio 2022 hoặc mới hơn
- Azure subscription (để sử dụng các dịch vụ AI)

## Cài đặt

1. Clone repository:
```bash
git clone [repository-url]
cd AI_CV_Analyze
```

2. Cài đặt các package NuGet cần thiết:
```bash
dotnet restore
```

3. Cấu hình connection string trong `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=CV_Analysis;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

4. Cấu hình các dịch vụ Azure AI trong `appsettings.json`:
```json
{
  "Azure": {
    "ComputerVision": {
      "SubscriptionKey": "YOUR_KEY",
      "Endpoint": "YOUR_ENDPOINT"
    },
    "FormRecognizer": {
      "Endpoint": "YOUR_ENDPOINT",
      "Key": "YOUR_KEY"
    },
    "TextAnalytics": {
      "Endpoint": "YOUR_ENDPOINT",
      "Key": "YOUR_KEY"
    }
  },
  "AzureAI": {
    "OpenAIEndpoint": "YOUR_ENDPOINT",
    "OpenAIKey": "YOUR_KEY",
    "OpenAIDeploymentName": "YOUR_DEPLOYMENT"
  }
}
```

5. Tạo database và migration:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Chạy ứng dụng

1. Chạy ứng dụng trong Visual Studio:
   - Mở solution trong Visual Studio
   - Nhấn F5 hoặc chọn "Start Debugging"

2. Hoặc chạy bằng command line:
```bash
dotnet run
```

3. Truy cập ứng dụng tại: `https://localhost:5001` hoặc `http://localhost:5000`

## Cấu trúc dự án

```
AI_CV_Analyze/
├── Controllers/         # MVC Controllers
├── Models/             # Data models
├── Views/              # Razor views
├── Data/               # Database context và migrations
├── Services/           # Business logic và services
├── wwwroot/           # Static files
└── Program.cs         # Application entry point
```

## Các dịch vụ Azure AI được sử dụng

1. **Azure Computer Vision**
   - Phân tích hình ảnh CV
   - Trích xuất văn bản từ hình ảnh

2. **Azure Form Recognizer**
   - Trích xuất thông tin có cấu trúc từ CV
   - Nhận dạng các trường thông tin

3. **Azure Text Analytics**
   - Phân tích ngôn ngữ
   - Trích xuất từ khóa
   - Phân tích cảm xúc

4. **Azure OpenAI**
   - Phân tích nội dung CV
   - Đánh giá kỹ năng và kinh nghiệm
   - Đề xuất vị trí phù hợp


