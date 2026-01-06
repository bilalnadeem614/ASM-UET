/**
 * ASM UET Admin Notification Toast System
 * Provides success, error, info, and warning notifications
 * Auto-dismiss after 3 seconds, positioned top-right
 */

class NotificationToast {
    constructor() {
        this.container = this.createContainer();
        this.notifications = [];
    }

    createContainer() {
        const container = document.createElement('div');
        container.id = 'notification-container';
        container.className = 'fixed top-4 right-4 z-50 space-y-2';
        container.style.maxWidth = '400px';
        document.body.appendChild(container);
        return container;
    }

    show(message, type = 'info', duration = 3000) {
        const notification = this.createNotification(message, type);
        this.container.appendChild(notification);
        this.notifications.push(notification);

        // Animate in
        setTimeout(() => {
            notification.classList.add('notification-show');
        }, 10);

        // Auto dismiss
        setTimeout(() => {
            this.dismiss(notification);
        }, duration);

        return notification;
    }

    createNotification(message, type) {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type} transform translate-x-full opacity-0 transition-all duration-300 ease-out`;
        
        const config = this.getTypeConfig(type);
        
        notification.innerHTML = `
            <div class="flex items-center p-4 mb-2 rounded-lg shadow-lg backdrop-blur-sm ${config.bgClass} ${config.textClass} border ${config.borderClass}">
                <div class="flex items-center">
                    <div class="flex-shrink-0">
                        <div class="w-8 h-8 rounded-full ${config.iconBg} flex items-center justify-center">
                            <i class="${config.icon} text-sm"></i>
                        </div>
                    </div>
                    <div class="ml-3 flex-1">
                        <p class="text-sm font-medium">${message}</p>
                    </div>
                    <div class="ml-4 flex-shrink-0">
                        <button class="inline-flex ${config.textClass} hover:opacity-75 transition-opacity" onclick="notifications.dismiss(this.closest('.notification'))">
                            <i class="fas fa-times text-sm"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;

        return notification;
    }

    getTypeConfig(type) {
        const configs = {
            success: {
                bgClass: 'bg-green-50',
                textClass: 'text-green-800',
                borderClass: 'border-green-200',
                iconBg: 'bg-green-500 text-white',
                icon: 'fas fa-check'
            },
            error: {
                bgClass: 'bg-red-50',
                textClass: 'text-red-800', 
                borderClass: 'border-red-200',
                iconBg: 'bg-red-500 text-white',
                icon: 'fas fa-exclamation-circle'
            },
            warning: {
                bgClass: 'bg-yellow-50',
                textClass: 'text-yellow-800',
                borderClass: 'border-yellow-200', 
                iconBg: 'bg-yellow-500 text-white',
                icon: 'fas fa-exclamation-triangle'
            },
            info: {
                bgClass: 'bg-blue-50',
                textClass: 'text-blue-800',
                borderClass: 'border-blue-200',
                iconBg: 'bg-blue-500 text-white', 
                icon: 'fas fa-info-circle'
            }
        };

        return configs[type] || configs.info;
    }

    dismiss(notification) {
        if (!notification) return;

        notification.classList.remove('notification-show');
        notification.classList.add('translate-x-full', 'opacity-0');

        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
            
            const index = this.notifications.indexOf(notification);
            if (index > -1) {
                this.notifications.splice(index, 1);
            }
        }, 300);
    }

    dismissAll() {
        this.notifications.forEach(notification => {
            this.dismiss(notification);
        });
    }

    // Convenience methods
    success(message, duration = 3000) {
        return this.show(message, 'success', duration);
    }

    error(message, duration = 4000) {
        return this.show(message, 'error', duration);
    }

    warning(message, duration = 3500) {
        return this.show(message, 'warning', duration);
    }

    info(message, duration = 3000) {
        return this.show(message, 'info', duration);
    }
}

// Create global instance
const notifications = new NotificationToast();

// Add CSS for animations
const style = document.createElement('style');
style.textContent = `
    .notification-show {
        transform: translateX(0) !important;
        opacity: 1 !important;
    }
    
    .notification {
        max-width: 400px;
        word-wrap: break-word;
    }
    
    @media (max-width: 640px) {
        #notification-container {
            left: 1rem;
            right: 1rem;
            top: 1rem;
        }
        
        .notification {
            max-width: none;
        }
    }
`;
document.head.appendChild(style);

// Global convenience functions for backward compatibility
window.showSuccess = (message, duration) => notifications.success(message, duration);
window.showError = (message, duration) => notifications.error(message, duration);
window.showWarning = (message, duration) => notifications.warning(message, duration);
window.showInfo = (message, duration) => notifications.info(message, duration);