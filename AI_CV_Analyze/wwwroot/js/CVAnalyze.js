// DOM Elements
const dropzone = document.getElementById('dropzone');
const fileInput = document.getElementById('cvFile');
const uploadInstructions = document.getElementById('upload-instructions');
const analyzeSection = document.getElementById('analyze-section');
const fileName = document.getElementById('file-name');
const fileSize = document.getElementById('file-size');
const removeFileBtn = document.getElementById('remove-file');
const errorMessage = document.getElementById('error-message');
const errorText = document.getElementById('error-text');
const form = document.getElementById('cvForm');

let analyzeModal;
let formSubmitting = false;
let abortController;
const cancelBtn = document.getElementById('cancelAnalyzeBtn');

// Prevent default drag behaviors
['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
    dropzone.addEventListener(eventName, preventDefaults, false);
    document.body.addEventListener(eventName, preventDefaults, false);
});

function preventDefaults(e) {
    e.preventDefault();
    e.stopPropagation();
}

// Highlight drop zone when item is dragged over it
['dragenter', 'dragover'].forEach(eventName => {
    dropzone.addEventListener(eventName, highlight, false);
});

['dragleave', 'drop'].forEach(eventName => {
    dropzone.addEventListener(eventName, unhighlight, false);
});

function highlight() {
    dropzone.classList.add('active');
}

function unhighlight() {
    dropzone.classList.remove('active');
}

// Handle dropped files
dropzone.addEventListener('drop', handleDrop, false);

function handleDrop(e) {
    const dt = e.dataTransfer;
    const files = dt.files;
    handleFiles(files);
}

// Handle selected files
fileInput.addEventListener('change', function () {
    handleFiles(this.files);
});

// Handle files
function handleFiles(files) {
    if (files.length > 0) {
        const file = files[0];
        const validTypes = [
            'application/pdf',
            'application/msword',
            'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
            'image/jpeg',
            'image/png'
        ];

        const validExtensions = ['.pdf', '.doc', '.docx', '.jpg', '.jpeg', '.png'];
        const fileExtension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();

        if (!validTypes.includes(file.type) && !validExtensions.includes(fileExtension)) {
            showError('Chỉ hỗ trợ định dạng PDF, DOC, DOCX, JPG hoặc PNG');
            return;
        }

        if (file.size > 5 * 1024 * 1024) {
            showError('Kích thước file tối đa là 5MB. Vui lòng chọn file nhỏ hơn.');
            return;
        }

        hideError();
        displayFileInfo(file);
        fileInput.files = files; // Set files to input for form submission
    }
}

// Display file info and show analyze button
function displayFileInfo(file) {
    fileName.textContent = file.name;
    fileSize.textContent = formatFileSize(file.size);

    // Hide upload instructions and show analyze section
    uploadInstructions.style.display = 'none';
    analyzeSection.style.display = 'block';
}

// Remove file and reset to upload state
removeFileBtn.addEventListener('click', function () {
    fileInput.value = '';
    uploadInstructions.style.display = 'block';
    analyzeSection.style.display = 'none';
    hideError();
});

// Format file size
function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

// Show error message
function showError(message) {
    errorText.textContent = message;
    errorMessage.classList.remove('hidden');
}

// Hide error message
function hideError() {
    errorMessage.classList.add('hidden');
}

// Form submission
form.addEventListener('submit', function (e) {
    if (formSubmitting) return; // Đã submit, không submit lại
    e.preventDefault();
    // Hiển thị loading modal (không dùng bootstrap.Modal)
    const loadingModal = document.getElementById('loadingModal');
    if (loadingModal) loadingModal.classList.remove('hidden');
    formSubmitting = true;

    // Tạo AbortController cho fetch (nếu dùng fetch)
    abortController = new AbortController();

    // Gửi form bằng AJAX để có thể hủy
    const formData = new FormData(form);
    fetch(form.action, {
        method: 'POST',
        body: formData,
        signal: abortController.signal
    })
        .then(response => response.text())
        .then(html => {
            // Chuyển sang trang kết quả (replace body)
            document.open();
            document.write(html);
            document.close();
        })
        .catch(err => {
            if (err.name === 'AbortError') {
                // Đã hủy
                if (loadingModal) loadingModal.classList.add('hidden');
                formSubmitting = false;
            } else {
                alert('Đã xảy ra lỗi khi phân tích CV!');
                if (loadingModal) loadingModal.classList.add('hidden');
                formSubmitting = false;
            }
        });
});

cancelBtn.addEventListener('click', function () {
    if (abortController) abortController.abort();
    const loadingModal = document.getElementById('loadingModal');
    if (loadingModal) loadingModal.classList.add('hidden');
    formSubmitting = false;
});

// Toggle user dropdown menu
document.addEventListener('DOMContentLoaded', function () {
    const userMenuButton = document.getElementById('user-menu-button');
    const userMenuDropdown = document.getElementById('user-menu-dropdown');
    if (userMenuButton && userMenuDropdown) {
        userMenuButton.addEventListener('click', function (e) {
            e.stopPropagation();
            userMenuDropdown.classList.toggle('hidden');
        });
        // Hide dropdown when clicking outside
        document.addEventListener('click', function (e) {
            if (!userMenuDropdown.classList.contains('hidden')) {
                userMenuDropdown.classList.add('hidden');
            }
        });
        // Prevent dropdown from closing when clicking inside
        userMenuDropdown.addEventListener('click', function (e) {
            e.stopPropagation();
        });
    }
});