// =============================================
// MOBILE BOTTOM NAVIGATION - ULTRA PREMIUM
// =============================================

class MobileBottomNav {
    constructor() {
        this.nav = null;
        this.lastScrollTop = 0;
        this.scrollThreshold = 30;
        this.navVisible = false;
        this.userType = '';
        this.isLoggedIn = false;
        this.init();
    }

    init() {
        // Check user type and login status
        this.checkUserStatus();

        // Don't show for Admin users or non-logged in users
        if (this.userType === 'Admin' || !this.isLoggedIn) {
            console.log('Mobile nav hidden: Admin user or not logged in');
            return;
        }

        // Create navigation HTML
        this.createNavHTML();

        // Initialize navigation
        this.initNavigation();

        // Setup scroll behavior
        this.setupScrollBehavior();

        // Setup click effects
        this.setupClickEffects();

        // Set active menu item based on current page
        this.setActiveMenuItem();

        // Add keyboard navigation support
        this.setupKeyboardNav();
    }

    checkUserStatus() {
        // Try to get user type from various sources
        this.userType = this.getUserType();
        this.isLoggedIn = this.checkIfLoggedIn();

        console.log('User Type:', this.userType, 'Logged In:', this.isLoggedIn);
    }

    getUserType() {
        // Check multiple sources for user type
        const sources = [
            () => document.querySelector('[data-user-type]')?.getAttribute('data-user-type'),
            () => {
                const url = window.location.pathname;
                if (url.includes('/Patient/')) return 'Patient';
                if (url.includes('/Doctor/')) return 'Doctor';
                if (url.includes('/Nurse/')) return 'Nurse';
                if (url.includes('/Admin/')) return 'Admin';
                return '';
            },
            () => {
                try {
                    const userType = localStorage.getItem('userType') ||
                        sessionStorage.getItem('userType');
                    return userType;
                } catch {
                    return '';
                }
            }
        ];

        for (const source of sources) {
            const result = source();
            if (result) return result;
        }

        return '';
    }

    checkIfLoggedIn() {
        // Check if user is logged in
        const checks = [
            () => !!this.userType && this.userType !== '',
            () => !!document.cookie.includes('.AspNetCore.Cookies'),
            () => {
                try {
                    return localStorage.getItem('isLoggedIn') === 'true' ||
                        sessionStorage.getItem('isLoggedIn') === 'true';
                } catch {
                    return false;
                }
            }
        ];

        return checks.some(check => check());
    }

    createNavHTML() {
        const menuItems = this.getMenuItems();

        if (!menuItems.length) return;

        const navHTML = `
            <nav class="mobile-bottom-nav" id="mobileBottomNav" 
                 role="navigation" 
                 aria-label="Mobile Navigation">
                <div class="nav-scroll-progress" id="navScrollProgress"></div>
                <div class="mobile-nav-divider"></div>
                <div class="mobile-nav-items">
                    ${menuItems.map((item, index) => `
                        <a href="${item.url}" 
                           class="mobile-nav-item ${item.class || ''}" 
                           data-page="${item.url}"
                           title="${item.label}"
                           aria-label="${item.label}"
                           ${index === 0 ? 'tabindex="0"' : 'tabindex="-1"'}>
                            <div class="nav-icon-container">
                                <i class="${item.icon}"></i>
                                ${item.badge ? `<span class="nav-badge">${item.badge}</span>` : ''}
                            </div>
                            <span class="nav-label">${item.label}</span>
                        </a>
                    `).join('')}
                </div>
            </nav>
        `;

        document.body.insertAdjacentHTML('beforeend', navHTML);
        this.nav = document.getElementById('mobileBottomNav');

        // Add user type class for theming
        if (this.userType) {
            this.nav.classList.add(`user-${this.userType.toLowerCase()}`);
        }

        // Show nav with animation
        setTimeout(() => {
            this.nav.classList.add('active');
            this.navVisible = true;
        }, 300);
    }

