document.addEventListener('DOMContentLoaded', function () {
    // Mobile menu toggle
    const mobileMenuButton = document.getElementById('mobile-menu-button');
    const mobileMenu = document.getElementById('mobile-menu');

    if (mobileMenuButton && mobileMenu) {
        mobileMenuButton.addEventListener('click', function () {
            const expanded = this.getAttribute('aria-expanded') === 'true';
            this.setAttribute('aria-expanded', !expanded);
            mobileMenu.classList.toggle('hidden');
        });
    }

    // User dropdown toggle
    const userMenuButton = document.getElementById('user-menu-button');
    const userMenuDropdown = document.getElementById('user-menu-dropdown');

    if (userMenuButton && userMenuDropdown) {
        userMenuButton.addEventListener('click', function () {
            const expanded = this.getAttribute('aria-expanded') === 'true';
            this.setAttribute('aria-expanded', !expanded);
            userMenuDropdown.classList.toggle('hidden');
        });
    }

    // Hide validation summary if no errors
    const validationSummary = document.querySelector('.validation-summary-errors');
    if (validationSummary) {
        const errorList = validationSummary.querySelector('ul');
        if (!errorList || errorList.children.length === 0) {
            validationSummary.style.display = 'none';
        } else {
            // Add icon to each error message
            const errors = errorList.querySelectorAll('li');
            errors.forEach(error => {
                const icon = document.createElement('i');
                icon.className = 'fas fa-exclamation-circle';
                error.prepend(icon);
            });
        }
    }

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
    const cancelBtn = document.getElementById('cancelAnalyzeBtn');
    let isSubmitting = false;

    // Show modal on submit
    const registerForm = document.getElementById('registerForm');
    if (registerForm) {
        registerForm.addEventListener('submit', function (e) {
            // Kiểm tra hợp lệ phía client
            if (!$(registerForm).valid()) {
                isSubmitting = false;
                return; // Không show modal nếu form không hợp lệ
            }
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

// Hàm này sẽ được Razor gọi khi đăng ký thành công
window.showRegisterSuccessModal = function() {
    const loadingModal = document.getElementById('loadingModal');
    const loadingText = document.getElementById('loadingText');
    const loadingSpinner = document.getElementById('loadingSpinner');
    if (loadingModal && loadingText && loadingSpinner) {
        loadingModal.classList.remove('hidden');
        loadingSpinner.style.display = 'none';
        loadingText.innerHTML = 'Registration successful! Redirecting...';
        setTimeout(function() {
            window.location.href = '/Auth/Login';
        }, 2000);
    }
};