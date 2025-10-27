// wwwroot/js/cart-client.js
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
        document.querySelector('meta[name="user-id"]') !== null ||
        !!getCookie('__RequestVerificationToken')
    );
}

async function addToCart(productId, qty = 1) {
    if (!productId || qty < 1) return;

    // === KIỂM TRA ĐĂNG NHẬP TRƯỚC KHI GỌI API ===
    if (!isUserLoggedIn()) {
        const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
        window.location.href = `/Auth/Login?returnUrl=${returnUrl}`;
        return;
    }

    try {
        const response = await fetch('/Cart/AddToCart', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: JSON.stringify({ id: productId, qty: qty })
        });

        // === DỰ PHÒNG: XỬ LÝ 401 (nếu token hết hạn) ===
        if (response.status === 401) {
            const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
            window.location.href = `/Auth/Login?returnUrl=${returnUrl}`;
            return;
        }

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Thêm vào giỏ hàng thất bại');
        }

        const result = await response.json();
        if (result.success) {
            await updateCartBadge();
            showCartToast('success', 'Đã thêm vào giỏ hàng!');
        }
    } catch (error) {
        console.error('Add to cart error:', error);
        showCartToast('error', error.message || 'Lỗi khi thêm vào giỏ');
    }
}

async function updateCartBadge() {
    try {
        const response = await fetch('/Cart/GetCartData');
        let count = 0;
        if (response.ok) {
            const data = await response.json();
            count = data.cartItems?.reduce((sum, item) => sum + (item.quantity || 0), 0) ?? 0;
        }
        const badge = document.getElementById('cart-badge');
        if (badge) {
            badge.textContent = count;
            badge.style.display = count > 0 ? 'block' : 'none';
        }
    } catch (e) {
        console.error('Update cart badge error:', e);
    }
}

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