    getMenuItems() {
        const menus = {
            'Patient': [
                {
                    icon: 'fas fa-tachometer-alt',
                    label: 'Home',
                    url: '/Patient/Dashboard',
                    badge: this.getPatientBadge('dashboard')
                },
                {
                    icon: 'fas fa-user-edit',
                    label: 'Profile',
                    url: '/Patient/Profile'
                },
                {
                    icon: 'fas fa-calendar-plus',
                    label: 'Book',
                    url: '/Appointment/Create',
                    badge: this.getPatientBadge('appointments')
                },
                {
                    icon: 'fas fa-prescription',
                    label: 'Rx',
                    url: '/Patient/Prescriptions',
                    badge: this.getPatientBadge('prescriptions')
                },
                {
                    icon: 'fas fa-sign-out-alt',
                    label: 'Logout',
                    url: '/Account/Logout',
                    class: 'logout'
                }
            ],
            'Doctor': [
                {
                    icon: 'fas fa-tachometer-alt',
                    label: 'Home',
                    url: '/Doctor/Dashboard',
                    badge: this.getDoctorBadge('dashboard')
                },
                {
                    icon: 'fas fa-calendar-check',
                    label: 'Today',
                    url: '/Doctor/MyAppointments',
                    badge: this.getDoctorBadge('appointments')
                },
                {
                    icon: 'fas fa-user-injured',
                    label: 'Patients',
                    url: '/Doctor/MyPatients',
                    badge: this.getDoctorBadge('patients')
                },
                {
                    icon: 'fas fa-prescription',
                    label: 'Rx',
                    url: '/Prescription/Create',
                    badge: this.getDoctorBadge('prescriptions')
                },
                {
                    icon: 'fas fa-sign-out-alt',
                    label: 'Logout',
                    url: '/Account/Logout',
                    class: 'logout'
                }
            ],
            'Nurse': [
                {
                    icon: 'fas fa-tachometer-alt',
                    label: 'Home',
                    url: '/Nurse/Dashboard',
                    badge: this.getNurseBadge('dashboard')
                },
                {
                    icon: 'fas fa-user-injured',
                    label: 'Patients',
                    url: '/Nurse/Patients',
                    badge: this.getNurseBadge('patients')
                },
                {
                    icon: 'fas fa-calendar-check',
                    label: 'Schedule',
                    url: '/AppointmentManagement/Index',
                    badge: this.getNurseBadge('appointments')
                },
                {
                    icon: 'fas fa-vial',
                    label: 'Tests',
                    url: '/Test/Create',
                    badge: this.getNurseBadge('tests')
                },
                {
                    icon: 'fas fa-sign-out-alt',
                    label: 'Logout',
                    url: '/Account/Logout',
                    class: 'logout'
                }
            ]
        };

        return menus[this.userType] || [];
    }

    getPatientBadge(type) {
        // You can implement dynamic badge counts here
        const badges = {
            'dashboard': '',
            'appointments': '', // Example: '3' for pending appointments
            'prescriptions': '' // Example: '2' for new prescriptions
        };
        return badges[type] || '';
    }

    getDoctorBadge(type) {
        const badges = {
            'dashboard': '',
            'appointments': '', // Example: '5' for today's appointments
            'patients': '',    // Example: '12' for active patients
            'prescriptions': '' // Example: '4' for prescriptions to write
        };
        return badges[type] || '';
    }

    getNurseBadge(type) {
        const badges = {
            'dashboard': '',
            'patients': '',    // Example: '8' for patients to attend
            'appointments': '', // Example: '6' for upcoming appointments
            'tests': ''        // Example: '3' for pending tests
        };
        return badges[type] || '';
    }

    initNavigation() {
        // Add touch support
        this.setupTouchSupport();

        // Add vibration feedback on mobile
        this.setupVibration();

        // Setup intersection observer for scroll progress
        this.setupScrollProgress();
    }

    setupScrollBehavior() {
        let ticking = false;

        window.addEventListener('scroll', () => {
            if (!ticking) {
                window.requestAnimationFrame(() => {
                    this.handleScroll();
                    ticking = false;
                });
                ticking = true;
            }
        });
    }

    handleScroll() {
        const currentScroll = window.pageYOffset || document.documentElement.scrollTop;
        const windowHeight = window.innerHeight;
        const documentHeight = document.documentElement.scrollHeight;

        // Calculate scroll progress
        const scrollPercent = (currentScroll / (documentHeight - windowHeight)) * 100;
        this.updateScrollProgress(scrollPercent);

        // Hide/show nav based on scroll direction
        if (currentScroll > this.lastScrollTop && currentScroll > this.scrollThreshold) {
            // Scrolling DOWN - hide nav
            if (this.navVisible) {
                this.nav.classList.add('scroll-hide');
                this.nav.classList.remove('scroll-show');
            }
        } else {
            // Scrolling UP - show nav
            if (this.navVisible) {
                this.nav.classList.add('scroll-show');
                this.nav.classList.remove('scroll-hide');
            }
        }

        this.lastScrollTop = currentScroll <= 0 ? 0 : currentScroll;
    }

    updateScrollProgress(percent) {
        const progressBar = document.getElementById('navScrollProgress');
        if (progressBar) {
            progressBar.style.width = `${percent}%`;
        }
    }

    setupClickEffects() {
        const navItems = this.nav.querySelectorAll('.mobile-nav-item');

        navItems.forEach(item => {
            item.addEventListener('click', (e) => {
                // Create ripple effect
                this.createRippleEffect(e, item);

                // Add click animation
                this.animateClick(item);

                // Handle special cases
                this.handleSpecialClick(item, e);
            });
        });
    }

    createRippleEffect(event, element) {
        const rect = element.getBoundingClientRect();
        const ripple = document.createElement('span');
        const size = Math.max(rect.width, rect.height);

        ripple.style.width = ripple.style.height = `${size}px`;
        ripple.style.left = `${event.clientX - rect.left - size / 2}px`;
        ripple.style.top = `${event.clientY - rect.top - size / 2}px`;
        ripple.classList.add('ripple');

        element.appendChild(ripple);

        setTimeout(() => {
            ripple.remove();
        }, 600);
    }

