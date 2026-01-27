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

// ============== MOBILE DASHBOARD MENU FUNCTIONS ==============

// Mobile dashboard menu toggle functionality
function setupMobileDashboardMenu() {
    const mobileToggle = document.getElementById('mobileDashboardToggle');
    const mobileSidebar = document.getElementById('mobileDashboardSidebar');
    const mobileOverlay = document.getElementById('mobileDashboardOverlay');
    const mobileClose = document.getElementById('mobileSidebarClose');

    if (!mobileToggle || !mobileSidebar) return;

    // Open mobile sidebar
    mobileToggle.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();

        // Show sidebar
        mobileSidebar.classList.add('open');

        // Show overlay
        if (mobileOverlay) {
            mobileOverlay.style.display = 'block';
            setTimeout(() => {
                mobileOverlay.style.opacity = '1';
            }, 10);
        }

        // Disable body scroll
        document.body.style.overflow = 'hidden';
    });

    // Close mobile sidebar
    if (mobileClose) {
        mobileClose.addEventListener('click', closeMobileDashboardMenu);
    }

    // Close on overlay click
    if (mobileOverlay) {
        mobileOverlay.addEventListener('click', closeMobileDashboardMenu);
    }

    // Close on ESC key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && mobileSidebar.classList.contains('open')) {
            closeMobileDashboardMenu();
        }
    });
}

function closeMobileDashboardMenu() {
    const mobileSidebar = document.getElementById('mobileDashboardSidebar');
    const mobileOverlay = document.getElementById('mobileDashboardOverlay');

    if (mobileSidebar) {
        mobileSidebar.classList.remove('open');
    }

    if (mobileOverlay) {
        mobileOverlay.style.opacity = '0';
        setTimeout(() => {
            mobileOverlay.style.display = 'none';
        }, 300);
    }

    // Enable body scroll
    document.body.style.overflow = 'auto';
}

// Call this function when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    setupMobileDashboardMenu();

    // Load patient stats for mobile menu
    loadMobilePatientStats();
});

// Load patient stats for mobile menu
function loadMobilePatientStats() {
    // Only load if we're on patient dashboard
    if (!document.querySelector('#patientDashboard')) return;

    fetch('/Patient/GetPatientStats')
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                // Update mobile stats
                const mobileAppointments = document.getElementById('mobileTotalAppointments');
                const mobileReports = document.getElementById('mobileTotalReports');

                if (mobileAppointments) {
                    mobileAppointments.textContent = data.totalAppointments || 0;
                }
                if (mobileReports) {
                    mobileReports.textContent = data.totalReports || 0;
                }
            }
        })
        .catch(error => {
            console.log('Could not load patient stats:', error);
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