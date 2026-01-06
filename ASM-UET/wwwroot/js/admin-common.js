/**
 * ASM UET Admin Common JavaScript Utilities
 * Shared functionality across all admin pages
 */

class AdminUtils {
    constructor() {
        this.currentPage = this.getCurrentPage();
        this.init();
    }

    init() {
        this.setupMobileMenu();
        this.setupSidebarHighlighting();
        this.setupFormValidation();
        this.setupLoadingStates();
    }

    // ==================== NAVIGATION & MENU ====================

    getCurrentPage() {
        const path = window.location.pathname;
        const segments = path.split('/').filter(s => s);
        return segments[segments.length - 1]?.toLowerCase() || 'index';
    }

    setupMobileMenu() {
        const mobileMenuBtn = document.getElementById('mobileMenuBtn');
        const sidebar = document.getElementById('sidebar');
        const mobileOverlay = document.getElementById('mobileOverlay');

        if (!mobileMenuBtn || !sidebar || !mobileOverlay) return;

        const toggleMobileMenu = () => {
            sidebar.classList.toggle('-translate-x-full');
            mobileOverlay.classList.toggle('hidden');
        };

        mobileMenuBtn.addEventListener('click', toggleMobileMenu);
        mobileOverlay.addEventListener('click', toggleMobileMenu);

        // Close menu on window resize
        window.addEventListener('resize', () => {
            if (window.innerWidth >= 1024) {
                sidebar.classList.remove('-translate-x-full');
                mobileOverlay.classList.add('hidden');
            }
        });
    }

    setupSidebarHighlighting() {
        const navLinks = document.querySelectorAll('nav a');
        const currentPage = this.currentPage;

        navLinks.forEach(link => {
            const href = link.getAttribute('href');
            if (!href) return;

            // Remove existing active classes
            link.classList.remove('bg-white', 'bg-opacity-20', 'backdrop-blur-sm');
            link.classList.add('hover:bg-white', 'hover:bg-opacity-10');

            // Check if this link matches current page
            const isActive = this.isLinkActive(href, currentPage);

            if (isActive) {
                link.classList.remove('hover:bg-white', 'hover:bg-opacity-10');
                link.classList.add('bg-white', 'bg-opacity-20', 'backdrop-blur-sm', 'hover:bg-opacity-30');
            }
        });
    }

    isLinkActive(href, currentPage) {
        const linkSegments = href.split('/').filter(s => s);
        const linkPage = linkSegments[linkSegments.length - 1]?.toLowerCase();

        // Direct match
        if (linkPage === currentPage) return true;

        // Special cases
        if (currentPage === 'index' && (linkPage === 'admin' || href.includes('/Admin') || href.includes('Index'))) return true;
        if (currentPage === 'courses' && linkPage === 'courses') return true;
        if (currentPage === 'users' && linkPage === 'users') return true;
        if (currentPage === 'reports' && linkPage === 'reports') return true;

        return false;
    }

    // ==================== MODAL UTILITIES ====================

    createModal(config) {
        const {
            id,
            title,
            content,
            buttons = [],
            size = 'md',
            headerGradient = 'from-indigo-600 to-purple-600'
        } = config;

        const sizeClasses = {
            sm: 'max-w-sm',
            md: 'max-w-md', 
            lg: 'max-w-lg',
            xl: 'max-w-xl',
            '2xl': 'max-w-2xl'
        };

        const modal = document.createElement('div');
        modal.id = id;
        modal.className = 'fixed inset-0 z-50 hidden items-center justify-center p-4';

        modal.innerHTML = `
            <div class="modal-backdrop absolute inset-0" onclick="AdminUtils.closeModal('${id}')"></div>
            <div class="glass relative bg-white rounded-2xl shadow-2xl ${sizeClasses[size]} w-full modal-content">
                <div class="bg-gradient-to-r ${headerGradient} text-white px-6 py-4 rounded-t-2xl">
                    <h3 class="text-xl font-bold flex items-center space-x-2">
                        ${title}
                    </h3>
                </div>
                <div class="p-6">
                    ${content}
                </div>
                ${buttons.length > 0 ? `
                    <div class="flex space-x-3 px-6 pb-6">
                        ${buttons.map(btn => `
                            <button type="button" 
                                    class="${btn.classes}" 
                                    onclick="${btn.onclick || ''}">
                                ${btn.text}
                            </button>
                        `).join('')}
                    </div>
                ` : ''}
            </div>
        `;

        document.body.appendChild(modal);
        return modal;
    }

