/**
 * Admin Dashboard JavaScript Module
 * Handles all admin panel operations including dashboard stats, course management, and API interactions
 */

// Global state management
const AdminState = {
    courses: [],
    teachers: [],
    deleteTargetId: null,
    currentCourseId: null
};

// API Configuration
const API_BASE_URL = '/api/admin';

// Utility Functions
const Utils = {
    /**
     * Get JWT token from cookie
     */
    getAuthToken() {
        const name = 'ASM_TOKEN=';
        const decodedCookie = decodeURIComponent(document.cookie);
        const cookieArray = decodedCookie.split(';');
        
        for (let cookie of cookieArray) {
            cookie = cookie.trim();
            if (cookie.indexOf(name) === 0) {
                return cookie.substring(name.length, cookie.length);
            }
        }
        return null;
    },

    /**
     * Create fetch options with JWT token
     */
    createFetchOptions(method = 'GET', body = null) {
        const options = {
            method,
            headers: {
                'Content-Type': 'application/json'
            }
        };

        const token = this.getAuthToken();
        if (token) {
            options.headers['Authorization'] = `Bearer ${token}`;
        }

        if (body) {
            options.body = JSON.stringify(body);
        }

        return options;
    },

    /**
     * Handle API errors consistently
     */
    async handleApiError(response) {
        if (!response.ok) {
            const error = await response.json().catch(() => ({ error: 'Unknown error occurred' }));
            throw new Error(error.details || error.error || `HTTP ${response.status}: ${response.statusText}`);
        }
        return response;
    },

    /**
     * Format date for display
     */
    formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
    },

    /**
     * Format time for display
     */
    formatTime(dateString) {
        const date = new Date(dateString);
        return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
    }
};

// Notification System
const NotificationManager = {
    /**
     * Show notification toast
     * @param {string} message - Message to display
     * @param {string} type - 'success', 'error', 'warning', 'info'
     */
    show(message, type = 'info') {
        // Remove existing notifications
        const existing = document.getElementById('notification-toast');
        if (existing) {
            existing.remove();
        }

        // Create notification element
        const notification = document.createElement('div');
        notification.id = 'notification-toast';
        notification.className = 'fixed top-4 right-4 z-[60] transition-all duration-300 transform translate-x-0';
        
        const colors = {
            success: 'from-green-500 to-green-600',
            error: 'from-red-500 to-red-600',
            warning: 'from-yellow-500 to-yellow-600',
            info: 'from-blue-500 to-blue-600'
        };

        const icons = {
            success: 'fa-check-circle',
            error: 'fa-exclamation-circle',
            warning: 'fa-exclamation-triangle',
            info: 'fa-info-circle'
        };

        notification.innerHTML = `
            <div class="bg-gradient-to-r ${colors[type]} text-white px-6 py-4 rounded-lg shadow-2xl flex items-center space-x-3 min-w-[300px] max-w-md">
                <i class="fas ${icons[type]} text-2xl"></i>
                <div class="flex-1">
                    <p class="font-medium">${message}</p>
                </div>
                <button onclick="this.parentElement.parentElement.remove()" class="text-white hover:text-gray-200 transition-colors">
                    <i class="fas fa-times"></i>
                </button>
            </div>
        `;

        document.body.appendChild(notification);

        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (notification.parentElement) {
                notification.style.transform = 'translateX(400px)';
                notification.style.opacity = '0';
                setTimeout(() => notification.remove(), 300);
            }
        }, 5000);
    },

    success(message) {
        this.show(message, 'success');
    },

    error(message) {
        this.show(message, 'error');
    },

    warning(message) {
        this.show(message, 'warning');
    },

    info(message) {
        this.show(message, 'info');
    }
};

// Expose as global function for backward compatibility
window.showNotification = (message, type) => NotificationManager.show(message, type);

