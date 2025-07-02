document.addEventListener('DOMContentLoaded', function () {
    // Toggle password visibility
    document.querySelectorAll('.toggle-password').forEach(function(btn) {
        btn.addEventListener('click', function() {
            const input = this.parentNode.querySelector('input');
            if (!input) return;
            const type = input.getAttribute('type') === 'password' ? 'text' : 'password';
            input.setAttribute('type', type);
            const icon = this.querySelector('i');
            icon.className = type === 'password' ? 'fas fa-eye' : 'fas fa-eye-slash';
        });
    });

    // Modal logic
    const loadingModal = document.getElementById('loadingModal');
    const loadingText = document.getElementById('loadingText');
    const loadingSpinner = document.getElementById('loadingSpinner');
    const cancelBtn = document.getElementById('cancelLoginBtn');
    let isSubmitting = false;

    // Show modal on submit
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', function (e) {
            if (isSubmitting) return;
            isSubmitting = true;
            if (loadingModal) loadingModal.classList.remove('hidden');
        });
    }

    // Cancel button
    if (cancelBtn) {
        cancelBtn.addEventListener('click', function () {
            if (loadingModal) loadingModal.classList.add('hidden');
            isSubmitting = false;
        });
    }
});

// Hàm này sẽ được Razor gọi khi đăng nhập thành công
window.showLoginSuccessModal = function() {
    const loadingModal = document.getElementById('loadingModal');
    const loadingText = document.getElementById('loadingText');
    const loadingSpinner = document.getElementById('loadingSpinner');
    if (loadingModal && loadingText && loadingSpinner) {
        loadingModal.classList.remove('hidden');
        loadingSpinner.style.display = 'none';
        loadingText.innerHTML = 'Đăng nhập thành công! Đang chuyển hướng...';
        setTimeout(function() {
            window.location.href = '/';
        }, 2000);
    }
};