    animateClick(element) {
        element.style.transform = 'scale(0.95)';
        setTimeout(() => {
            element.style.transform = '';
        }, 200);

        // Add loading state if needed
        if (element.href.includes('Logout')) {
            element.classList.add('loading');
        }
    }

    handleSpecialClick(item, event) {
        if (item.classList.contains('logout')) {
            if (!confirm('Are you sure you want to logout?')) {
                event.preventDefault();
                item.classList.remove('loading');
            }
        }
    }

    setupKeyboardNav() {
        const navItems = this.nav.querySelectorAll('.mobile-nav-item');
        let currentIndex = 0;

        document.addEventListener('keydown', (e) => {
            if (!this.navVisible) return;

            switch (e.key) {
                case 'ArrowRight':
                    e.preventDefault();
                    currentIndex = (currentIndex + 1) % navItems.length;
                    this.focusNavItem(navItems, currentIndex);
                    break;
                case 'ArrowLeft':
                    e.preventDefault();
                    currentIndex = (currentIndex - 1 + navItems.length) % navItems.length;
                    this.focusNavItem(navItems, currentIndex);
                    break;
                case 'Enter':
                case ' ':
                    if (document.activeElement.classList.contains('mobile-nav-item')) {
                        document.activeElement.click();
                    }
                    break;
            }
        });
    }

    focusNavItem(items, index) {
        items.forEach((item, i) => {
            item.tabIndex = i === index ? '0' : '-1';
        });

        items[index].focus();
        this.setActiveItem(items[index]);
    }

    setActiveMenuItem() {
        const currentPath = window.location.pathname;
        const navItems = this.nav.querySelectorAll('.mobile-nav-item');

        navItems.forEach(item => {
            const itemUrl = item.getAttribute('href');
            if (this.isActivePage(currentPath, itemUrl)) {
                this.setActiveItem(item);
            }
        });
    }

    isActivePage(currentPath, itemUrl) {
        // Simple path matching logic
        const normalizedPath = currentPath.toLowerCase();
        const normalizedUrl = itemUrl.toLowerCase();

        if (normalizedUrl === '/account/logout') return false;

        return normalizedPath.includes(normalizedUrl.replace('/', '')) ||
            (normalizedPath === '/' && normalizedUrl.includes('dashboard'));
    }

    setActiveItem(item) {
        // Remove active class from all items
        this.nav.querySelectorAll('.mobile-nav-item').forEach(el => {
            el.classList.remove('active');
        });

        // Add active class to clicked item
        item.classList.add('active');
    }

    setupTouchSupport() {
        // Prevent bounce on iOS
        document.addEventListener('touchmove', (e) => {
            if (e.scale !== 1) {
                e.preventDefault();
            }
        }, { passive: false });
    }

    setupVibration() {
        // Add haptic feedback on supported devices
        if ('vibrate' in navigator) {
            const navItems = this.nav.querySelectorAll('.mobile-nav-item');
            navItems.forEach(item => {
                item.addEventListener('click', () => {
                    navigator.vibrate(10); // 10ms vibration
                });
            });
        }
    }

    setupScrollProgress() {
        // Already implemented in handleScroll
    }

    // Public API
    show() {
        if (this.nav) {
            this.nav.classList.add('active');
            this.nav.classList.add('scroll-show');
            this.navVisible = true;
        }
    }

    hide() {
        if (this.nav) {
            this.nav.classList.remove('active');
            this.nav.classList.remove('scroll-show');
            this.navVisible = false;
        }
    }

    toggle() {
        if (this.navVisible) {
            this.hide();
        } else {
            this.show();
        }
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    // Create global instance
    window.mobileNav = new MobileBottomNav();

    // Add to global scope for debugging
    console.log('Ultra Premium Mobile Navigation Loaded');
});

// Handle page transitions
document.addEventListener('pagehide', () => {
    if (window.mobileNav) {
        window.mobileNav.hide();
    }
});

document.addEventListener('pageshow', () => {
    if (window.mobileNav) {
        setTimeout(() => {
            window.mobileNav.show();
        }, 100);
    }
});

// Handle resize events
let resizeTimeout;
window.addEventListener('resize', () => {
    clearTimeout(resizeTimeout);
    resizeTimeout = setTimeout(() => {
        if (window.mobileNav && window.mobileNav.navVisible) {
            window.mobileNav.setActiveMenuItem();
        }
    }, 250);
});

// Swipe gestures for hide/show
let touchStartY = 0;
let touchEndY = 0;

document.addEventListener('touchstart', (e) => {
    touchStartY = e.changedTouches[0].screenY;
});

document.addEventListener('touchend', (e) => {
    touchEndY = e.changedTouches[0].screenY;
    handleSwipe();
});

function handleSwipe() {
    const swipeDistance = touchEndY - touchStartY;

    if (Math.abs(swipeDistance) < 50) return;

    if (swipeDistance > 0 && window.mobileNav) {
        // Swipe down - show nav
        window.mobileNav.nav.classList.add('scroll-show');
    } else if (swipeDistance < 0 && window.mobileNav) {
        // Swipe up - hide nav
        window.mobileNav.nav.classList.add('scroll-hide');
    }
}