    static showModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.classList.remove('hidden');
            modal.classList.add('flex');
        }
    }

    static closeModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.classList.add('hidden');
            modal.classList.remove('flex');
        }
    }

    // ==================== FORM VALIDATION ====================

    setupFormValidation() {
        // Enhanced form validation with consistent styling
        document.addEventListener('invalid', (e) => {
            e.target.classList.add('border-red-500', 'focus:ring-red-500');
            e.target.classList.remove('border-gray-300', 'focus:ring-indigo-500');
        }, true);

        document.addEventListener('input', (e) => {
            if (e.target.validity.valid) {
                e.target.classList.remove('border-red-500', 'focus:ring-red-500');
                e.target.classList.add('border-gray-300', 'focus:ring-indigo-500');
            }
        });
    }

    static setFormLoading(formElement, isLoading = true) {
        const submitBtns = formElement.querySelectorAll('button[type="submit"]');
        const inputs = formElement.querySelectorAll('input, select, textarea');

        submitBtns.forEach(btn => {
            if (isLoading) {
                btn.disabled = true;
                btn.dataset.originalText = btn.innerHTML;
                btn.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Processing...';
            } else {
                btn.disabled = false;
                btn.innerHTML = btn.dataset.originalText || btn.innerHTML;
            }
        });

        inputs.forEach(input => {
            input.disabled = isLoading;
        });
    }

    static showFieldError(fieldElement, message) {
        // Remove existing error
        AdminUtils.clearFieldError(fieldElement);

        // Add error styling
        fieldElement.classList.add('border-red-500', 'focus:ring-red-500');
        fieldElement.classList.remove('border-gray-300', 'focus:ring-indigo-500');

        // Create error message element
        const errorElement = document.createElement('p');
        errorElement.className = 'text-red-600 text-sm mt-1';
        errorElement.textContent = message;
        errorElement.id = `${fieldElement.id}-error`;

        // Insert after field
        fieldElement.parentNode.insertBefore(errorElement, fieldElement.nextSibling);
    }

    static clearFieldError(fieldElement) {
        // Remove error styling
        fieldElement.classList.remove('border-red-500', 'focus:ring-red-500');
        fieldElement.classList.add('border-gray-300', 'focus:ring-indigo-500');

        // Remove error message
        const errorElement = document.getElementById(`${fieldElement.id}-error`);
        if (errorElement) {
            errorElement.remove();
        }
    }

    // ==================== LOADING STATES ====================

    setupLoadingStates() {
        // Add loading skeleton CSS if not already present
        if (!document.getElementById('admin-loading-styles')) {
            const style = document.createElement('style');
            style.id = 'admin-loading-styles';
            style.textContent = `
                .skeleton {
                    animation: skeleton-loading 1.5s ease-in-out infinite alternate;
                    background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
                }

                @keyframes skeleton-loading {
                    0% { opacity: 1; }
                    100% { opacity: 0.4; }
                }

                .skeleton-text {
                    height: 1rem;
                    margin: 0.25rem 0;
                    border-radius: 0.25rem;
                }

                .skeleton-avatar {
                    width: 2.5rem;
                    height: 2.5rem;
                    border-radius: 50%;
                }

                .skeleton-button {
                    height: 2.5rem;
                    border-radius: 0.5rem;
                }
            `;
            document.head.appendChild(style);
        }
    }

    static showTableSkeleton(tableBodyId, columns = 5, rows = 5) {
        const tbody = document.getElementById(tableBodyId);
        if (!tbody) return;

        const skeletonRows = Array.from({ length: rows }, (_, rowIndex) => {
            const cells = Array.from({ length: columns }, (_, colIndex) => {
                if (colIndex === 0) {
                    return `
                        <td class="px-6 py-4">
                            <div class="flex items-center space-x-3">
                                <div class="skeleton skeleton-avatar"></div>
                                <div class="skeleton skeleton-text w-32"></div>
                            </div>
                        </td>
                    `;
                } else if (colIndex === columns - 1) {
                    return `
                        <td class="px-6 py-4">
                            <div class="flex space-x-2">
                                <div class="skeleton skeleton-button w-16"></div>
                                <div class="skeleton skeleton-button w-16"></div>
                            </div>
                        </td>
                    `;
                } else {
                    return `<td class="px-6 py-4"><div class="skeleton skeleton-text w-24"></div></td>`;
                }
            }).join('');

            return `<tr>${cells}</tr>`;
        }).join('');

        tbody.innerHTML = skeletonRows;
    }

    static hideTableSkeleton(tableBodyId) {
        const tbody = document.getElementById(tableBodyId);
        if (tbody) {
            tbody.innerHTML = '';
        }
    }

    // ==================== API UTILITIES ====================

    static async makeApiCall(url, options = {}) {
        try {
            const response = await fetch(url, {
                headers: {
                    'Content-Type': 'application/json',
                    ...options.headers
                },
                ...options
            });

            const data = await response.json();

            if (!data.success) {
                throw new Error(data.error || 'Request failed');
            }

            return data;
        } catch (error) {
            console.error('API call failed:', error);
            showError(error.message || 'An error occurred');
            throw error;
        }
    }

    // ==================== UTILITY FUNCTIONS ====================

    static formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    }

    static formatDateTime(dateString) {
        const date = new Date(dateString);
        return date.toLocaleString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    static debounce(func, delay) {
        let timeoutId;
        return function (...args) {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => func.apply(this, args), delay);
        };
    }

    static copyToClipboard(text) {
        navigator.clipboard.writeText(text).then(() => {
            showSuccess('Copied to clipboard');
        }).catch(() => {
            showError('Failed to copy to clipboard');
        });
    }
}

// Initialize admin utilities when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.adminUtils = new AdminUtils();
});

// Export utilities for global use
window.AdminUtils = AdminUtils;