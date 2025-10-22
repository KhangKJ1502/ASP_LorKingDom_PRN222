function formatCurrencyVND(valueNumber) {
    try {
        return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(valueNumber || 0);
    } catch {
        return (valueNumber || 0).toLocaleString('vi-VN') + ' ₫';
    }
}

/* ========== UI HELPERS: Toast + Confirm Modal ========== */
function ensureUIHelpers() {
    // Toast container
    if (!document.getElementById('toast-container')) {
        const wrap = document.createElement('div');
        wrap.id = 'toast-container';
        wrap.className = 'fixed z-[9999] top-4 right-4 space-y-3';
        document.body.appendChild(wrap);
    }
    // Confirm modal skeleton
    if (!document.getElementById('confirm-overlay')) {
        const modalHtml = `
      <div id="confirm-overlay" class="fixed inset-0 z-[9998] hidden">
        <div class="absolute inset-0 bg-black/40"></div>
        <div class="absolute inset-0 flex items-center justify-center p-4">
          <div class="w-full max-w-md bg-white rounded-2xl shadow-2xl overflow-hidden">
            <div class="px-6 py-4 border-b">
              <h3 id="confirm-title" class="text-lg font-semibold text-gray-800">Xác nhận</h3>
            </div>
            <div class="px-6 py-4">
              <p id="confirm-message" class="text-gray-600">Bạn có chắc?</p>
            </div>
            <div class="px-6 py-4 bg-gray-50 flex justify-end gap-3">
              <button id="confirm-cancel"
                class="px-4 py-2 rounded-xl border border-gray-300 text-gray-700 hover:bg-gray-100 transition">
                Hủy
              </button>
              <button id="confirm-ok"
                class="px-4 py-2 rounded-xl bg-red-500 text-white hover:bg-red-600 transition">
                Đồng ý
              </button>
            </div>
          </div>
        </div>
      </div>`;
        const el = document.createElement('div');
        el.innerHTML = modalHtml;
        document.body.appendChild(el);
    }
}

function showToast(type = 'success', message = '') {
    ensureUIHelpers();
    const container = document.getElementById('toast-container');
    const id = 't' + Date.now() + Math.random().toString(16).slice(2);

    const colorMap = {
        success: { ring: 'ring-green-200', bg: 'bg-green-50', text: 'text-green-700', icon: '✓' },
        error: { ring: 'ring-red-200', bg: 'bg-red-50', text: 'text-red-700', icon: '⚠' },
        info: { ring: 'ring-blue-200', bg: 'bg-blue-50', text: 'text-blue-700', icon: 'ℹ' },
    };
    const c = colorMap[type] || colorMap.info;

    const toast = document.createElement('div');
    toast.id = id;
    toast.className = `flex items-center gap-3 px-4 py-3 rounded-xl shadow-lg ring-1 ${c.ring} ${c.bg} ${c.text} animate-[fadeIn_.2s_ease-out]`;
    toast.innerHTML = `
    <span class="text-lg">${c.icon}</span>
    <div class="text-sm font-medium">${message}</div>
    <button aria-label="Close"
      class="ml-3 text-gray-500 hover:text-gray-700 transition"
      onclick="document.getElementById('${id}')?.remove()">✕</button>
  `;
    container.appendChild(toast);

    // auto remove after 3s
    setTimeout(() => {
        toast.classList.add('opacity-0', 'transition');
        setTimeout(() => toast.remove(), 250);
    }, 3000);
}

/** Promise<boolean> */
function showConfirm({ title = 'Xác nhận', message = 'Bạn có chắc?', confirmText = 'Đồng ý', cancelText = 'Hủy', danger = false } = {}) {
    ensureUIHelpers();
    return new Promise(resolve => {
        const overlay = document.getElementById('confirm-overlay');
        const ttl = document.getElementById('confirm-title');
        const msg = document.getElementById('confirm-message');
        const btnOk = document.getElementById('confirm-ok');
        const btnCancel = document.getElementById('confirm-cancel');

        ttl.textContent = title;
        msg.textContent = message;
        btnOk.textContent = confirmText;
        btnCancel.textContent = cancelText;

        // style danger
        btnOk.className = `px-4 py-2 rounded-xl ${danger ? 'bg-red-500 hover:bg-red-600' : 'bg-orange-500 hover:bg-orange-600'} text-white transition`;

        const close = (v) => {
            overlay.classList.add('hidden');
            // cleanup listeners
            btnOk.onclick = null;
            btnCancel.onclick = null;
            overlay.onclick = null;
            document.onkeydown = null;
            resolve(v);
        };

        btnOk.onclick = () => close(true);
        btnCancel.onclick = () => close(false);
        overlay.onclick = (e) => { if (e.target.id === 'confirm-overlay') close(false); };
        document.onkeydown = (e) => { if (e.key === 'Escape') close(false); };

        overlay.classList.remove('hidden');
    });
}
/* ========== END UI HELPERS ========== */

