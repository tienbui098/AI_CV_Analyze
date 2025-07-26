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
        // Đã xóa: footer: document.querySelector('footer'),

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

            this.element.style.top = '20px';
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
                } else if (response.ok) {
                    // Nếu response thành công, reload trang để hiển thị suggestions
                    window.location.reload();
                } else {
                    throw new Error('Network response was not ok');
                }
            } catch (error) {
                if (error.name !== 'AbortError') {
                    toast.show('An error occurred while sending the request. Please try again.', 'error');
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

    // ================ CV SCORING HANDLER ================
    const btnScoreCV = document.getElementById('btnScoreCV');
    if (btnScoreCV) {
        btnScoreCV.addEventListener('click', function () {
            const resumeId = btnScoreCV.getAttribute('data-resumeid');
            const loading = document.getElementById('score-loading');
            const error = document.getElementById('score-error');
            const table = document.getElementById('cv-score-table');
            btnScoreCV.style.display = 'none';
            error.style.display = 'none';
            loading.style.display = 'block';
            loadingModal.show();
            // Lấy nội dung CV từ input ẩn nếu có
            const cvContentInput = document.querySelector('input[name="Content"]');
            const cvContent = cvContentInput ? cvContentInput.value : '';
            fetch('/CVAnalysis/ScoreCV', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': document.querySelector('input[name=__RequestVerificationToken]')?.value || ''
                },
                body: 'resumeId=' + encodeURIComponent(resumeId) + '&cvContent=' + encodeURIComponent(cvContent)
            })
            .then(res => res.json())
            .then(data => {
                loading.style.display = 'none';
                loadingModal.hide();
                if (data.success) {
                    table.style.display = 'block';
                    btnScoreCV.style.display = 'none';
                    document.getElementById('score-layout').textContent = data.layout + ' / 10';
                    document.getElementById('score-skill').textContent = data.skill + ' / 10';
                    document.getElementById('score-experience').textContent = data.experience + ' / 10';
                    document.getElementById('score-education').textContent = data.education + ' / 10';
                    document.getElementById('score-keyword').textContent = data.keyword + ' / 10';
                    document.getElementById('score-format').textContent = data.format + ' / 10';
                    document.getElementById('score-total').textContent = data.total + ' / 60';
                } else {
                    btnScoreCV.style.display = 'block';
                    error.style.display = 'block';
                    error.textContent = data.error || 'An error occurred while scoring the CV.';
                }
            })
            .catch(err => {
                loading.style.display = 'none';
                loadingModal.hide();
                btnScoreCV.style.display = 'block';
                error.style.display = 'block';
                error.textContent = 'An error occurred while connecting to the server.';
            });
        });
    }

    // ================ JOB RECOMMENDATION HANDLER ================
    const btnJobRecommend = document.getElementById('btnJobRecommend');
    const loadingText = document.getElementById('loadingText');
    let abortController = null;
    if (btnJobRecommend) {
        btnJobRecommend.addEventListener('click', function () {
            btnJobRecommend.disabled = true;
            btnJobRecommend.textContent = 'Loading...';
            if (loadingText) loadingText.textContent = 'AI system is processing and giving suitable jobs based on your CV. Please wait a moment...';
            loadingModal.show();
            abortController = new AbortController();
            
            // Get skills and work experience from hidden fields (Model data)
            const skills = document.getElementById('cvSkills').value || '';
            const experience = document.getElementById('cvExperience').value || '';
            const project = document.getElementById('cvProject').value || '';
            
            // Combine experience and project (either can be null)
            let workExperience = '';
            if (experience && project) {
                workExperience = experience + '\n\n' + project;
            } else if (experience) {
                workExperience = experience;
            } else if (project) {
                workExperience = project;
            }
            
            fetch('/CVAnalysis/RecommendJobs', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name=__RequestVerificationToken]')?.value || ''
                },
                body: JSON.stringify({ 
                    skills: skills,
                    workExperience: workExperience 
                }),
                signal: abortController.signal
            })
            .then(response => response.json())
            .then(data => {
                let html = '';
                
                if (data && data.recommendedJob && data.matchPercentage) {
                    // New OpenAI-based format
                    html = '<div class="space-y-4">';
                    html += '<p class="text-sm text-gray-600 mb-4">Based on your skills and experience, we recommend:</p>';
                    html += `
                        <div class="job-card p-4 rounded-lg border border-gray-100 hover:border-blue-200 hover:shadow-md transition">
                            <div class="flex items-start">
                                <div class="flex-grow">
                                    <h3 class="font-bold text-gray-900">${data.recommendedJob}</h3>
                                    <div class="flex items-center mt-2">
                                        <div class="w-full bg-gray-200 rounded-full h-2">
                                            <div class="bg-green-500 h-2 rounded-full" style="width: ${data.matchPercentage}%"></div>
                                        </div>
                                    </div>
                                    ${data.missingSkills && data.missingSkills.length > 0 ? `
                                        <div class="mt-3">
                                            <p class="text-xs text-red-600 mb-1">Skills to improve:</p>
                                            <div class="flex flex-wrap gap-1">
                                                ${data.missingSkills.map(skill => `<span class="text-red-600 font-semibold">${skill}</span>`).join('')}
                                            </div>
                                        </div>
                                    ` : ''}
                                </div>
                                <div class="ml-4">
                                    <div class="circular-chart w-12 h-12">
                                        <svg viewBox="0 0 36 36">
                                            <path class="circle-bg" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />
                                            <path class="circle" stroke="#10b981" stroke-dasharray="${data.matchPercentage}, 100" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />
                                            <text x="18" y="20.35" class="percentage">${data.matchPercentage}%</text>
                                        </svg>
                                    </div>
                                </div>
                            </div>
                        </div>
                    `;
                    html += '</div>';
                    html += `<a href="/CVAnalysis/JobPrediction" class="mt-6 w-full flex justify-center items-center px-4 py-3 border border-gray-300 rounded-xl shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition"><i class="fas fa-search mr-2"></i>View more jobs</a>`;
                } else if (data && data.improvementPlan) {
                    // Error or improvement plan message
                    html = `<div class="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
                        <div class="flex items-center">
                            <i class="fas fa-exclamation-triangle text-yellow-500 mr-2"></i>
                            <span class="text-sm text-yellow-700">${data.improvementPlan}</span>
                        </div>
                    </div>`;
                } else {
                    html = '<div class="text-gray-500">No recommendations found.</div>';
                }
                
                document.getElementById('jobRecommendResult').innerHTML = html;
            })
            .catch((err) => {
                if (err.name === 'AbortError') {
                    document.getElementById('jobRecommendResult').innerHTML = '<div class="text-gray-500">Request cancelled.</div>';
                } else {
                    document.getElementById('jobRecommendResult').innerHTML = '<div class="text-red-500">Error fetching recommendations.</div>';
                }
            })
            .finally(() => {
                btnJobRecommend.disabled = false;
                btnJobRecommend.innerHTML = '<i class="fas fa-magic mr-2"></i> Job Recommendation';
                loadingModal.hide();
                if (loadingText) loadingText.innerHTML = '<div class="text-gray-800">Analyzing your CV...</div><div class="text-sm text-gray-500 mt-1 animate-pulse">This may take a few moments</div>';
            });
        });
    }
    // Cancel button for job recommend loading
    const cancelBtn = document.getElementById('cancelAnalyzeBtn');
    if (cancelBtn) {
        cancelBtn.addEventListener('click', function () {
            if (abortController) abortController.abort();
            loadingModal.hide();
            if (btnJobRecommend) {
                btnJobRecommend.disabled = false;
                btnJobRecommend.textContent = 'Job Recommend';
            }
            if (loadingText) loadingText.textContent = 'The AI system is processing and generating edit suggestions. Please wait a moment...';
        });
    }

    document.getElementById('btnBackEditSuggestions')?.addEventListener('click', function (e) {
        e.preventDefault();
        fetch('/CVAnalysis/HasEditSuggestions')
            .then(response => response.json())
            .then(data => {
                if (data.hasSuggestions) {
                    window.location.href = '/CVAnalysis/EditSuggestions';
                } else {
                    toast.show('You have not used the CV editing suggestion function', 'warning');
                }
            })
            .catch(() => {
                toast.show('Unable to check edit suggestions', 'error');
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
        toast.show('Link copied to clipboard', 'success');
    });

    // ================ COPY SUGGESTIONS CARD ================
    document.querySelectorAll('.copy-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const suggestionText = btn.closest('.suggestion-card').querySelector('.ai-suggestion-text');
            if (suggestionText) {
                const text = suggestionText.innerText || suggestionText.textContent;
                navigator.clipboard.writeText(text).then(() => {
                    toast.show('Copied suggestions to clipboard', 'success');
                });
            }
        });
    });

    // ================ CONTENT CARD MODAL HANDLING ================
    const contentDetailModal = {
        element: document.getElementById('contentDetailModal'),
        title: document.getElementById('modalTitle'),
        content: document.getElementById('modalContent'),
        closeBtn: document.getElementById('closeContentModal'),

        show: function (title, content) {
            this.title.textContent = title;
            this.content.innerHTML = content;
            this.element.classList.remove('hidden');
            this.element.style.display = 'flex';
            setTimeout(() => this.element.classList.add('show'), 10);
        },

        hide: function () {
            this.element.classList.remove('show');
            setTimeout(() => {
                this.element.style.display = 'none';
                this.element.classList.add('hidden');
            }, 300);
        },

        init: function () {
            if (this.closeBtn) {
                this.closeBtn.addEventListener('click', () => this.hide());
            }
            
            // Close modal when clicking outside
            this.element.addEventListener('click', (e) => {
                if (e.target === this.element) {
                    this.hide();
                }
            });

            // Close modal with Escape key
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Escape' && !this.element.classList.contains('hidden')) {
                    this.hide();
                }
            });
        }
    };

    // Initialize content detail modal
    contentDetailModal.init();

    // Handle content card clicks
    document.querySelectorAll('.content-card').forEach(card => {
        card.addEventListener('click', function (e) {
            // Don't trigger if clicking on the show-more button
            if (e.target.closest('.show-more-btn')) {
                e.stopPropagation();
                return;
            }

            const fullContent = this.getAttribute('data-full-content');
            const title = this.getAttribute('data-title');
            
            if (fullContent && title) {
                contentDetailModal.show(title, fullContent);
            }
        });
    });

    // Handle show-more button clicks
    document.querySelectorAll('.show-more-btn').forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            const card = this.closest('.content-card');
            const fullContent = card.getAttribute('data-full-content');
            const title = card.getAttribute('data-title');
            
            if (fullContent && title) {
                contentDetailModal.show(title, fullContent);
            }
        });
    });

    // ================ ENHANCED LOADING MODAL WITH PROGRESS ================
    let progressInterval = null;
    let progress = 0;
    const progressBar = document.getElementById('progressBar');
    // Patch loadingModal.show/hide for progress
    const originalShow = loadingModal.show.bind(loadingModal);
    const originalHide = loadingModal.hide.bind(loadingModal);
    loadingModal.show = function () {
        if (progressBar) progressBar.style.width = '0%';
        progress = 0;
        if (progressBar) progressBar.classList.add('animate-progress');
        if (loadingText) loadingText.innerHTML = `<div class="text-gray-800">Analyzing your CV...</div><div class="text-sm text-gray-500 mt-1 animate-pulse">This may take a few moments</div>`;
        originalShow();
        if (progressBar) {
            progressInterval = setInterval(() => {
                progress += Math.random() * 10;
                if (progress > 90) progress = 90;
                progressBar.style.width = `${progress}%`;
            }, 300);
        }
    };
    loadingModal.hide = function () {
        if (progressBar) progressBar.classList.remove('animate-progress');
        if (progressBar) progressBar.style.width = '100%';
        if (progressInterval) clearInterval(progressInterval);
        setTimeout(() => {
            if (progressBar) progressBar.style.width = '0%';
        }, 400);
        originalHide();
    };
    // Cancel button logic (reset progress)
    if (cancelBtn) {
        cancelBtn.addEventListener('click', function () {
            if (progressInterval) clearInterval(progressInterval);
            if (progressBar) progressBar.classList.remove('animate-progress');
            if (progressBar) progressBar.style.width = '0%';
        });
    }
});
