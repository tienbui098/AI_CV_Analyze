# Cấu hình Azure OpenAI Only

## Thay đổi đã thực hiện

✅ **Đã loại bỏ các AI khác:**
- OpenAI API thông thường
- Google Gemini
- DeepSeek
- Claude

✅ **Chỉ giữ lại Azure OpenAI**

## Cấu hình hiện tại

### appsettings.json
```json
"AzureAI": {
    "OpenAIEndpoint": "https://your-resource-name.openai.azure.com/openai/deployments/your-deployment-name/chat/completions?api-version=2024-02-15-preview",
    "OpenAIKey": "your-azure-openai-key",
    "OpenAIDeploymentName": "your-deployment-name"
}
```

### Code changes
- Loại bỏ fallback sang OpenAI API thông thường
- Chỉ sử dụng Azure OpenAI
- Thêm validation cho Azure OpenAI configuration

## Lợi ích

1. **Đơn giản hóa**: Chỉ cần quản lý một dịch vụ AI
2. **Bảo mật**: Azure OpenAI có bảo mật tốt hơn
3. **Tích hợp**: Tích hợp tốt với các dịch vụ Azure khác
4. **Chi phí**: Dễ dàng quản lý chi phí qua Azure portal

## Cách lấy thông tin Azure OpenAI

### 1. Azure Portal
1. Truy cập [portal.azure.com](https://portal.azure.com)
2. Tìm resource Azure OpenAI của bạn

### 2. Keys and Endpoint
1. Chọn "Keys and Endpoint"
2. Copy API Key và Endpoint URL

### 3. Deployments
1. Chọn "Deployments"
2. Copy tên deployment

### 4. Cập nhật appsettings.json
```json
"AzureAI": {
    "OpenAIEndpoint": "https://your-resource.openai.azure.com/openai/deployments/your-deployment/chat/completions?api-version=2024-02-15-preview",
    "OpenAIKey": "your-actual-key",
    "OpenAIDeploymentName": "your-deployment-name"
}
```

## Troubleshooting

### Lỗi 401 Unauthorized
- Kiểm tra API key có đúng không
- Kiểm tra endpoint URL có đúng format không
- Đảm bảo subscription còn active

### Lỗi 404 Not Found
- Kiểm tra deployment name có đúng không
- Kiểm tra API version có phù hợp không

### Lỗi cấu hình
- Đảm bảo đủ 3 thông tin: Endpoint, Key, DeploymentName
- Kiểm tra format JSON có đúng không

## Test

Sau khi cấu hình:
1. Restart ứng dụng
2. Upload CV để test chức năng đề xuất chỉnh sửa
3. Kiểm tra console log để debug nếu cần 