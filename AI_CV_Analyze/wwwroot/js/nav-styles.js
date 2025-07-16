document.addEventListener('DOMContentLoaded', function () {
    // ================ MOBILE MENU HANDLING ================
    const mobileMenu = {
        button: document.getElementById('mobile-menu-button'),
        menu: document.getElementById('mobile-menu'),
        isOpen: false,
        toggle: function () {
            this.isOpen = !this.isOpen;
            if (this.isOpen) {
                this.menu.classList.remove('hidden');
                this.button.setAttribute('aria-expanded', 'true');
            } else {
                this.menu.classList.add('hidden');
                this.button.setAttribute('aria-expanded', 'false');
            }
        },
        init: function () {
            if (this.button && this.menu) {
                this.button.addEventListener('click', () => this.toggle());
                // Close menu when clicking outside
                document.addEventListener('click', (e) => {
                    if (!this.button.contains(e.target) && !this.menu.contains(e.target)) {
                        if (this.isOpen) {
                            this.toggle();
                        }
                    }
                });
            }
        }
    };
    // ================ USER MENU DROPDOWN ================
    const userMenu = {
        button: document.getElementById('user-menu-button'),
        dropdown: document.getElementById('user-menu-dropdown'),
        isOpen: false,
        toggle: function () {
            this.isOpen = !this.isOpen;
            if (this.isOpen) {
                this.dropdown.classList.remove('hidden');
                this.button.setAttribute('aria-expanded', 'true');
            } else {
                this.dropdown.classList.add('hidden');
                this.button.setAttribute('aria-expanded', 'false');
            }
        },
        init: function () {
            if (this.button && this.dropdown) {
                this.button.addEventListener('click', (e) => {
                    e.stopPropagation();
                    this.toggle();
                });
                // Close dropdown when clicking outside
                document.addEventListener('click', (e) => {
                    if (!this.button.contains(e.target) && !this.dropdown.contains(e.target)) {
                        if (this.isOpen) {
                            this.toggle();
                        }
                    }
                });
            }
        }
    };
    // ================ ACTIVE LINK ================
    const currentPath = window.location.pathname;
    document.querySelectorAll('.nav-link').forEach(link => {
        if (link.getAttribute('href') === currentPath) {
            link.classList.add('active');
        }
    });
    // ================ INITIALIZATION ================
    mobileMenu.init();
    userMenu.init();
});