// Dashboard API
const DashboardAPI = {
    /**
     * Load dashboard statistics
     */
    async loadDashboardStats() {
        try {
            const response = await fetch(`${API_BASE_URL}/dashboard/stats`, 
                Utils.createFetchOptions('GET'));
            
            await Utils.handleApiError(response);
            const stats = await response.json();
            
            this.updateDashboardDOM(stats);
            return stats;
        } catch (error) {
            console.error('Error loading dashboard stats:', error);
            NotificationManager.error(`Failed to load dashboard: ${error.message}`);
            throw error;
        }
    },

    /**
     * Update dashboard DOM with stats
     */
    updateDashboardDOM(stats) {
        // Update stat cards
        const totalCoursesEl = document.querySelector('[data-stat="total-courses"]');
        const totalTeachersEl = document.querySelector('[data-stat="total-teachers"]');
        const totalStudentsEl = document.querySelector('[data-stat="total-students"]');
        const recentEnrollmentsEl = document.querySelector('[data-stat="recent-enrollments"]');

        if (totalCoursesEl) totalCoursesEl.textContent = stats.totalCourses;
        if (totalTeachersEl) totalTeachersEl.textContent = stats.totalTeachers;
        if (totalStudentsEl) totalStudentsEl.textContent = stats.totalStudents;
        if (recentEnrollmentsEl) recentEnrollmentsEl.textContent = stats.recentEnrollments.length;

        // Update recent activity list
        const activityContainer = document.getElementById('recent-activity-list');
        if (activityContainer && stats.recentEnrollments) {
            this.renderRecentActivity(activityContainer, stats.recentEnrollments);
        }
    },

    /**
     * Render recent activity items
     */
    renderRecentActivity(container, enrollments) {
        if (enrollments.length === 0) {
            container.innerHTML = `
                <div class="text-center py-12">
                    <i class="fas fa-inbox text-6xl text-gray-300 mb-4"></i>
                    <p class="text-gray-500">No recent enrollments found</p>
                </div>
            `;
            return;
        }

        container.innerHTML = enrollments.map(enrollment => `
            <div class="flex items-center space-x-4 p-4 bg-gray-50 hover:bg-gray-100 rounded-xl transition-all">
                <div class="flex-shrink-0">
                    <div class="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-full flex items-center justify-center">
                        <i class="fas fa-user-graduate text-white"></i>
                    </div>
                </div>
                <div class="flex-1 min-w-0">
                    <p class="font-medium text-gray-900 truncate">${enrollment.studentName}</p>
                    <p class="text-sm text-gray-500 truncate">
                        Enrolled in <span class="font-medium text-indigo-600">${enrollment.courseName}</span>
                    </p>
                </div>
                <div class="flex-shrink-0 text-right">
                    <p class="text-sm text-gray-500">${Utils.formatDate(enrollment.enrollmentDate)}</p>
                    <p class="text-xs text-gray-400">${Utils.formatTime(enrollment.enrollmentDate)}</p>
                </div>
            </div>
        `).join('');
    }
};

