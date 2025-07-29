// ShowFinalCV.js - JavaScript functionality for the Final CV page

document.addEventListener('DOMContentLoaded', function() {
    console.log('ShowFinalCV.js loaded successfully');

    // ================ DOM ELEMENTS ================
    const copyCVBtn = document.getElementById('copyCVBtn');
    const downloadCVBtn = document.getElementById('downloadCVBtn');
    const printCVBtn = document.getElementById('printCVBtn');
    const shareCVBtn = document.getElementById('shareCVBtn');
    const cvContent = document.getElementById('cvContent');
    const shareModal = document.getElementById('shareModal');
    const closeShareModal = document.getElementById('closeShareModal');
    const copyShareLink = document.getElementById('copyShareLink');
    const shareLink = document.getElementById('shareLink');
    const publishForm = document.getElementById('publishForm');

    // ================ COPY CV FUNCTIONALITY ================
    function initCopyCV() {
        if (copyCVBtn && cvContent) {
            copyCVBtn.addEventListener('click', function() {
                const text = cvContent.innerText || cvContent.textContent;
                
                navigator.clipboard.writeText(text).then(() => {
                    showCopySuccess(copyCVBtn, 'Copied!');
                }).catch(err => {
                    console.error('Failed to copy: ', err);
                    showCopyError(copyCVBtn);
                });
            });
        }
    }

    function showCopySuccess(button, message) {
        const originalText = button.innerHTML;
        const originalClasses = button.className;
        
        button.innerHTML = `<i class="fas fa-check mr-1"></i> ${message}`;
        button.classList.remove('bg-blue-600', 'hover:bg-blue-700');
        button.classList.add('bg-green-600', 'hover:bg-green-700');
        
        setTimeout(() => {
            button.innerHTML = originalText;
            button.className = originalClasses;
        }, 2000);
    }

    function showCopyError(button) {
        const originalText = button.innerHTML;
        const originalClasses = button.className;
        
        button.innerHTML = `<i class="fas fa-times mr-1"></i> Failed`;
        button.classList.remove('bg-blue-600', 'hover:bg-blue-700');
        button.classList.add('bg-red-600', 'hover:bg-red-700');
        
        setTimeout(() => {
            button.innerHTML = originalText;
            button.className = originalClasses;
        }, 2000);
    }

    // ================ DOWNLOAD CV FUNCTIONALITY ================
    function initDownloadCV() {
        if (downloadCVBtn && cvContent) {
            downloadCVBtn.addEventListener('click', function() {
                const text = cvContent.innerText || cvContent.textContent;
                const timestamp = new Date().toISOString().slice(0, 10);
                const filename = `my-cv-${timestamp}.txt`;
                
                downloadTextFile(text, filename);
            });
        }
    }

    function downloadTextFile(content, filename) {
        const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        
        link.href = url;
        link.download = filename;
        link.style.display = 'none';
        
        document.body.appendChild(link);
        link.click();
        
        // Cleanup
        window.URL.revokeObjectURL(url);
        document.body.removeChild(link);
        
        // Show success message
        showToast('CV downloaded successfully!', 'success');
    }

    // ================ PRINT CV FUNCTIONALITY ================
    function initPrintCV() {
        if (printCVBtn) {
            printCVBtn.addEventListener('click', function() {
                // Add print-specific styles
                addPrintStyles();
                
                // Print the page
                window.print();
                
                // Remove print styles after printing
                setTimeout(() => {
                    removePrintStyles();
                }, 1000);
            });
        }
    }

    function addPrintStyles() {
        const style = document.createElement('style');
        style.id = 'print-styles';
        style.textContent = `
            @media print {
                body * {
                    visibility: hidden;
                }
                #cvContent, #cvContent * {
                    visibility: visible;
                }
                #cvContent {
                    position: absolute;
                    left: 0;
                    top: 0;
                    width: 100%;
                    height: 100%;
                    margin: 0;
                    padding: 20px;
                    background: white !important;
                    border: none !important;
                    box-shadow: none !important;
                }
            }
        `;
        document.head.appendChild(style);
    }

    function removePrintStyles() {
        const printStyles = document.getElementById('print-styles');
        if (printStyles) {
            printStyles.remove();
        }
    }

    // ================ SHARE CV FUNCTIONALITY ================
    function initShareCV() {
        if (shareCVBtn && shareModal) {
            shareCVBtn.addEventListener('click', function() {
                showShareModal();
            });
        }
        
        if (closeShareModal && shareModal) {
            closeShareModal.addEventListener('click', function() {
                hideShareModal();
            });
        }
        
        // Close modal when clicking outside
        if (shareModal) {
            shareModal.addEventListener('click', function(e) {
                if (e.target === shareModal) {
                    hideShareModal();
                }
            });
        }
    }

    function showShareModal() {
        shareModal.classList.remove('hidden');
        shareModal.style.display = 'flex';
        
        // Add animation
        setTimeout(() => {
            shareModal.classList.add('animate-fade-in');
        }, 10);
    }

    function hideShareModal() {
        shareModal.classList.remove('animate-fade-in');
        shareModal.classList.add('hidden');
        shareModal.style.display = 'none';
    }

    // ================ COPY SHARE LINK FUNCTIONALITY ================
    function initCopyShareLink() {
        if (copyShareLink && shareLink) {
            copyShareLink.addEventListener('click', function() {
                copyShareLinkToClipboard();
            });
        }
    }

    function copyShareLinkToClipboard() {
        shareLink.select();
        shareLink.setSelectionRange(0, 99999); // For mobile devices
        
        try {
            document.execCommand('copy');
            showCopySuccess(copyShareLink, 'Link copied!');
            showToast('Share link copied to clipboard!', 'success');
        } catch (err) {
            console.error('Failed to copy link: ', err);
            showCopyError(copyShareLink);
        }
    }

    // ================ PUBLISH FORM FUNCTIONALITY ================
    function initPublishForm() {
        if (publishForm) {
            publishForm.addEventListener('submit', function(e) {
                if (!confirmPublish()) {
                    e.preventDefault();
                }
            });
        }
    }

    function confirmPublish() {
        return confirm('Are you sure you want to publish your CV? This will make it available to potential employers.');
    }

    // ================ TOAST NOTIFICATIONS ================
    function showToast(message, type = 'info') {
        const toast = createToastElement(message, type);
        document.body.appendChild(toast);
        
        // Animate in
        setTimeout(() => {
            toast.classList.add('show');
        }, 10);
        
        // Auto remove
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            }, 300);
        }, 3000);
    }

    function createToastElement(message, type) {
        const toast = document.createElement('div');
        toast.className = `toast-notification toast-${type}`;
        
        const icon = getToastIcon(type);
        const color = getToastColor(type);
        
        toast.innerHTML = `
            <div class="toast-content ${color}">
                <i class="${icon} mr-2"></i>
                <span>${message}</span>
                <button class="toast-close" onclick="this.parentElement.parentElement.remove()">
                    <i class="fas fa-times"></i>
                </button>
            </div>
        `;
        
        return toast;
    }

    function getToastIcon(type) {
        const icons = {
            'success': 'fas fa-check-circle',
            'error': 'fas fa-exclamation-circle',
            'warning': 'fas fa-exclamation-triangle',
            'info': 'fas fa-info-circle'
        };
        return icons[type] || icons.info;
    }

    function getToastColor(type) {
        const colors = {
            'success': 'bg-green-500',
            'error': 'bg-red-500',
            'warning': 'bg-yellow-500',
            'info': 'bg-blue-500'
        };
        return colors[type] || colors.info;
    }

    // ================ SOCIAL SHARE FUNCTIONALITY ================
    function initSocialShare() {
        const socialButtons = document.querySelectorAll('.social-share-btn');
        
        socialButtons.forEach(button => {
            button.addEventListener('click', function(e) {
                e.preventDefault();
                const platform = this.getAttribute('data-platform');
                const url = encodeURIComponent(window.location.href);
                const text = encodeURIComponent('Check out my professional CV!');
                
                shareToSocial(platform, url, text);
            });
        });
    }

    function shareToSocial(platform, url, text) {
        const shareUrls = {
            'linkedin': `https://www.linkedin.com/sharing/share-offsite/?url=${url}`,
            'facebook': `https://www.facebook.com/sharer/sharer.php?u=${url}`,
            'twitter': `https://twitter.com/intent/tweet?url=${url}&text=${text}`,
            'whatsapp': `https://wa.me/?text=${text}%20${url}`
        };
        
        const shareUrl = shareUrls[platform];
        if (shareUrl) {
            window.open(shareUrl, '_blank', 'width=600,height=400');
        }
    }

    // ================ ANIMATIONS ================
    function initAnimations() {
        // Animate elements when they come into view
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        };
        
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('animate-in');
                }
            });
        }, observerOptions);
        
        // Observe all animated elements
        document.querySelectorAll('.animate-fade-in-up').forEach(el => {
            observer.observe(el);
        });
    }

    // ================ UTILITY FUNCTIONS ================
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    function throttle(func, limit) {
        let inThrottle;
        return function() {
            const args = arguments;
            const context = this;
            if (!inThrottle) {
                func.apply(context, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    }

    // ================ ERROR HANDLING ================
    function handleError(error, context) {
        console.error(`Error in ${context}:`, error);
        showToast(`An error occurred: ${error.message}`, 'error');
    }

    // Global error handler
    window.addEventListener('error', function(e) {
        handleError(e.error, 'Global');
    });

    // ================ INITIALIZATION ================
    function init() {
        try {
            initCopyCV();
            initDownloadCV();
            initPrintCV();
            initShareCV();
            initCopyShareLink();
            initPublishForm();
            initSocialShare();
            initAnimations();
            
            console.log('ShowFinalCV.js initialized successfully');
        } catch (error) {
            handleError(error, 'Initialization');
        }
    }

    // ================ EXPORT FUNCTIONS FOR GLOBAL USE ================
    window.ShowFinalCV = {
        showToast,
        downloadTextFile,
        shareToSocial,
        showShareModal,
        hideShareModal
    };

    // Initialize when DOM is ready
    init();
}); 