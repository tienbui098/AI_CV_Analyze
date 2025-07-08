document.addEventListener("DOMContentLoaded", function () {
    // Animate progress ring
    const circle = document.querySelector('.progress-ring__circle');
    if (circle) {
        const radius = circle.r.baseVal.value;
        const circumference = radius * 2 * Math.PI;

        circle.style.strokeDasharray = `${circumference} ${circumference}`;
        circle.style.strokeDashoffset = circumference;

        const offset = circumference - (75 / 100) * circumference;
        setTimeout(() => {
            circle.style.strokeDashoffset = offset;
        }, 100);
    }

    // Highlight important keywords in content
    const preElements = document.querySelectorAll('pre');
    const keywords = ['kinh nghiệm', 'thành tích', 'dự án', 'kỹ năng', 'chứng chỉ', 'giải thưởng'];

    preElements.forEach(pre => {
        let text = pre.innerHTML;
        keywords.forEach(keyword => {
            const regex = new RegExp(`(${keyword})`, 'gi');
            text = text.replace(regex, '<span class="font-bold text-blue-600">$1</span>');
        });
        pre.innerHTML = text;
    });

    // Form submission handling
    const form = document.getElementById("editSuggestionsForm");
    const cancelBtn = document.getElementById("cancelAnalyzeBtn");
    const loadingModal = document.getElementById("loadingModal");
    let controller = null;

    if (form) {
        form.addEventListener("submit", function (e) {
            e.preventDefault();

            // Show loading modal
            loadingModal.classList.remove('hidden');

            // Create new AbortController
            controller = new AbortController();

            // Submit form with fetch
            fetch(form.action, {
                method: "POST",
                body: new FormData(form),
                signal: controller.signal
            })
                .then(response => {
                    if (response.redirected) {
                        window.location.href = response.url;
                    }
                })
                .catch(error => {
                    if (error.name !== 'AbortError') {
                        alert("Có lỗi xảy ra khi gửi yêu cầu.");
                    }
                })
                .finally(() => {
                    loadingModal.classList.add('hidden');
                });
        });
    }

    if (cancelBtn) {
        cancelBtn.addEventListener("click", function () {
            if (controller) {
                controller.abort();
            }
            loadingModal.classList.add('hidden');
        });
    }
});