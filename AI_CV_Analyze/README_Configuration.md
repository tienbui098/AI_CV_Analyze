# Hướng dẫn cấu hình AI CV Analyze

## Cấu hình Azure OpenAI

### Azure OpenAI (Bắt buộc)
Cấu hình Azure OpenAI trong `appsettings.json`:

```json
"AzureAI": {
    "OpenAIEndpoint": "https://your-resource-name.openai.azure.com/openai/deployments/your-deployment-name/chat/completions?api-version=2024-02-15-preview",
    "OpenAIKey": "your-azure-openai-key",
    "OpenAIDeploymentName": "your-deployment-name"
}
```

## Các dịch vụ Azure khác

### Azure Computer Vision
```json
"Azure": {
    "ComputerVision": {
        "SubscriptionKey": "your-computer-vision-key",
        "Endpoint": "https://your-resource-name.cognitiveservices.azure.com/"
    }
}
```

### Azure Form Recognizer
```json
"Azure": {
    "FormRecognizer": {
        "Endpoint": "https://your-resource-name.cognitiveservices.azure.com/",
        "Key": "your-form-recognizer-key"
    }
}
```

### Azure Text Analytics
```json
"Azure": {
    "TextAnalytics": {
        "Endpoint": "https://your-resource-name.cognitiveservices.azure.com/",
        "Key": "your-text-analytics-key"
    }
}
```

## Cấu hình Job Recommendation (Tùy chọn)

Nếu bạn có Azure Machine Learning service cho job recommendation:

```json
"JobRecommendation": {
    "Endpoint": "https://your-region.services.azureml.net/workspaces/your-workspace-id/services/your-service-id/execute?api-version=2.0&details=true",
    "ApiKey": "your-azure-ml-api-key"
}
```

## Lưu ý quan trọng

1. **API Keys**: Không bao giờ commit API keys vào source code. Sử dụng User Secrets hoặc Environment Variables trong production.

2. **Endpoint URLs**: Đảm bảo endpoint URLs đúng format và region.

3. **Deployment Name**: Với Azure OpenAI, deployment name phải khớp với deployment đã tạo trong Azure portal.

4. **API Version**: Đảm bảo sử dụng API version phù hợp với dịch vụ.

## Troubleshooting

### Lỗi 401 Unauthorized
- Kiểm tra API key có đúng không
- Kiểm tra endpoint URL có đúng không
- Đảm bảo subscription còn active

### Lỗi 404 Not Found
- Kiểm tra deployment name có đúng không
- Kiểm tra endpoint URL có đúng format không

### Lỗi Timeout
- Tăng timeout trong code nếu cần
- Kiểm tra kết nối mạng 