// Course Management API
const CourseAPI = {
    /**
     * Load all courses
     */
    async loadCourses() {
        try {
            const response = await fetch(`${API_BASE_URL}/courses`, 
                Utils.createFetchOptions('GET'));
            
            await Utils.handleApiError(response);
            AdminState.courses = await response.json();
            
            this.renderCoursesTable(AdminState.courses);
            return AdminState.courses;
        } catch (error) {
            console.error('Error loading courses:', error);
            NotificationManager.error(`Failed to load courses: ${error.message}`);
            throw error;
        }
    },

    /**
     * Load all teachers for dropdown
     */
    async loadTeachers() {
        try {
            const response = await fetch(`${API_BASE_URL}/teachers`, 
                Utils.createFetchOptions('GET'));
            
            await Utils.handleApiError(response);
            AdminState.teachers = await response.json();
            
            this.populateTeacherDropdown();
            return AdminState.teachers;
        } catch (error) {
            console.error('Error loading teachers:', error);
            NotificationManager.error(`Failed to load teachers: ${error.message}`);
            throw error;
        }
    },

    /**
     * Populate teacher dropdown
     */
    populateTeacherDropdown() {
        const dropdown = document.getElementById('teacherId');
        if (!dropdown) return;

        dropdown.innerHTML = '<option value="">Select a teacher...</option>';
        AdminState.teachers.forEach(teacher => {
            const option = document.createElement('option');
            option.value = teacher.userId;
            option.textContent = teacher.fullName;
            dropdown.appendChild(option);
        });
    },

    /**
     * Render courses table
     */
    renderCoursesTable(courses) {
        const tbody = document.getElementById('coursesTableBody');
        if (!tbody) return;

        if (courses.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="4" class="px-6 py-12 text-center">
                        <i class="fas fa-folder-open text-6xl text-gray-300 mb-4"></i>
                        <p class="text-gray-500">No courses found</p>
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = courses.map((course, index) => `
            <tr class="${index % 2 === 0 ? 'bg-white' : 'bg-gray-50'} hover:bg-indigo-50 transition-colors">
                <td class="px-6 py-4 whitespace-nowrap">
                    <span class="font-semibold text-indigo-600">${this.escapeHtml(course.courseCode)}</span>
                </td>
                <td class="px-6 py-4">
                    <span class="font-medium text-gray-900">${this.escapeHtml(course.courseName)}</span>
                </td>
                <td class="px-6 py-4">
                    <div class="flex items-center space-x-2">
                        <div class="w-8 h-8 bg-gradient-to-br from-green-500 to-green-600 rounded-full flex items-center justify-center">
                            <i class="fas fa-chalkboard-teacher text-white text-xs"></i>
                        </div>
                        <span class="text-gray-700">${this.escapeHtml(course.teacherName)}</span>
                    </div>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-center">
                    <div class="flex items-center justify-center space-x-2">
                        <button onclick="CourseManager.openEditCourseModal(${course.courseId})" 
                                class="px-4 py-2 bg-blue-100 text-blue-600 hover:bg-blue-200 rounded-lg transition-all flex items-center space-x-1">
                            <i class="fas fa-edit"></i>
                            <span class="hidden sm:inline">Edit</span>
                        </button>
                        <button onclick="CourseManager.confirmDeleteCourse(${course.courseId}, '${this.escapeHtml(course.courseName)}')" 
                                class="px-4 py-2 bg-red-100 text-red-600 hover:bg-red-200 rounded-lg transition-all flex items-center space-x-1">
                            <i class="fas fa-trash"></i>
                            <span class="hidden sm:inline">Delete</span>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');
    },

    /**
     * Escape HTML to prevent XSS
     */
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    },

    /**
     * Filter courses by search term
     */
    filterCourses(searchTerm) {
        const term = searchTerm.toLowerCase();
        const filtered = AdminState.courses.filter(course => 
            course.courseCode.toLowerCase().includes(term) ||
            course.courseName.toLowerCase().includes(term) ||
            course.teacherName.toLowerCase().includes(term)
        );
        this.renderCoursesTable(filtered);
    },

    /**
     * Create a new course
     */
    async createCourse(courseData) {
        try {
            const response = await fetch(`${API_BASE_URL}/courses`, 
                Utils.createFetchOptions('POST', courseData));
            
            await Utils.handleApiError(response);
            const newCourse = await response.json();
            
            NotificationManager.success('Course created successfully!');
            return newCourse;
        } catch (error) {
            console.error('Error creating course:', error);
            NotificationManager.error(`Failed to create course: ${error.message}`);
            throw error;
        }
    },

    /**
     * Update an existing course
     */
    async updateCourse(courseId, courseData) {
        try {
            courseData.courseId = courseId;
            const response = await fetch(`${API_BASE_URL}/courses/${courseId}`, 
                Utils.createFetchOptions('PUT', courseData));
            
            await Utils.handleApiError(response);
            const updatedCourse = await response.json();
            
            NotificationManager.success('Course updated successfully!');
            return updatedCourse;
        } catch (error) {
            console.error('Error updating course:', error);
            NotificationManager.error(`Failed to update course: ${error.message}`);
            throw error;
        }
    },

    /**
     * Delete a course
     */
    async deleteCourse(courseId) {
        try {
            const response = await fetch(`${API_BASE_URL}/courses/${courseId}`, 
                Utils.createFetchOptions('DELETE'));
            
            await Utils.handleApiError(response);
            
            NotificationManager.success('Course deleted successfully!');
            return true;
        } catch (error) {
            console.error('Error deleting course:', error);
            
            if (error.message.includes('enrollments')) {
                NotificationManager.error('Cannot delete course with existing enrollments');
            } else {
                NotificationManager.error(`Failed to delete course: ${error.message}`);
            }
            throw error;
        }
    }
};

// Course Manager (UI Logic)
const CourseManager = {
    /**
     * Open add course modal
     */
    async openAddCourseModal() {
        // Load teachers if not already loaded
        if (AdminState.teachers.length === 0) {
            await CourseAPI.loadTeachers();
        }

        AdminState.currentCourseId = null;
        
        const modalTitle = document.getElementById('modalTitle');
        const courseForm = document.getElementById('courseForm');
        const courseIdInput = document.getElementById('courseId');
        const modalError = document.getElementById('modalError');

        if (modalTitle) {
            modalTitle.innerHTML = '<i class="fas fa-plus mr-2"></i>Add New Course';
        }
        
        if (courseIdInput) {
            courseIdInput.value = '';
        }
        
        if (courseForm) {
            courseForm.reset();
        }
        
        if (modalError) {
            modalError.classList.add('hidden');
        }

        this.showModal('courseModal');
    },

    /**
     * Open edit course modal
     */
    async openEditCourseModal(courseId) {
        // Load teachers if not already loaded
        if (AdminState.teachers.length === 0) {
            await CourseAPI.loadTeachers();
        }

        const course = AdminState.courses.find(c => c.courseId === courseId);
        if (!course) {
            NotificationManager.error('Course not found');
            return;
        }

        AdminState.currentCourseId = courseId;

        const modalTitle = document.getElementById('modalTitle');
        const courseIdInput = document.getElementById('courseId');
        const courseCodeInput = document.getElementById('courseCode');
        const courseNameInput = document.getElementById('courseName');
        const teacherIdSelect = document.getElementById('teacherId');
        const modalError = document.getElementById('modalError');

        if (modalTitle) {
            modalTitle.innerHTML = '<i class="fas fa-edit mr-2"></i>Edit Course';
        }
        
        if (courseIdInput) courseIdInput.value = course.courseId;
        if (courseCodeInput) courseCodeInput.value = course.courseCode;
        if (courseNameInput) courseNameInput.value = course.courseName;
        if (teacherIdSelect) teacherIdSelect.value = course.teacherId;
        
        if (modalError) {
            modalError.classList.add('hidden');
        }

        this.showModal('courseModal');
    },

    /**
     * Save course (create or update)
     */
    async saveCourse(event) {
        event.preventDefault();

        const courseCodeInput = document.getElementById('courseCode');
        const courseNameInput = document.getElementById('courseName');
        const teacherIdSelect = document.getElementById('teacherId');
        const submitBtn = document.getElementById('submitBtn');
        const modalError = document.getElementById('modalError');

        const courseData = {
            courseCode: courseCodeInput.value.trim(),
            courseName: courseNameInput.value.trim(),
            teacherId: parseInt(teacherIdSelect.value)
        };

        // Validation
        if (!courseData.courseCode || !courseData.courseName || !courseData.teacherId) {
            if (modalError) {
                modalError.textContent = 'All fields are required';
                modalError.classList.remove('hidden');
            }
            return;
        }

        // Show loading state
        const originalBtnText = submitBtn.innerHTML;
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Saving...';

        try {
            if (AdminState.currentCourseId) {
                // Update existing course
                await CourseAPI.updateCourse(AdminState.currentCourseId, courseData);
            } else {
                // Create new course
                await CourseAPI.createCourse(courseData);
            }

            this.closeModal('courseModal');
            await CourseAPI.loadCourses();
        } catch (error) {
            if (modalError) {
                modalError.textContent = error.message;
                modalError.classList.remove('hidden');
            }
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalBtnText;
        }
    },

    /**
     * Confirm and delete course
     */
    confirmDeleteCourse(courseId, courseName) {
        AdminState.deleteTargetId = courseId;

        const deleteCourseName = document.getElementById('deleteCourseName');
        const deleteError = document.getElementById('deleteError');

        if (deleteCourseName) {
            deleteCourseName.textContent = courseName;
        }
        
        if (deleteError) {
            deleteError.classList.add('hidden');
        }

        this.showModal('deleteModal');
    },

    /**
     * Execute course deletion
     */
    async deleteCourse() {
        if (!AdminState.deleteTargetId) return;

        const deleteBtn = document.getElementById('confirmDeleteBtn');
        const deleteError = document.getElementById('deleteError');
        const originalBtnText = deleteBtn.innerHTML;

        deleteBtn.disabled = true;
        deleteBtn.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Deleting...';

        try {
            await CourseAPI.deleteCourse(AdminState.deleteTargetId);
            this.closeModal('deleteModal');
            await CourseAPI.loadCourses();
        } catch (error) {
            if (deleteError) {
                deleteError.textContent = error.message;
                deleteError.classList.remove('hidden');
            }
        } finally {
            deleteBtn.disabled = false;
            deleteBtn.innerHTML = originalBtnText;
        }
    },

    /**
     * Show modal by ID
     */
    showModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.classList.remove('hidden');
            modal.classList.add('flex');
        }
    },

    /**
     * Close modal by ID
     */
    closeModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.classList.add('hidden');
            modal.classList.remove('flex');
        }
        
        // Reset delete target when closing delete modal
        if (modalId === 'deleteModal') {
            AdminState.deleteTargetId = null;
        }
        
        // Reset current course when closing course modal
        if (modalId === 'courseModal') {
            AdminState.currentCourseId = null;
        }
    }
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    console.log('Admin Dashboard JS loaded');

    // Initialize based on current page
    const isDashboard = document.querySelector('[data-page="dashboard"]');
    const isCoursesPage = document.querySelector('[data-page="courses"]');

    if (isDashboard) {
        DashboardAPI.loadDashboardStats();
    }

    if (isCoursesPage) {
        CourseAPI.loadTeachers();
        CourseAPI.loadCourses();

        // Setup search functionality
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                CourseAPI.filterCourses(e.target.value);
            });
        }

        // Setup form submission
        const courseForm = document.getElementById('courseForm');
        if (courseForm) {
            courseForm.addEventListener('submit', (e) => CourseManager.saveCourse(e));
        }
    }
});

// Export to global scope for inline event handlers
window.DashboardAPI = DashboardAPI;
window.CourseAPI = CourseAPI;
window.CourseManager = CourseManager;
window.loadDashboardStats = () => DashboardAPI.loadDashboardStats();
window.loadCourses = () => CourseAPI.loadCourses();
window.openAddModal = () => CourseManager.openAddCourseModal();
window.openEditModal = (id) => CourseManager.openEditCourseModal(id);
window.closeModal = (id) => CourseManager.closeModal(id || 'courseModal');
window.openDeleteModal = (id, name) => CourseManager.confirmDeleteCourse(id, name);
window.closeDeleteModal = () => CourseManager.closeModal('deleteModal');
window.confirmDelete = () => CourseManager.deleteCourse();
