document.addEventListener('DOMContentLoaded', function () {
    // ================ LOADING MODAL ================
    const loadingModal = {
        element: document.getElementById('loadingModal'),
        modalContent: document.querySelector('#loadingModal > div'), // Updated selector to match new markup
        controller: null,

        show: function () {
            if (!this.element) {
                console.error('Loading modal element not found!');
                return;
            }

            this.element.classList.remove('hidden');
            this.element.style.display = 'flex';
            this.element.classList.remove('show');
            if (this.modalContent) this.modalContent.classList.remove('show');

            setTimeout(() => {
                this.element.classList.add('show');
                if (this.modalContent) this.modalContent.classList.add('show');
            }, 50);
        },

        hide: function () {
            if (!this.element) return;

            this.element.classList.remove('show');
            if (this.modalContent) this.modalContent.classList.remove('show');

            setTimeout(() => {
                this.element.style.display = 'none';
                this.element.classList.add('hidden');
            }, 300);
        },

        init: function () {
            const cancelBtn = document.getElementById('cancelAnalyzeBtn');
            if (cancelBtn) {
                cancelBtn.addEventListener('click', () => {
                    if (this.controller) this.controller.abort();
                    this.hide();
                });
            }
        }
    };

    // Hide modal on initial load (in case it was left open)
    loadingModal.hide();
    // Hide modal when navigating back/forward (bfcache)
    window.addEventListener('pageshow', function() {
        loadingModal.hide();
    });

    // ================ STICKY SIDEBAR ================
    const stickySidebar = {
        element: document.querySelector('.sticky-sidebar'),
        mainContent: document.querySelector('.lg\\:col-span-2'),
        footer: document.querySelector('footer'),

        updatePosition: function () {
            if (!this.element) return;

            if (window.innerWidth < 1024) {
                this.element.style.position = 'static';
                this.element.style.height = 'auto';
                return;
            }

            const sidebarRect = this.element.getBoundingClientRect();
            const maxHeight = window.innerHeight - 40;
            this.element.style.maxHeight = `${maxHeight}px`;

            if (this.footer) {
                const footerRect = this.footer.getBoundingClientRect();
                if (sidebarRect.bottom > footerRect.top) {
                    this.element.style.top = `${maxHeight - (sidebarRect.bottom - footerRect.top)}px`;
                } else {
                    this.element.style.top = '20px';
                }
            }
        },

        init: function () {
            if (!this.element) return;

            this.updatePosition();
            const debouncedUpdate = debounce(() => this.updatePosition(), 100);
            window.addEventListener('resize', debouncedUpdate);
            window.addEventListener('scroll', debouncedUpdate);
        }
    };

    // ================ FORM HANDLING ================
    const formHandler = {
        form: null,

        submit: async function (e) {
            e.preventDefault();
            console.log('Form submit intercepted!');
            loadingModal.show();
            loadingModal.controller = new AbortController();

            try {
                const response = await fetch(this.form.action, {
                    method: 'POST',
                    body: new FormData(this.form),
                    signal: loadingModal.controller.signal,
                    headers: {
                        'X-RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    }
                });

                if (response.redirected) {
                    window.location.href = response.url;
                } else if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
            } catch (error) {
                if (error.name !== 'AbortError') {
                    toast.show('Có lỗi xảy ra khi gửi yêu cầu. Vui lòng thử lại.', 'error');
                }
            } finally {
                loadingModal.hide();
            }
        },

        init: function () {
            this.form = document.getElementById('editSuggestionsForm');
            if (this.form) {
                this.form.addEventListener('submit', (e) => this.submit(e));
            } else {
                console.error('Form #editSuggestionsForm not found!');
            }
        }
    };

    // ================ TOAST NOTIFICATIONS ================
    const toast = {
        show: function (message, type = 'info') {
            const toastElement = document.createElement('div');
            toastElement.className = `toast fixed bottom-4 right-4 px-4 py-3 rounded-lg shadow-lg text-white font-medium flex items-center ${this.getColor(type)}`;
            toastElement.innerHTML = `<i class="${this.getIcon(type)} mr-2"></i>${message}`;

            document.body.appendChild(toastElement);
            setTimeout(() => toastElement.classList.add('fade-in'), 10);

            setTimeout(() => {
                toastElement.classList.remove('fade-in');
                setTimeout(() => toastElement.remove(), 300);
            }, 3000);
        },

        getColor: function (type) {
            const colors = {
                'success': 'bg-green-500',
                'error': 'bg-red-500',
                'warning': 'bg-yellow-500',
                'info': 'bg-blue-500'
            };
            return colors[type] || 'bg-blue-500';
        },

        getIcon: function (type) {
            const icons = {
                'success': 'fas fa-check-circle',
                'error': 'fas fa-exclamation-circle',
                'warning': 'fas fa-exclamation-triangle',
                'info': 'fas fa-info-circle'
            };
            return icons[type] || 'fas fa-info-circle';
        }
    };

    // ================ UTILITY FUNCTIONS ================
    function debounce(func, wait) {
        let timeout;
        return (...args) => {
            clearTimeout(timeout);
            timeout = setTimeout(() => func.apply(this, args), wait);
        };
    }

    // ================ INITIALIZATION ================
    loadingModal.init();
    stickySidebar.init();
    formHandler.init();

    // Other initializations...
    document.getElementById('btnBackEditSuggestions')?.addEventListener('click', function (e) {
        e.preventDefault();
        fetch('/CVAnalysis/HasEditSuggestions')
            .then(response => response.json())
            .then(data => {
                if (data.hasSuggestions) {
                    window.location.href = '/CVAnalysis/EditSuggestions';
                } else {
                    toast.show('Bạn chưa sử dụng chức năng đề xuất chỉnh sửa CV', 'warning');
                }
            })
            .catch(() => {
                toast.show('Không thể kiểm tra đề xuất chỉnh sửa', 'error');
            });
    });

    document.getElementById('shareResultBtn')?.addEventListener('click', function () {
        const shareModal = document.getElementById('shareModal');
        shareModal?.classList.remove('hidden');
        setTimeout(() => shareModal?.classList.add('show'), 10);
    });

    document.getElementById('closeShareModal')?.addEventListener('click', function () {
        const shareModal = document.getElementById('shareModal');
        shareModal?.classList.remove('show');
        setTimeout(() => shareModal?.classList.add('hidden'), 300);
    });

    document.getElementById('copyShareLink')?.addEventListener('click', function () {
        const shareLink = document.getElementById('shareLink');
        shareLink?.select();
        document.execCommand('copy');
        toast.show('Đã sao chép liên kết vào clipboard', 'success');
    });
});
