// wwwroot/js/cart-client.js
// Wrap trong IIFE để tránh duplicate variable khi file load 2 lần
(function() {
    'use strict';
    
    // Kiểm tra nếu đã load rồi thì return
    if (window.cartClientLoaded) return;
    window.cartClientLoaded = true;

let cartToastShown = false;

// Helper: lấy cookie
function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
    return null;
}

// Kiểm tra trạng thái đăng nhập ở Frontend
function isUserLoggedIn() {
    return (
        document.body.classList.contains('logged-in') ||
        document.querySelector('meta[name="user-id"]')?.content?.trim() !== '' ||
        !!getCookie('__RequestVerificationToken')
    );
}

// Lấy Anti-Forgery Token
function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value
        || document.querySelector('#__AntiForgeryForm input[name="__RequestVerificationToken"]')?.value
        || '';
}

// Redirect đến login
function redirectToLogin() {
    const returnUrl = encodeURIComponent(window.location.href);
    window.location.href = `/Auth/Login?returnUrl=${returnUrl}`;
}

// Cập nhật badge
function updateBadgeCount(count) {
    const badge = document.getElementById('cart-badge');
    if (badge) {
        badge.textContent = count > 99 ? '99+' : count;
        badge.style.display = count > 0 ? 'block' : 'none';
    }
}

// === THÊM VÀO GIỎ HÀNG ===
async function addToCart(productId, qty = 1) {
    if (!productId || qty < 1) return;

    if (!isUserLoggedIn()) {
        redirectToLogin();
        return;
    }

    try {
        const response = await fetch('/Cart/AddToCart', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({ id: productId, qty: qty })
        });

        // Kiểm tra content-type
        const contentType = response.headers.get('content-type');
        if (!contentType || !contentType.includes('application/json')) {
            redirectToLogin();
            return;
        }

        const result = await response.json();

        if (result.redirectToLogin || response.status === 401) {
            redirectToLogin();
            return;
        }

        if (result.success) {
            await updateCartBadge();
            showCartToast('success', 'Đã thêm vào giỏ hàng!');
        } else {
            showCartToast('error', result.message || 'Thêm thất bại');
        }
    } catch (error) {
        console.error('Add to cart error:', error);
        showCartToast('error', 'Lỗi kết nối');
    }
}

// === CẬP NHẬT BADGE ===
async function updateCartBadge() {
    try {
        const response = await fetch('/Cart/GetCartData', {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (response.status === 401) {
            updateBadgeCount(0);
            return;
        }

        if (!response.ok) return;

        const contentType = response.headers.get('content-type');
        if (!contentType || !contentType.includes('application/json')) {
            updateBadgeCount(0);
            return;
        }

        const data = await response.json();
        const count = data.cartItems?.reduce((sum, item) => sum + (item.quantity || 0), 0) ?? 0;
        updateBadgeCount(count);
    } catch (e) {
        console.error('Update cart badge error:', e);
        updateBadgeCount(0);
    }
}

// === TOAST ===
function showCartToast(type, message) {
    if (typeof showToast === 'function') {
        showToast(type, message);
        return;
    }

    const id = 'toast-' + Date.now();
    const toast = document.createElement('div');
    toast.id = id;
    toast.style.cssText = `
        position: fixed; top: 20px; right: 20px; z-index: 9999;
        background: ${type === 'success' ? '#10b981' : '#ef4444'};
        color: white; padding: 16px 24px; border-radius: 12px;
        box-shadow: 0 10px 25px rgba(0,0,0,0.15); font-size: 15px;
        display: flex; align-items: center; gap: 10px; min-width: 300px;
        animation: slideIn 0.3s ease-out;
    `;
    toast.innerHTML = `
        <span style="font-size: 20px;">${type === 'success' ? 'Success' : 'Warning'}</span>
        <span>${message}</span>
        <button onclick="document.getElementById('${id}').remove()" style="margin-left: auto; background:none; border:none; color:white; font-size:18px; cursor:pointer;">×</button>
    `;
    document.body.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = 'slideOut 0.3s ease-in';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// Load badge khi trang khởi động
document.addEventListener('DOMContentLoaded', () => {
    updateCartBadge();
});

// CSS Animation
const style = document.createElement('style');
style.textContent = `
    @keyframes slideIn { from { transform: translateX(100%); opacity: 0; } to { transform: translateX(0); opacity: 1; } }
    @keyframes slideOut { to { transform: translateX(100%); opacity: 0; } }
`;
document.head.appendChild(style);

})(); // Đóng IIFE