// API Configuration
const API_BASE_URL = 'https://localhost:7124/api';

// DOM Elements
const loginLink = document.getElementById('loginLink');
const registerLink = document.getElementById('registerLink');
const profileLink = document.getElementById('profileLink');
const logoutLink = document.getElementById('logoutLink');
const userDropdown = document.getElementById('userDropdown');
const cartCount = document.querySelector('.cart-count');
const categoriesMenu = document.getElementById('categoriesMenu');

// Check if user is logged in
function checkAuthStatus() {
    const token = localStorage.getItem('token');
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    
    if (token) {
        // Show user profile and logout links
        if (loginLink) loginLink.style.display = 'none';
        if (registerLink) registerLink.style.display = 'none';
        if (profileLink) profileLink.style.display = 'block';
        if (logoutLink) logoutLink.style.display = 'block';
        
        // Update user avatar
        if (userDropdown) {
            const avatar = userDropdown.querySelector('.user-avatar');
            if (avatar) {
                avatar.src = `https://ui-avatars.com/api/?name=${user.firstName}+${user.lastName}&background=2c3e50&color=fff`;
            }
        }
    } else {
        // Show login and register links
        if (loginLink) loginLink.style.display = 'block';
        if (registerLink) registerLink.style.display = 'block';
        if (profileLink) profileLink.style.display = 'none';
        if (logoutLink) logoutLink.style.display = 'none';
    }
}

// Logout function
function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = 'index.html';
}

// API Request function
// Replace the apiRequest function in common.js with this improved version
async function apiRequest(endpoint, options = {}) {
    const url = `${API_BASE_URL}${endpoint}`;
    const token = localStorage.getItem('token');
    
    const defaultOptions = {
        headers: {
            'Content-Type': 'application/json'
        }
    };
    
    // Add authorization header if token exists
    if (token) {
        defaultOptions.headers.Authorization = `Bearer ${token}`;
    }
    
    const config = { ...defaultOptions, ...options };
    
    // If body is provided and it's not a string, stringify it
    if (config.body && typeof config.body !== 'string') {
        config.body = JSON.stringify(config.body);
    }
    
    try {
        const response = await fetch(url, config);
        
        // Handle 401 Unauthorized (token expired or invalid)
        if (response.status === 401) {
            localStorage.removeItem('token');
            localStorage.removeItem('user');
            
            // Only redirect if not already on login page to prevent infinite loop
            if (!window.location.pathname.includes('login.html')) {
                window.location.href = 'login.html?redirect=' + encodeURIComponent(window.location.pathname);
            }
            throw new Error('Session expired. Please login again.');
        }
        
        if (!response.ok) {
            let errorMessage = 'Something went wrong';
            
            try {
                const errorData = await response.json();
                errorMessage = errorData.message || errorData.title || JSON.stringify(errorData);
                
                // If it's a validation error, get more details
                if (errorData.errors) {
                    const validationErrors = Object.values(errorData.errors)
                        .flat()
                        .join(', ');
                    errorMessage = validationErrors || errorMessage;
                }
            } catch (e) {
                // If we can't parse the error response, use the status text
                errorMessage = response.statusText || `Error ${response.status}`;
            }
            
            throw new Error(errorMessage);
        }
        
        // For 204 No Content responses
        if (response.status === 204) {
            return null;
        }
        
        return await response.json();
    } catch (error) {
        console.error('API Request Error:', error);
        throw error;
    }
}
// Load categories for navigation
async function loadCategories() {
    try {
        const categories = await apiRequest('/categories');
        
        if (categoriesMenu) {
            categoriesMenu.innerHTML = '';
            categories.forEach(category => {
                const li = document.createElement('li');
                li.innerHTML = `<a class="dropdown-item" href="products.html?category=${category.id}">${category.name}</a>`;
                categoriesMenu.appendChild(li);
            });
        }
    } catch (error) {
        console.error('Error loading categories:', error);
    }
}

// Update cart count
async function updateCartCount() {
    const token = localStorage.getItem('token');
    
    if (!token || !cartCount) return;
    
    try {
        const cart = await apiRequest('/cart');
        cartCount.textContent = cart.totalItems || 0;
    } catch (error) {
        console.error('Error updating cart count:', error);
    }
}

// Format price
function formatPrice(price) {
    return new Intl.NumberFormat('ar-SA', {
        style: 'currency',
        currency: 'SAR',
        minimumFractionDigits: 0
    }).format(price);
}

// Format date
function formatDate(dateString) {
    const options = { year: 'numeric', month: 'long', day: 'numeric' };
    return new Date(dateString).toLocaleDateString('ar-SA', options);
}

// Show notification
function showNotification(message, type = 'success') {
    const notification = document.createElement('div');
    notification.className = `alert alert-${type} position-fixed top-0 start-50 translate-middle-x mt-3`;
    notification.style.zIndex = '9999';
    notification.style.dir = 'rtl';
    notification.style.textAlign = 'right';
    notification.textContent = message;
    
    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.remove();
    }, 5000);
}

// Initialize common functionality
document.addEventListener('DOMContentLoaded', () => {
    checkAuthStatus();
    loadCategories();
    updateCartCount();
    
    // Add event listeners
    if (logoutLink) {
        logoutLink.addEventListener('click', (e) => {
            e.preventDefault();
            logout();
        });
    }
    
    // Search functionality
    const searchInput = document.querySelector('.search-input');
    const searchBtn = document.querySelector('.search-btn');
    
    if (searchBtn && searchInput) {
        searchBtn.addEventListener('click', () => {
            const searchTerm = searchInput.value.trim();
            if (searchTerm) {
                window.location.href = `products.html?search=${encodeURIComponent(searchTerm)}`;
            }
        });
        
        searchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                const searchTerm = searchInput.value.trim();
                if (searchTerm) {
                    window.location.href = `products.html?search=${encodeURIComponent(searchTerm)}`;
                }
            }
        });
    }
    
    // Newsletter form
    const newsletterForm = document.querySelector('.newsletter-form');
    if (newsletterForm) {
        newsletterForm.addEventListener('submit', (e) => {
            e.preventDefault();
            const email = newsletterForm.querySelector('input[type="email"]').value;
            showNotification(`تم اشتراكك بنجاح باستخدام البريد الإلكتروني: ${email}`);
            newsletterForm.reset();
        });
    }
});