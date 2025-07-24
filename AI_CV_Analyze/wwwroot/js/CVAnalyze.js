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
const loadingModal = document.getElementById('loadingModal');
const cancelBtn = document.getElementById('cancelAnalyzeBtn');
const progressBar = document.getElementById('progressBar');

let formSubmitting = false;
let abortController;

// Initialize the form
document.addEventListener('DOMContentLoaded', function () {
    // Set initial states
    uploadInstructions.style.display = 'block';
    analyzeSection.style.display = 'none';

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
                showError('Only PDF, DOC, DOCX, JPG or PNG files are supported');
                return;
            }

            if (file.size > 5 * 1024 * 1024) {
                showError('File size exceeds 5MB limit. Please choose a smaller file.');
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
        if (formSubmitting) return;
        e.preventDefault();

        const loadingModal = document.getElementById('loadingModal');
        if (loadingModal) {
            loadingModal.classList.remove('hidden');
            loadingModal.style.animation = 'fadeIn 0.3s ease-out';
        }

        formSubmitting = true;
        abortController = new AbortController();

        let progress = 0;
        const progressInterval = setInterval(() => {
            progress += Math.random() * 10;
            if (progress > 90) progress = 90;
            progressBar.style.width = `${progress}%`;
        }, 300);

        const formData = new FormData(form);

        fetch(form.action, {
            method: 'POST',
            body: formData,
            signal: abortController.signal
        })
            .then(response => {
                clearInterval(progressInterval);
                progressBar.style.width = '100%';
                return response.text();
            })
            .then(html => {
                if (loadingModal) {
                    loadingModal.querySelector('#loadingSpinner').innerHTML = `
                    <div class="success-checkmark">
                        <div class="check-icon">
                            <span class="icon-line line-tip"></span>
                            <span class="icon-line line-long"></span>
                            <div class="icon-circle"></div>
                            <div class="icon-fix"></div>
                        </div>
                    </div>
                `;
                }

                setTimeout(() => {
                    // Xóa trạng thái active
                    if (dropzone) dropzone.classList.remove('active');
                    // Xóa các sự kiện cũ
                    dropzone.removeEventListener('dragenter', highlight);
                    dropzone.removeEventListener('dragover', highlight);
                    dropzone.removeEventListener('dragleave', unhighlight);
                    dropzone.removeEventListener('drop', unhighlight);
                    dropzone.removeEventListener('drop', handleDrop);

                    document.open();
                    document.write(html);
                    document.close();

                    // Cập nhật URL
                    const newUrl = '/CVAnalysis/AnalysisResult'; // URL thực tế
                    window.history.pushState({ path: newUrl }, '', newUrl);

                    // Tái khởi tạo sự kiện nếu cần
                    const newDropzone = document.getElementById('dropzone');
                    if (newDropzone) {
                        ['dragenter', 'dragover'].forEach(eventName => {
                            newDropzone.addEventListener(eventName, highlight, false);
                        });
                        ['dragleave', 'drop'].forEach(eventName => {
                            newDropzone.addEventListener(eventName, unhighlight, false);
                        });
                        newDropzone.addEventListener('drop', handleDrop, false);
                    }
                }, 1000);
            })
            .catch(err => {
                clearInterval(progressInterval);
                if (err.name === 'AbortError') {
                    if (loadingModal) loadingModal.classList.add('hidden');
                    formSubmitting = false;
                } else {
                    showError('An error occurred during CV analysis. Please try again.');
                    if (loadingModal) loadingModal.classList.add('hidden');
                    formSubmitting = false;
                }
            });
    });

    cancelBtn.addEventListener('click', function () {
        if (abortController) abortController.abort();
        loadingModal.classList.add('hidden');
        formSubmitting = false;
    });
});