async function loadCart() {
    try {
        const response = await fetch('/Cart/GetCartData');
        if (!response.ok) {
            if (response.status === 401) {
                window.location.href = '/Auth/Login?returnUrl=/Cart/Index';
                return;
            }
            throw new Error(`Failed to load cart: ${response.status} ${response.statusText}`);
        }

        const cart = await response.json();
        const container = document.getElementById('cart-items-container');
        container.innerHTML = '';

        // Empty state
        if (!cart.cartItems || cart.cartItems.length === 0) {
            container.innerHTML = `
        <div class="text-center py-16 bg-white rounded-2xl border border-orange-100">
          <svg class="h-16 w-16 text-gray-300 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z"/>
          </svg>
          <h3 class="text-xl font-semibold text-gray-600 mb-2">Your cart is empty</h3>
          <p class="text-gray-500 mb-6">Add some amazing toys to get started!</p>
          <a href="/" class="inline-flex items-center justify-center bg-gradient-to-r from-orange-400 to-orange-500 text-white px-6 py-3 rounded-xl hover:opacity-90 transition">
            Continue Shopping
          </a>
        </div>`;
            updateSummary(0, 0, 0, 0);
            const checkoutBtn = document.getElementById('checkout-btn');
            if (checkoutBtn) {
                checkoutBtn.disabled = true;
                checkoutBtn.classList.add('opacity-50', 'cursor-not-allowed');
            }
            const shippingEl = document.getElementById('shipping-cost');
            if (shippingEl) shippingEl.textContent = 'MIỄN PHÍ';
            return;
        }

        // Render items
        let subtotal = 0;
        cart.cartItems.forEach(item => {
            const itemTotal = item.quantity * item.priceAtThatTime;
            subtotal += itemTotal;

            const outOfStockBadge = item.quantity > item.productQuantity
                ? '<div class="absolute inset-0 bg-black/50 rounded-xl flex items-center justify-center"><span class="bg-red-500 text-white text-xs px-2 py-1 rounded">Out of Stock</span></div>'
                : '';

            const itemHtml = `
        <div class="group border-2 border-transparent hover:border-orange-200 bg-white rounded-2xl shadow-sm hover:shadow-lg transition-all duration-300 p-6 flex gap-6">
          <!-- Product Image -->
          <div class="relative">
            <img src="${item.mainImageUrl || '/assets/placeholder.jpg'}" alt="${item.productName}"
                 class="w-32 h-32 object-cover rounded-xl shadow-md group-hover:shadow-lg transition" />
            ${outOfStockBadge}
          </div>

          <!-- Details -->
          <div class="flex-1 space-y-3">
            <div class="flex justify-between items-start">
              <h3 class="font-semibold text-lg text-gray-800 group-hover:text-orange-600 transition-colors">${item.productName}</h3>

              <!-- Remove button -->
              <button
                class="remove-btn text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-full p-2.5
                       transition-transform duration-200 hover:scale-110 focus:outline-none focus:ring-2 focus:ring-red-200"
                data-cart-item-id="${item.cartItemId}" aria-label="Remove item" title="Remove">
                <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6M4 7h16"/>
                </svg>
              </button>
            </div>

            <div class="flex items-center gap-2">
              <span class="text-xl font-bold text-orange-600">${formatCurrencyVND(itemTotal)}</span>
            </div>

            <div class="flex items-center gap-3">
              <span class="text-sm font-medium text-gray-600">Quantity:</span>
              <div class="flex items-center border-2 border-orange-200 rounded-xl overflow-hidden bg-white">
                <button class="quantity-decrease h-10 w-10 flex items-center justify-center hover:bg-orange-100 transition ${item.quantity <= 1 ? 'opacity-50 cursor-not-allowed' : ''}"
                        data-cart-item-id="${item.cartItemId}" ${item.quantity <= 1 ? 'disabled' : ''}>−</button>
                <span class="px-4 py-2 font-semibold min-w-[3rem] text-center select-none">${item.quantity}</span>
                <button class="quantity-increase h-10 w-10 flex items-center justify-center hover:bg-orange-100 transition"
                        data-cart-item-id="${item.cartItemId}">+</button>
              </div>
            </div>
          </div>
        </div>`;
            container.insertAdjacentHTML('beforeend', itemHtml);
        });

        // Clear cart button
        container.insertAdjacentHTML(
            'beforeend',
            `<div class="mt-6 text-right">
        <button id="clear-cart-btn"
          class="inline-flex items-center justify-center bg-red-500 hover:bg-red-600 text-white px-6 py-3 rounded-xl shadow-md hover:shadow-lg hover:scale-[1.02] transition">
          Clear Cart
        </button>
      </div>`
        );

        // Events
        document.querySelectorAll('.quantity-decrease').forEach(btn => btn.addEventListener('click', handleQuantityDecrease));
        document.querySelectorAll('.quantity-increase').forEach(btn => btn.addEventListener('click', handleQuantityIncrease));
        document.querySelectorAll('.remove-btn').forEach(btn => btn.addEventListener('click', handleRemoveItem));
        const clearBtn = document.getElementById('clear-cart-btn');
        if (clearBtn) clearBtn.addEventListener('click', handleClearCart);

        // Summary (VND) - Tax = 0
        const tax = 0;
        const shipping = subtotal > 50000 ? 0 : 5000; // chỉnh rule theo business
        const total = subtotal + tax + shipping;

        updateSummary(cart.cartItems.length, subtotal, tax, total);
        const shippingEl = document.getElementById('shipping-cost');
        if (shippingEl) shippingEl.textContent = shipping === 0 ? 'MIỄN PHÍ' : formatCurrencyVND(shipping);

        // Enable checkout
        const checkoutBtn = document.getElementById('checkout-btn');
        if (checkoutBtn) {
            checkoutBtn.disabled = false;
            checkoutBtn.classList.remove('opacity-50', 'cursor-not-allowed');
        }
    } catch (error) {
        console.error('Load cart error:', error);
        const container = document.getElementById('cart-items-container');
        if (container) {
            container.innerHTML = `
        <div class="text-center py-16 bg-white rounded-2xl border border-orange-100">
          <svg class="h-16 w-16 text-gray-300 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z"/>
          </svg>
          <h3 class="text-xl font-semibold text-gray-600 mb-2">Unable to load your cart</h3>
          <p class="text-gray-500 mb-6">Please try again in a moment.</p>
          <a href="/" class="inline-flex items-center justify-center bg-gradient-to-r from-orange-400 to-orange-500 text-white px-6 py-3 rounded-xl hover:opacity-90 transition">
            Continue Shopping
          </a>
        </div>`;
        }
        updateSummary(0, 0, 0, 0);
        showToast('error', 'Không thể tải giỏ hàng. Vui lòng thử lại.');
    }
}

