// ============== GENERAL SITE FUNCTIONS ==============
document.addEventListener('DOMContentLoaded', function () {
    // Scroll animation
    const fadeElements = document.querySelectorAll('.fade-in');
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
            }
        });
    }, {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    });

    fadeElements.forEach(element => {
        observer.observe(element);
    });

    // Navbar scroll effect
    const navbar = document.querySelector('.navbar');
    if (navbar) {
        window.addEventListener('scroll', function () {
            if (window.scrollY > 100) {
                navbar.style.padding = '0.8rem 0';
                navbar.style.boxShadow = '0 4px 20px rgba(0, 0, 0, 0.1)';
            } else {
                navbar.style.padding = '1.2rem 0';
                navbar.style.boxShadow = '0 4px 20px rgba(0, 0, 0, 0.05)';
            }
        });
    }

    // Set active nav link
    $('.nav-link').each(function () {
        if (this.href === window.location.href) {
            $(this).addClass('active');
        }
    });

    // Emergency banner animation
    setInterval(() => {
        $('.emergency-banner i').toggleClass('fa-beat');
    }, 2000);

    // Complaint modal event listeners
    setupComplaintModal();

    // Load user info if logged in (for complaint form)
    autoFillUserInfoForComplaint();
});

// ============== COMPLAINT MODAL FUNCTIONS ==============

function setupComplaintModal() {
    const modal = document.getElementById('complaintModal');
    const complaintBtn = document.getElementById('complaintBtn');

    if (complaintBtn) {
        complaintBtn.addEventListener('click', openComplaintModal);
    }

    if (modal) {
        // Close when clicking outside
        modal.addEventListener('click', function (e) {
            if (e.target === modal) {
                closeComplaintModal();
            }
        });

        // ESC key to close
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && modal.style.display === 'flex') {
                closeComplaintModal();
            }
        });
    }
}

function openComplaintModal() {
    const modal = document.getElementById('complaintModal');
    if (modal) {
        modal.style.display = 'flex';
        document.body.style.overflow = 'hidden';

        // Auto-fill user information if logged in
        autoFillUserInfoForComplaint();

        // Add focus to first input
        setTimeout(() => {
            const nameInput = document.getElementById('name');
            if (nameInput) nameInput.focus();
        }, 100);
    }
}

function closeComplaintModal() {
    const modal = document.getElementById('complaintModal');
    if (modal) {
        modal.style.display = 'none';
        document.body.style.overflow = 'auto';

        // Reset form to editable state for non-logged in users
        resetComplaintForm();
    }
}

// Auto-fill user info for logged in users
function autoFillUserInfoForComplaint() {
    // Check if user is logged in (check session/cookie)
    fetch('/Complaint/GetLoggedInUserInfo')
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success && data.isLoggedIn) {
                // Fill form fields
                const nameInput = document.getElementById('name');
                const emailInput = document.getElementById('email');
                const phoneInput = document.getElementById('phone');

                if (nameInput && emailInput && phoneInput) {
                    // Set values
                    nameInput.value = data.name || '';
                    emailInput.value = data.email || '';
                    phoneInput.value = data.phone || '';

                    // Make fields read-only for logged-in users
                    nameInput.readOnly = true;
                    emailInput.readOnly = true;
                    phoneInput.readOnly = true;

                    // Change placeholder to show info is from profile
                    nameInput.placeholder = "Filled from your profile";
                    emailInput.placeholder = "Filled from your profile";
                    phoneInput.placeholder = "Filled from your profile";

                    // Add small info message
                    showComplaintFormMessage('Your profile information has been auto-filled', 'info');
                }
            } else {
                // User not logged in, keep fields editable
                resetComplaintForm();
            }
        })
        .catch(error => {
            console.log('User not logged in or error fetching user info:', error);
            resetComplaintForm();
        });
}

function resetComplaintForm() {
    const nameInput = document.getElementById('name');
    const emailInput = document.getElementById('email');
    const phoneInput = document.getElementById('phone');

    if (nameInput && emailInput && phoneInput) {
        // Reset to editable
        nameInput.readOnly = false;
        emailInput.readOnly = false;
        phoneInput.readOnly = false;

        // Reset placeholders
        nameInput.placeholder = "Your full name";
        emailInput.placeholder = "example@email.com";
        phoneInput.placeholder = "01XXXXXXXXX";

        // Remove any existing messages
        removeComplaintFormMessage();
    }
}

