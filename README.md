# AI CV Analyze

**AI CV Analyze** là hệ thống phân tích CV tự động sử dụng các dịch vụ AI của Azure (Computer Vision, Form Recognizer, Text Analytics, OpenAI GPT-4) để đánh giá, trích xuất thông tin và đề xuất vị trí công việc phù hợp cho ứng viên.

## Tính năng nổi bật

- Phân tích hình ảnh CV (PDF/JPG/PNG) bằng Azure Computer Vision
- Trích xuất thông tin có cấu trúc từ CV với Azure Form Recognizer
- Phân tích ngôn ngữ, từ khóa, cảm xúc bằng Azure Text Analytics
- Đánh giá nội dung, kỹ năng, kinh nghiệm với Azure OpenAI GPT-4
- Đề xuất vị trí công việc phù hợp dựa trên nội dung CV
- Lưu trữ lịch sử phân tích, hỗ trợ quản lý người dùng, quên mật khẩu qua email

## Yêu cầu hệ thống

- .NET 7.0 SDK trở lên
- SQL Server 2019 trở lên
- Visual Studio 2022 (hoặc dùng CLI .NET)
- Azure Subscription (để lấy API Key các dịch vụ AI)
- Gmail (nếu muốn dùng chức năng gửi OTP quên mật khẩu)

## Hướng dẫn cài đặt & chạy dự án

### 1. Clone source code

```bash
git clone [repository-url]
cd AI_CV_Analyze
```

### 2. Cài đặt package

```bash
dotnet restore
```

### 3. Cấu hình kết nối Database & Azure AI

- Tạo file `appsettings.Development.json` (hoặc sửa `appsettings.json`)
- Thêm các thông tin sau (thay YOUR_* bằng thông tin thực tế):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=CV_Analysis;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Azure": {
    "ComputerVision": {
      "SubscriptionKey": "YOUR_COMPUTER_VISION_KEY",
      "Endpoint": "YOUR_COMPUTER_VISION_ENDPOINT"
    },
    "FormRecognizer": {
      "Endpoint": "YOUR_FORM_RECOGNIZER_ENDPOINT",
      "Key": "YOUR_FORM_RECOGNIZER_KEY"
    },
    "TextAnalytics": {
      "Endpoint": "YOUR_TEXT_ANALYTICS_ENDPOINT",
      "Key": "YOUR_TEXT_ANALYTICS_KEY"
    }
  },
  "AzureAI": {
    "OpenAIEndpoint": "YOUR_OPENAI_ENDPOINT",
    "OpenAIKey": "YOUR_OPENAI_KEY",
    "OpenAIDeploymentName": "YOUR_OPENAI_DEPLOYMENT"
  }
}
```

> **Tham khảo chi tiết cấu hình trong file [`README_Configuration.md`](AI_CV_Analyze/README_Configuration.md)**

### 4. Tạo database và migration

```bash
dotnet ef database update
```

### 5. (Tùy chọn) Cấu hình gửi email OTP qua Gmail

- Cài package MailKit:
  ```bash
  dotnet add package MailKit
  ```
- Thêm vào `appsettings.Development.json`:
  ```json
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "Username": "your_gmail@gmail.com",
    "Password": "your_app_password_16_ky_tu",
    "From": "your_gmail@gmail.com"
  }
  ```
- Xem hướng dẫn lấy App Password trong README_Configuration.md

- Đăng ký Service và Session trong `Program.cs`:
  ```csharp
  builder.Services.AddScoped<AI_CV_Analyze.Services.Interfaces.IEmailSender, AI_CV_Analyze.Services.Implementation.EmailSender>();
  builder.Services.AddSession(options =>
  {
      options.IdleTimeout = TimeSpan.FromMinutes(10);
      options.Cookie.HttpOnly = true;
      options.Cookie.IsEssential = true;
  });
  ...
  app.UseSession();
  ```

- Sử dụng chức năng quên mật khẩu:
  - Tại trang đăng nhập, nhấn "Quên mật khẩu?"
  - Nhập email, nhận OTP qua Gmail
  - Nhập OTP và mật khẩu mới để đặt lại mật khẩu

### 6. Chạy ứng dụng

- Trong Visual Studio: Mở solution và nhấn F5
- Hoặc dùng CLI:
  ```bash
  dotnet run
  ```
- Truy cập: http://localhost:5000 hoặc https://localhost:5001

## Cấu trúc thư mục chính

```
AI_CV_Analyze/
├── Controllers/         # Controller MVC
├── Models/              # Data models
├── Views/               # Razor views
├── Data/                # DbContext & Migrations
├── Services/            # Business logic & services
├── wwwroot/             # Static files (css, js, img)
└── Program.cs           # Entry point
```

## Lưu ý bảo mật

- **Không commit API Key lên Git**. Sử dụng User Secrets hoặc Environment Variables khi deploy production.
- Đảm bảo endpoint, deployment name, API version đúng với Azure portal.

## Troubleshooting

- Xem chi tiết các lỗi thường gặp và cách xử lý trong [`README_Configuration.md`](AI_CV_Analyze/README_Configuration.md)

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


