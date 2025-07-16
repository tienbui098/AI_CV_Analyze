# AI_CV_Analyze

## Giới thiệu tổng quát

**AI_CV_Analyze** là một hệ thống web ứng dụng trí tuệ nhân tạo để phân tích, đánh giá và gợi ý nghề nghiệp dựa trên CV (Curriculum Vitae) của người dùng. Ứng dụng hỗ trợ người dùng tải lên CV dưới nhiều định dạng (PDF, DOC, DOCX, JPG, PNG), phân tích nội dung, trích xuất kỹ năng, và đề xuất các ngành nghề phù hợp.

Các tính năng chính:
- Đăng ký, đăng nhập, quên mật khẩu cho người dùng
- Tải lên và phân tích CV tự động
- Trích xuất kỹ năng, kinh nghiệm, học vấn từ CV
- Đưa ra gợi ý ngành nghề phù hợp
- Lưu lịch sử phân tích và kết quả cho từng người dùng

## Công nghệ sử dụng
- **Backend:** ASP.NET Core MVC (.NET 7 trở lên)
- **Frontend:** Razor Pages, HTML, CSS, JavaScript
- **Database:** SQL Server (hoặc SQLite cho phát triển)
- **AI/ML:** Tích hợp các dịch vụ AI để phân tích nội dung CV

## Hướng dẫn cài đặt và setup

### 1. Yêu cầu hệ thống
- .NET 7 SDK hoặc mới hơn: https://dotnet.microsoft.com/download
- SQL Server (hoặc SQLite cho phát triển)
- Node.js (nếu muốn build lại các file JS/CSS frontend)

### 2. Clone dự án
```bash
git clone <repo-url>
cd AI_CV_Analyze
```

### 3. Cấu hình chuỗi kết nối Database
- Mở file `AI_CV_Analyze/appsettings.Development.json`
- Sửa trường `ConnectionStrings:DefaultConnection` cho phù hợp với môi trường của bạn.

Ví dụ:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=AI_CV_Analyze;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

### 4. Chạy migration để tạo database
```bash
cd AI_CV_Analyze
# Nếu chưa cài dotnet-ef:
dotnet tool install --global dotnet-ef
# Chạy migration:
dotnet ef database update
```

### 5. Chạy ứng dụng
```bash
dotnet run --project AI_CV_Analyze/AI_CV_Analyze.csproj
```
- Ứng dụng sẽ chạy tại địa chỉ: http://localhost:5000 hoặc http://localhost:5001

### 6. Truy cập giao diện web
- Mở trình duyệt và truy cập: http://localhost:5000
- Đăng ký tài khoản, đăng nhập và bắt đầu sử dụng các tính năng phân tích CV.

## Một số lưu ý
- Nếu gặp lỗi về database, kiểm tra lại chuỗi kết nối và quyền truy cập SQL Server.
- Để gửi email (quên mật khẩu), cần cấu hình SMTP trong `appsettings.Development.json`.
- Có thể chạy ứng dụng bằng Docker, tham khảo file `Dockerfile` trong thư mục dự án.

## Đóng góp
Mọi đóng góp, báo lỗi hoặc đề xuất tính năng mới đều được hoan nghênh! Vui lòng tạo issue hoặc pull request trên repository này.

---
**AI_CV_Analyze** - Dự án phân tích CV ứng dụng AI hỗ trợ định hướng nghề nghiệp cho người Việt.


