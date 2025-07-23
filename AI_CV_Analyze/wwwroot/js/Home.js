document.addEventListener('DOMContentLoaded', function () {
    // FAQ Accordion
    const faqToggles = document.querySelectorAll('.faq-toggle');
    faqToggles.forEach(toggle => {
        toggle.addEventListener('click', function () {
            const content = this.nextElementSibling;

            // Close all other FAQs
            document.querySelectorAll('.faq-content').forEach(item => {
                if (item !== content) {
                    item.classList.remove('active');
                    item.previousElementSibling.classList.remove('active');
                    item.style.maxHeight = '0';
                }
            });

            // Toggle current FAQ
            this.classList.toggle('active');
            content.classList.toggle('active');

            if (content.classList.contains('active')) {
                content.style.maxHeight = content.scrollHeight + 'px';
            } else {
                content.style.maxHeight = '0';
            }
        });
    });

    // Animate elements when they come into view
    const animateOnScroll = function () {
        const elements = document.querySelectorAll('.animate-fade-in-up, .animate-fade-in-right, .animate-fade-in-left');

        elements.forEach(element => {
            const elementPosition = element.getBoundingClientRect().top;
            const windowHeight = window.innerHeight;

            if (elementPosition < windowHeight - 100) {
                element.style.opacity = '1';
                element.style.transform = 'translate(0)';
            }
        });
    };

    // Run once on load
    animateOnScroll();

    // Run on scroll
    window.addEventListener('scroll', animateOnScroll);
});