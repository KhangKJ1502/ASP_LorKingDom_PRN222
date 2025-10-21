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
        container.innerHTML = ''; // Clear

        if (!cart.cartItems || cart.cartItems.length === 0) {
            container.innerHTML = `
                <div class="text-center py-16 bg-white rounded-xl border-2 border-orange-100">
                    <svg class="h-16 w-16 text-gray-300 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z"/>
                    </svg>
                    <h3 class="text-xl font-semibold text-gray-600 mb-2">Your cart is empty</h3>
                    <p class="text-gray-500 mb-6">Add some amazing toys to get started!</p>
                    <a href="/">
                        <button class="bg-gradient-warm text-white px-6 py-3 rounded-xl hover:opacity-90 transition-all">Continue Shopping</button>
                    </a>
                </div>`;
            updateSummary(0, 0, 0, 0);
            return;
        }

        let subtotal = 0;
        cart.cartItems.forEach(item => {
            const itemTotal = item.quantity * item.priceAtThatTime;
            subtotal += itemTotal;
            const itemHtml = `
                <div class="group hover:shadow-lg transition-all duration-300 border-2 hover:border-orange-200 overflow-hidden rounded-xl bg-white">
                    <div class="p-6 flex gap-6 items-start">
                        <!-- Product Image -->
                        <div class="relative">
                            <img src="${item.mainImageUrl || '/assets/placeholder.jpg'}" alt="${item.productName}" class="w-32 h-32 object-cover rounded-xl shadow-md group-hover:shadow-lg transition-shadow"/>
                            ${item.quantity > item.productQuantity ? `
                                <div class="absolute inset-0 bg-black/50 rounded-xl flex items-center justify-center">
                                    <span class="bg-red-500 text-white text-xs px-2 py-1 rounded">Out of Stock</span>
                                </div>` : ''}
                            ${item.currentPrice < item.priceAtThatTime ? `
                                <span class="absolute -top-2 -right-2 bg-red-500 text-white text-xs px-2 py-1 rounded">SALE</span>` : ''}
                        </div>
                        <!-- Product Details -->
                        <div class="flex-1 space-y-3">
                            <div class="flex justify-between items-start">
                                <div>
                                    <h3 class="font-bold text-lg text-gray-800 group-hover:text-orange-600 transition-colors">${item.productName}</h3>
                                </div>
                                <button class="remove-btn text-gray-400 hover:text-red-500 hover:bg-red-50 rounded-full p-2 transition-all duration-200 hover:scale-110" data-cart-item-id="${item.cartItemId}">
                                    <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
                                    </svg>
                                </button>
                            </div>
                            <div class="flex items-center gap-2">
                                <span class="text-2xl font-bold text-orange-600">$${itemTotal.toFixed(2)}</span>
                                ${item.currentPrice < item.priceAtThatTime ? `
                                    <span class="text-lg text-gray-400 line-through">$${(item.quantity * item.currentPrice).toFixed(2)}</span>` : ''}
                            </div>
                            <div class="flex items-center justify-between">
                                <div class="flex items-center gap-3">
                                    <span class="text-sm font-medium text-gray-600">Quantity:</span>
                                    <div class="quantity-control flex items-center border-2 border-orange-200 rounded-xl overflow-hidden">
                                        <button class="quantity-decrease h-10 w-10 hover:bg-orange-100 transition-colors ${item.quantity <= 1 ? 'opacity-50 cursor-not-allowed' : ''}" data-cart-item-id="${item.cartItemId}" ${item.quantity <= 1 ? 'disabled' : ''}>
                                            <svg class="h-4 w-4 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 12H4"/>
                                            </svg>
                                        </button>
                                        <span class="quantity-value px-4 py-2 font-semibold min-w-[3rem] text-center">${item.quantity}</span>
                                        <button class="quantity-increase h-10 w-10 hover:bg-orange-100 transition-colors" data-cart-item-id="${item.cartItemId}">
                                            <svg class="h-4 w-4 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
                                            </svg>
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>`;
            container.innerHTML += itemHtml;
        });

        // Add Clear Cart button after cart items
        if (cart.cartItems.length > 0) {
            container.innerHTML += `
                <div class="mt-6 text-right">
                    <button id="clear-cart-btn" class="bg-red-500 text-white px-6 py-3 rounded-xl hover:bg-red-600 transition-all">Clear Cart</button>
                </div>`;
        }

        // Attach event listeners
        document.querySelectorAll('.quantity-decrease').forEach(btn => {
            btn.addEventListener('click', handleQuantityDecrease);
        });
        document.querySelectorAll('.quantity-increase').forEach(btn => {
            btn.addEventListener('click', handleQuantityIncrease);
        });
        document.querySelectorAll('.remove-btn').forEach(btn => {
            btn.addEventListener('click', handleRemoveItem);
        });
        const clearBtn = document.getElementById('clear-cart-btn');
        if (clearBtn) {
            clearBtn.addEventListener('click', handleClearCart);
        }

        const taxRate = 0.08;
        const tax = subtotal * taxRate;
        const shipping = subtotal > 50 ? 0 : 5;
        const total = subtotal + tax + shipping;

        updateSummary(cart.cartItems.length, subtotal, tax, total);
        document.getElementById('shipping-cost').textContent = shipping === 0 ? 'FREE' : `$${shipping.toFixed(2)}`;

        // Disable checkout button if cart is empty
        const checkoutBtn = document.getElementById('checkout-btn');
        if (cart.cartItems.length === 0) {
            checkoutBtn.disabled = true;
            checkoutBtn.classList.add('opacity-50', 'cursor-not-allowed');
        } else {
            checkoutBtn.disabled = false;
            checkoutBtn.classList.remove('opacity-50', 'cursor-not-allowed');
        }
    } catch (error) {
        console.error('Load cart error:', error);
        document.getElementById('cart-items-container').innerHTML = `
            <div class="text-center py-16 bg-white rounded-xl border-2 border-orange-100">
                <svg class="h-16 w-16 text-gray-300 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z"/>
                </svg>
                <h3 class="text-xl font-semibold text-gray-600 mb-2">Your cart is empty</h3>
                <p class="text-gray-500 mb-6">Add some amazing toys to get started!</p>
                <a href="/">
                    <button class="bg-gradient-warm text-white px-6 py-3 rounded-xl hover:opacity-90 transition-all">Continue Shopping</button>
                </a>
            </div>`;
        updateSummary(0, 0, 0, 0);
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
        loadCart(); // Reload cart
    } catch (error) {
        console.error('Update quantity error:', error);
        alert(`Error updating quantity: ${error.message}`);
    }
}

async function handleRemoveItem(e) {
    const btn = e.currentTarget;
    const cartItemId = parseInt(btn.dataset.cartItemId);
    try {
        const response = await fetch(`/Cart/RemoveItem/${cartItemId}`, { method: 'DELETE' });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Failed to remove item: ${response.status} ${response.statusText} - ${errorText}`);
        }
        loadCart(); // Reload
    } catch (error) {
        console.error('Remove item error:', error);
        alert(`Error removing item: ${error.message}`);
    }
}

async function handleClearCart() {
    if (!confirm('Are you sure to clear the cart?')) return;
    try {
        const response = await fetch('/Cart/Clear', { method: 'DELETE' });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Failed to clear cart: ${response.status} ${response.statusText} - ${errorText}`);
        }
        loadCart(); // Reload
    } catch (error) {
        console.error('Clear cart error:', error);
        alert(`Error clearing cart: ${error.message}`);
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
    if (subtotalEl) subtotalEl.textContent = subtotal.toFixed(2);
    if (taxEl) taxEl.textContent = tax.toFixed(2);
    if (totalEl) totalEl.textContent = total.toFixed(2);
}

document.addEventListener('DOMContentLoaded', () => {
    loadCart();
});