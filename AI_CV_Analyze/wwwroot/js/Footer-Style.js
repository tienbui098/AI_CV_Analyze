document.addEventListener('DOMContentLoaded', function () {
    // Newsletter form submission
    const newsletterForm = document.querySelector('footer form');
    if (newsletterForm) {
        newsletterForm.addEventListener('submit', function (e) {
            e.preventDefault();
            const emailInput = this.querySelector('input[type="email"]');
            if (emailInput.value) {
                // Here you would typically make an AJAX call to your backend
                console.log('Subscribed with email:', emailInput.value);
                alert('Thank you for subscribing to our newsletter!');
                emailInput.value = '';
            }
        });
    }

    // Social media links - could add analytics tracking here
    document.querySelectorAll('footer .social-icon').forEach(icon => {
        icon.addEventListener('click', function (e) {
            // Track social media clicks in analytics
            console.log('Social media link clicked:', this.href);
        });
    });
});