function showComplaintFormMessage(message, type) {
    // Remove existing message
    removeComplaintFormMessage();

    // Create message element
    const messageDiv = document.createElement('div');
    messageDiv.id = 'complaintFormMessage';
    messageDiv.className = `alert alert-${type} alert-dismissible fade show mb-3`;
    messageDiv.innerHTML = `
        <i class="fas fa-info-circle me-2"></i>${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

    // Insert at the top of form
    const form = document.getElementById('complaintForm');
    if (form) {
        form.parentNode.insertBefore(messageDiv, form);
    }
}

function removeComplaintFormMessage() {
    const messageDiv = document.getElementById('complaintFormMessage');
    if (messageDiv) {
        messageDiv.remove();
    }
}

// Submit complaint form
function submitComplaint(event) {
    event.preventDefault();

    const form = event.target;
    const formData = new FormData(form);

    // Get form values
    const name = formData.get('name');
    const email = formData.get('email');
    const phone = formData.get('phone');
    const feedbackType = formData.get('feedbackType');
    const feedback = formData.get('feedback');

    // Validation
    if (!name || !email || !phone || !feedbackType || !feedback) {
        showComplaintFormMessage('Please fill all required fields', 'danger');
        return;
    }

    // Email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
        showComplaintFormMessage('Please enter a valid email address', 'danger');
        return;
    }

    // Phone validation (Bangladeshi numbers)
    const phoneRegex = /^(?:\+88|88)?(01[3-9]\d{8})$/;
    if (!phoneRegex.test(phone.replace(/\s/g, ''))) {
        showComplaintFormMessage('Please enter a valid Bangladeshi phone number (e.g., 01712345678)', 'danger');
        return;
    }

    // Show loading state
    const submitBtn = form.querySelector('button[type="submit"]');
    const originalText = submitBtn.innerHTML;
    submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i> Submitting...';
    submitBtn.disabled = true;

    // Get anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    // Prepare data for submission (match your controller parameters)
    const submissionData = {
        Name: name,
        Email: email,
        Phone: phone,
        Subject: feedbackType,
        Description: feedback
    };

    // Submit to server
    fetch('/Complaint/Submit', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token,
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: JSON.stringify(submissionData)
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                // Show success message
                showComplaintFormMessage(data.message, 'success');

                // Reset form
                form.reset();

                // Close modal after 3 seconds
                setTimeout(() => {
                    closeComplaintModal();
                    // Reset form to editable for next use
                    resetComplaintForm();
                    // Remove success message
                    removeComplaintFormMessage();
                }, 3000);
            } else {
                showComplaintFormMessage(data.message || 'Submission failed', 'danger');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showComplaintFormMessage('Something went wrong. Please try again.', 'danger');
        })
        .finally(() => {
            // Restore button
            submitBtn.innerHTML = originalText;
            submitBtn.disabled = false;
        });
}


// site.js - 11 digit limit
document.addEventListener('input', function (e) {
    if (e.target.classList.contains('bd-phone')) {
        // Keep only digits and limit to 11
        e.target.value = e.target.value.replace(/\D/g, '').substring(0, 11);
    }
});

// Validation on form submit
document.addEventListener('submit', function (e) {
    e.target.querySelectorAll('.bd-phone').forEach(input => {
        const phone = input.value;
        if (phone.length === 11 && !/^01[3-9]/.test(phone)) {
            e.preventDefault();
            input.classList.add('is-invalid');
            input.focus();
        }
    });
});


// =============================================
// MOBILE BOTTOM NAVIGATION FUNCTIONS
// =============================================

let lastScrollTop = 0;
let navVisible = true;
let scrollTimeout;

// Initialize mobile bottom nav
function initMobileBottomNav() {
    // Only run on mobile and for specific user types
    if (window.innerWidth > 991) return;

    const userType = getCurrentUserType();
    if (!['Patient', 'Doctor', 'Nurse'].includes(userType)) {
        // Hide bottom nav for Admin and non-logged in users
        const bottomNav = document.getElementById('mobileBottomNav');
        if (bottomNav) {
            bottomNav.style.display = 'none';
        }
        return;
    }

    // Show bottom nav
    const bottomNav = document.getElementById('mobileBottomNav');
    if (bottomNav) {
        bottomNav.style.display = 'block';
        bottomNav.classList.remove('hide');
        bottomNav.classList.add('show');
    }

    // Set active menu item based on current page
    setActiveMobileMenuItem();

    // Setup scroll behavior
    setupMobileNavScrollBehavior();

    // Setup click handlers
    setupMobileNavClickHandlers();

    // Setup more menu
    setupMobileMoreMenu();
}

// Get current user type from session
function getCurrentUserType() {
    try {
        // Try to get from data attribute
        const userTypeElement = document.querySelector('[data-user-type]');
        if (userTypeElement) {
            return userTypeElement.dataset.userType;
        }

        // Try to get from sessionStorage
        const sessionUserType = sessionStorage.getItem('userType');
        if (sessionUserType) {
            return sessionUserType;
        }

        // Check URL path
        const path = window.location.pathname;
        if (path.includes('/Patient/')) return 'Patient';
        if (path.includes('/Doctor/')) return 'Doctor';
        if (path.includes('/Nurse/')) return 'Nurse';
        if (path.includes('/Admin/')) return 'Admin';

        return null;
    } catch (error) {
        console.log('Error getting user type:', error);
        return null;
    }
}

// Set active menu item based on current URL
function setActiveMobileMenuItem() {
    const currentPath = window.location.pathname.toLowerCase();
    const menuItems = document.querySelectorAll('.mobile-nav-link');

    menuItems.forEach(item => {
        item.classList.remove('active');
        const parentItem = item.closest('.mobile-nav-item');
        if (parentItem) {
            parentItem.classList.remove('active');
        }

        const href = item.getAttribute('href')?.toLowerCase();
        if (href && currentPath.includes(href.replace('/patient/', '')
            .replace('/doctor/', '')
            .replace('/nurse/', ''))) {
            item.classList.add('active');
            if (parentItem) {
                parentItem.classList.add('active');
            }
        }
    });
}

// Setup scroll behavior to hide/show nav
function setupMobileNavScrollBehavior() {
    window.addEventListener('scroll', function () {
        const bottomNav = document.getElementById('mobileBottomNav');
        if (!bottomNav) return;

        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
        const scrollDelta = 10; // Minimum scroll delta to trigger

        // Clear previous timeout
        clearTimeout(scrollTimeout);

        if (Math.abs(scrollTop - lastScrollTop) <= scrollDelta) {
            return;
        }

        // Scrolling down - hide nav
        if (scrollTop > lastScrollTop && scrollTop > 100) {
            if (navVisible) {
                bottomNav.classList.remove('show');
                bottomNav.classList.add('hide');
                navVisible = false;
            }
        }
        // Scrolling up - show nav
        else {
            if (!navVisible) {
                bottomNav.classList.remove('hide');
                bottomNav.classList.add('show');
                navVisible = true;
            }
        }

        lastScrollTop = scrollTop;

        // Hide nav again after 3 seconds of inactivity
        scrollTimeout = setTimeout(function () {
            if (navVisible && scrollTop > 300) {
                bottomNav.classList.remove('show');
                bottomNav.classList.add('hide');
                navVisible = false;
            }
        }, 3000);
    });
}

// Setup click handlers for menu items
function setupMobileNavClickHandlers() {
    // Add smooth scroll to top when clicking active item
    const menuItems = document.querySelectorAll('.mobile-nav-link');

    menuItems.forEach(item => {
        item.addEventListener('click', function (e) {
            const href = this.getAttribute('href');
            const currentPath = window.location.pathname;

            // If clicking on current page link, scroll to top
            if (href && currentPath.includes(href)) {
                e.preventDefault();
                window.scrollTo({
                    top: 0,
                    behavior: 'smooth'
                });

                // Add active class
                menuItems.forEach(i => i.classList.remove('active'));
                this.classList.add('active');
            }

            // Hide more menu if open
            const moreMenu = document.getElementById('mobileMoreMenu');
            if (moreMenu) {
                moreMenu.classList.remove('show');
            }
        });
    });
}

// Setup more menu functionality
function setupMobileMoreMenu() {
    const moreBtn = document.getElementById('mobileMoreBtn');
    const moreMenu = document.getElementById('mobileMoreMenu');

    if (!moreBtn || !moreMenu) return;

    // Toggle more menu
    moreBtn.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        moreMenu.classList.toggle('show');
    });

    // Close more menu when clicking outside
    document.addEventListener('click', function (e) {
        if (moreMenu.classList.contains('show') &&
            !moreMenu.contains(e.target) &&
            !moreBtn.contains(e.target)) {
            moreMenu.classList.remove('show');
        }
    });

    // Close on escape key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && moreMenu.classList.contains('show')) {
            moreMenu.classList.remove('show');
        }
    });
}

// Update badge counts dynamically
function updateMobileNavBadges(counts) {
    if (!counts) return;

    // Update appointment badge
    if (counts.appointments && counts.appointments > 0) {
        const appointmentBadge = document.getElementById('mobileAppointmentBadge');
        if (appointmentBadge) {
            appointmentBadge.textContent = counts.appointments;
            appointmentBadge.style.display = 'flex';
        }
    }

    // Update report badge
    if (counts.reports && counts.reports > 0) {
        const reportBadge = document.getElementById('mobileReportBadge');
        if (reportBadge) {
            reportBadge.textContent = counts.reports;
            reportBadge.style.display = 'flex';
        }
    }
}

// Fetch and update badge counts
function fetchMobileBadgeCounts() {
    const userType = getCurrentUserType();

    if (!userType) return;

    let endpoint = '';
    switch (userType) {
        case 'Patient':
            endpoint = '/Patient/GetBadgeCounts';
            break;
        case 'Doctor':
            endpoint = '/Doctor/GetBadgeCounts';
            break;
        case 'Nurse':
            endpoint = '/Nurse/GetBadgeCounts';
            break;
        default:
            return;
    }

    fetch(endpoint)
        .then(response => {
            if (!response.ok) throw new Error('Network response error');
            return response.json();
        })
        .then(data => {
            if (data.success) {
                updateMobileNavBadges(data);
            }
        })
        .catch(error => {
            console.log('Could not fetch badge counts:', error);
        });
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    // Initialize mobile bottom nav
    initMobileBottomNav();

    // Fetch badge counts after 1 second delay
    setTimeout(fetchMobileBadgeCounts, 1000);

    // Re-initialize on window resize
    window.addEventListener('resize', function () {
        initMobileBottomNav();
    });
});

// Also initialize after page fully loads
window.addEventListener('load', function () {
    // Small delay to ensure everything is loaded
    setTimeout(initMobileBottomNav, 500);
});