async function handleQuantityDecrease(e) {
    const cartItemId = parseInt(e.currentTarget.dataset.cartItemId);
    const quantitySpan = e.currentTarget.nextElementSibling;
    const currentQuantity = parseInt(quantitySpan.textContent);
    const newQuantity = currentQuantity - 1;
    if (newQuantity < 1) return;
    updateQuantity(cartItemId, newQuantity);
}

async function handleQuantityIncrease(e) {
    const cartItemId = parseInt(e.currentTarget.dataset.cartItemId);
    const quantitySpan = e.currentTarget.previousElementSibling;
    const currentQuantity = parseInt(quantitySpan.textContent);
    const newQuantity = currentQuantity + 1;
    updateQuantity(cartItemId, newQuantity);
}

async function updateQuantity(cartItemId, newQuantity) {
    try {
        const response = await fetch(`/Cart/UpdateQuantity/${cartItemId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(newQuantity)
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Failed to update quantity: ${response.status} ${response.statusText} - ${errorText}`);
        }
        loadCart();
        showToast('success', 'Đã cập nhật số lượng.');
    } catch (error) {
        console.error('Update quantity error:', error);
        showToast('error', `Lỗi cập nhật số lượng: ${error.message}`);
    }
}

async function handleRemoveItem(e) {
    const cartItemId = parseInt(e.currentTarget.dataset.cartItemId);
    try {
        const response = await fetch(`/Cart/RemoveItem/${cartItemId}`, { method: 'DELETE' });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Failed to remove item: ${response.status} ${response.statusText} - ${errorText}`);
        }
        loadCart();
        showToast('success', 'Đã xóa sản phẩm khỏi giỏ.');
    } catch (error) {
        console.error('Remove item error:', error);
        showToast('error', `Lỗi xóa sản phẩm: ${error.message}`);
    }
}

async function handleClearCart() {
    const ok = await showConfirm({
        title: 'Xóa toàn bộ giỏ hàng?',
        message: 'Thao tác này sẽ xóa tất cả sản phẩm trong giỏ.',
        confirmText: 'Xóa hết',
        cancelText: 'Hủy',
        danger: true
    });
    if (!ok) return;

    try {
        const response = await fetch('/Cart/Clear', { method: 'DELETE' });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Failed to clear cart: ${response.status} ${response.statusText} - ${errorText}`);
        }
        loadCart();
        showToast('success', 'Đã xóa toàn bộ giỏ hàng.');
    } catch (error) {
        console.error('Clear cart error:', error);
        showToast('error', `Lỗi xóa giỏ hàng: ${error.message}`);
    }
}

function updateSummary(itemCount, subtotal, tax, total) {
    const cartCountEl = document.getElementById('cart-item-count');
    const summaryCountEl = document.getElementById('summary-item-count');
    const subtotalEl = document.getElementById('subtotal');
    const taxEl = document.getElementById('tax');
    const totalEl = document.getElementById('total');

    if (cartCountEl) cartCountEl.textContent = itemCount;
    if (summaryCountEl) summaryCountEl.textContent = itemCount;
    if (subtotalEl) subtotalEl.textContent = formatCurrencyVND(subtotal);
    if (taxEl) taxEl.textContent = formatCurrencyVND(tax); // 0 ₫
    if (totalEl) totalEl.textContent = formatCurrencyVND(total);
}

document.addEventListener('DOMContentLoaded', () => {
    ensureUIHelpers();
    loadCart();
});
