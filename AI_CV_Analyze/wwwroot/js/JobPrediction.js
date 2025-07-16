document.addEventListener('DOMContentLoaded', function () {
    // ================ FORM VALIDATION ================
    const formValidation = {
        form: null,
        skillsInput: null,
        validateSkills: function (skills) {
            if (!skills || skills.trim().length === 0) {
                return {
                    isValid: false,
                    message: 'Vui lòng nhập ít nhất một kỹ năng.'
                };
            }
            const skillsArray = skills.split(',').map(skill => skill.trim()).filter(skill => skill.length > 0);
            if (skillsArray.length === 0) {
                return {
                    isValid: false,
                    message: 'Vui lòng nhập ít nhất một kỹ năng hợp lệ.'
                };
            }
            if (skillsArray.length < 3) {
                return {
                    isValid: false,
                    message: 'Vui lòng nhập ít nhất 3 kỹ năng để có kết quả chính xác hơn.'
                };
            }
            return {
                isValid: true,
                message: ''
            };
        },
        showError: function (input, message) {
            input.classList.add('is-invalid');
            const existingError = input.parentNode.querySelector('.invalid-feedback');
            if (existingError) {
                existingError.remove();
            }
            const errorDiv = document.createElement('div');
            errorDiv.className = 'invalid-feedback';
            errorDiv.textContent = message;
            input.parentNode.appendChild(errorDiv);
        },
        clearError: function (input) {
            input.classList.remove('is-invalid');
            const errorDiv = input.parentNode.querySelector('.invalid-feedback');
            if (errorDiv) {
                errorDiv.remove();
            }
        },
        init: function () {
            this.form = document.querySelector('form[asp-action="JobPrediction"]');
            this.skillsInput = document.getElementById('Skills');
            if (this.skillsInput) {
                this.skillsInput.addEventListener('input', () => {
                    const validation = this.validateSkills(this.skillsInput.value);
                    if (validation.isValid) {
                        this.clearError(this.skillsInput);
                    } else {
                        this.showError(this.skillsInput, validation.message);
                    }
                });
                if (this.form) {
                    this.form.addEventListener('submit', (e) => {
                        const validation = this.validateSkills(this.skillsInput.value);
                        if (!validation.isValid) {
                            e.preventDefault();
                            this.showError(this.skillsInput, validation.message);
                            toast.show(validation.message, 'error');
                            return false;
                        }
                    });
                }
            }
        }
    };
    // ================ SKILLS AUTOCOMPLETE ================
    const skillsAutocomplete = {
        commonSkills: [
            'JavaScript', 'Python', 'Java', 'C#', 'C++', 'PHP', 'Ruby', 'Go', 'Swift', 'Kotlin',
            'React', 'Angular', 'Vue.js', 'Node.js', 'Express.js', 'Django', 'Flask', 'Spring Boot',
            'HTML', 'CSS', 'Sass', 'Less', 'TypeScript', 'Bootstrap', 'Tailwind CSS',
            'SQL', 'MySQL', 'PostgreSQL', 'MongoDB', 'Redis', 'Oracle', 'SQL Server',
            'Git', 'GitHub', 'GitLab', 'Docker', 'Kubernetes', 'AWS', 'Azure', 'Google Cloud',
            'Linux', 'Windows', 'macOS', 'Agile', 'Scrum', 'JIRA', 'Confluence',
            'REST API', 'GraphQL', 'Microservices', 'CI/CD', 'Jenkins', 'Travis CI',
            'Machine Learning', 'Data Science', 'TensorFlow', 'PyTorch', 'Pandas', 'NumPy',
            'Photoshop', 'Illustrator', 'Figma', 'Sketch', 'Adobe XD'
        ],
        createSuggestionBox: function () {
            const suggestionBox = document.createElement('div');
            suggestionBox.className = 'skills-suggestion-box';
            suggestionBox.style.cssText = `
                position: absolute;
                top: 100%;
                left: 0;
                right: 0;
                background: white;
                border: 1px solid #e2e8f0;
                border-top: none;
                border-radius: 0 0 0.5rem 0.5rem;
                max-height: 200px;
                overflow-y: auto;
                z-index: 1000;
                display: none;
                box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
            `;
            return suggestionBox;
        },
        filterSkills: function (input) {
            const value = input.value.toLowerCase();
            const lastCommaIndex = value.lastIndexOf(',');
            const currentWord = lastCommaIndex >= 0 ? value.substring(lastCommaIndex + 1).trim() : value;
            if (currentWord.length < 2) return [];
            return this.commonSkills.filter(skill => 
                skill.toLowerCase().includes(currentWord) &&
                !value.toLowerCase().includes(skill.toLowerCase())
            ).slice(0, 8);
        },
        showSuggestions: function (input, suggestions) {
            let suggestionBox = input.parentNode.querySelector('.skills-suggestion-box');
            if (!suggestionBox) {
                suggestionBox = this.createSuggestionBox();
                input.parentNode.style.position = 'relative';
                input.parentNode.appendChild(suggestionBox);
            }
            if (suggestions.length === 0) {
                suggestionBox.style.display = 'none';
                return;
            }
            suggestionBox.innerHTML = suggestions.map(skill => 
                `<div class="suggestion-item" style="padding: 0.5rem 1rem; cursor: pointer; border-bottom: 1px solid #f1f5f9; transition: background-color 0.2s;" onmouseover="this.style.backgroundColor='#f8fafc'" onmouseout="this.style.backgroundColor='white'">${skill}</div>`
            ).join('');
            suggestionBox.style.display = 'block';
            suggestionBox.querySelectorAll('.suggestion-item').forEach(item => {
                item.addEventListener('click', () => {
                    const value = input.value;
                    const lastCommaIndex = value.lastIndexOf(',');
                    const newValue = lastCommaIndex >= 0 
                        ? value.substring(0, lastCommaIndex + 1) + ' ' + item.textContent
                        : item.textContent;
                    input.value = newValue;
                    suggestionBox.style.display = 'none';
                    input.focus();
                    input.dispatchEvent(new Event('input'));
                });
            });
        },
        init: function () {
            const skillsInput = document.getElementById('Skills');
            if (!skillsInput) return;

            let suggestionBox = null;

            skillsInput.addEventListener('input', () => {
                const suggestions = this.filterSkills(skillsInput);
                this.showSuggestions(skillsInput, suggestions);
            });

            skillsInput.addEventListener('blur', () => {
                // Delay hiding to allow for clicks
                setTimeout(() => {
                    const suggestionBox = skillsInput.parentNode.querySelector('.skills-suggestion-box');
                    if (suggestionBox) {
                        suggestionBox.style.display = 'none';
                    }
                }, 200);
            });

            // Hide suggestions when clicking outside
            document.addEventListener('click', (e) => {
                if (!skillsInput.contains(e.target) && !suggestionBox?.contains(e.target)) {
                    if (suggestionBox) {
                        suggestionBox.style.display = 'none';
                    }
                }
            });
        }
    };

    // ================ TOAST NOTIFICATIONS ================
    const toast = {
        show: function (message, type = 'info') {
            const toastElement = document.createElement('div');
            toastElement.className = `toast ${this.getColor(type)}`;
            toastElement.innerHTML = `<i class="${this.getIcon(type)} mr-2"></i>${message}`;

            document.body.appendChild(toastElement);

            // Auto remove after 4 seconds
            setTimeout(() => {
                toastElement.style.opacity = '0';
                toastElement.style.transform = 'translateX(100%)';
                setTimeout(() => toastElement.remove(), 300);
            }, 4000);
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

    // ================ ANIMATIONS ================
    const animations = {
        animateCircularCharts: function () {
            const charts = document.querySelectorAll('.circular-chart-svg .circle');
            charts.forEach((chart, index) => {
                setTimeout(() => {
                    chart.style.animation = 'progress 1s ease-out forwards';
                }, index * 200);
            });
        },

        animateCards: function () {
            const cards = document.querySelectorAll('.job-recommendation-card');
            cards.forEach((card, index) => {
                card.style.opacity = '0';
                card.style.transform = 'translateY(20px)';
                
                setTimeout(() => {
                    card.style.transition = 'all 0.5s ease';
                    card.style.opacity = '1';
                    card.style.transform = 'translateY(0)';
                }, index * 150);
            });
        },

        init: function () {
            // Animate on page load if results are present
            if (document.querySelector('.job-recommendation-card')) {
                setTimeout(() => {
                    this.animateCircularCharts();
                    this.animateCards();
                }, 500);
            }
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

    // ================ LOADING MODAL HANDLING ================
    const loadingModal = document.getElementById('loadingModal');
    const cancelAnalyzeBtn = document.getElementById('cancelAnalyzeBtn');
    let formSubmitting = false;

    function showLoadingModal() {
        if (loadingModal) {
            loadingModal.classList.remove('hidden');
        }
        formSubmitting = true;
    }
    function hideLoadingModal() {
        if (loadingModal) {
            loadingModal.classList.add('hidden');
        }
        formSubmitting = false;
    }
    if (cancelAnalyzeBtn) {
        cancelAnalyzeBtn.addEventListener('click', function () {
            hideLoadingModal();
            // Optionally, you can abort the form submission here if using AJAX
        });
    }

    // ================ INITIALIZATION ================
    formValidation.init();
    skillsAutocomplete.init();
    animations.init();

    // Show welcome message if no results
    if (!document.querySelector('.job-recommendation-card')) {
        console.log('Job Prediction page loaded successfully');
    }

    // Handle back button
    const backButton = document.querySelector('.back-button');
    if (backButton) {
        backButton.addEventListener('click', (e) => {
            e.preventDefault();
            const href = backButton.getAttribute('href');
            if (href) {
                window.location.href = href;
            }
            hideLoadingModal(); // Hide loading modal on navigation
        });
    }

    // Handle form submission with loading modal
    const jobPredictionForm = document.getElementById('jobPredictionForm');
    if (jobPredictionForm) {
        jobPredictionForm.addEventListener('submit', function (e) {
            // Only show modal if validation passes (no .is-invalid)
            if (!jobPredictionForm.querySelector('.is-invalid')) {
                showLoadingModal();
                // Hide modal after 30 seconds as fallback
                setTimeout(() => {
                    if (formSubmitting) hideLoadingModal();
                }, 30000);
            }
        });
    }

    // Keyboard shortcuts
    document.addEventListener('keydown', (e) => {
        // Ctrl/Cmd + Enter to submit form
        if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
            const form = document.querySelector('form[asp-action="JobPrediction"]');
            if (form && document.activeElement === document.getElementById('Skills')) {
                form.submit();
            }
        }
    });

    function shareResults() {
        if (navigator.share) {
            navigator.share({
                title: 'Kết quả dự đoán công việc - AI CV Analyze',
                text: 'Xem kết quả dự đoán công việc phù hợp với kỹ năng của tôi',
                url: window.location.href
            });
        } else {
            // Fallback: copy to clipboard
            navigator.clipboard.writeText(window.location.href).then(() => {
                toast.show('Đã sao chép link vào clipboard!', 'success');
            });
        }
    }
}); 