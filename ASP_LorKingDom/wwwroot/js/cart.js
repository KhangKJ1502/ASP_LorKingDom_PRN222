// Cart data
let cartItems = [
    {
        id: 1,
        name: "LEGO Creator 3-in-1 Deep Sea Creatures",
        price: 79.99,
        originalPrice: 99.99,
        quantity: 2,
        image: "/images/lego-deep-sea-creatures.jpg",
        rating: 4.8,
        reviews: 1234,
        inStock: true,
        freeShipping: true,
    },
    {
        id: 2,
        name: "Barbie Dreamhouse Adventures Playset",
        price: 199.99,
        originalPrice: 249.99,
        quantity: 1,
        image: "/images/barbie-dreamhouse-playset.jpg",
        rating: 4.9,
        reviews: 856,
        inStock: true,
        freeShipping: true,
    },
    {
        id: 3,
        name: "Hot Wheels Track Builder Unlimited",
        price: 49.99,
        originalPrice: 59.99,
        quantity: 1,
        image: "/images/hot-wheels-track-builder.jpg",
        rating: 4.7,
        reviews: 567,
        inStock: false,
        freeShipping: false,
    },
]

// Render cart items
function renderCartItems() {
    const container = document.getElementById("cart-items-container")

    if (cartItems.length === 0) {
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
            </div>
        `
        return
    }

    container.innerHTML = cartItems
        .map(
            (item, index) => `
        <div class="group hover:shadow-lg transition-all duration-300 border-2 hover:border-orange-200 overflow-hidden rounded-xl bg-white">
            <div class="p-6">
                <div class="flex gap-6">
                    <!-- Product Image -->
                    <div class="relative">
                        <img src="${item.image || `/placeholder.svg?height=120&width=120&query=${item.name}`}" 
                             alt="${item.name}" 
                             class="w-32 h-32 object-cover rounded-xl shadow-md group-hover:shadow-lg transition-shadow"/>
                        ${!item.inStock
                    ? `
                            <div class="absolute inset-0 bg-black/50 rounded-xl flex items-center justify-center">
                                <span class="bg-red-500 text-white text-xs px-2 py-1 rounded">Out of Stock</span>
                            </div>
                        `
                    : ""
                }
                        ${item.originalPrice > item.price
                    ? `
                            <span class="absolute -top-2 -right-2 bg-red-500 text-white text-xs px-2 py-1 rounded">SALE</span>
                        `
                    : ""
                }
                    </div>

                    <!-- Product Details -->
                    <div class="flex-1 space-y-3">
                        <div class="flex justify-between items-start">
                            <div>
                                <h3 class="font-bold text-lg text-gray-800 group-hover:text-orange-600 transition-colors">
                                    ${item.name}
                                </h3>
                                <div class="flex items-center gap-2 mt-1">
                                    <div class="flex items-center gap-1">
                                        ${[...Array(5)]
                    .map(
                        (_, i) => `
                                            <svg class="h-3 w-3 ${i < Math.floor(item.rating) ? "text-orange-400 fill-current" : "text-gray-300"}" viewBox="0 0 20 20">
                                                <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                                            </svg>
                                        `,
                    )
                    .join("")}
                                    </div>
                                    <span class="text-xs text-gray-500">(${item.reviews} reviews)</span>
                                </div>
                            </div>
                            <button onclick="removeItem(${item.id})" class="text-gray-400 hover:text-red-500 hover:bg-red-50 rounded-full p-2 transition-all duration-200 hover:scale-110">
                                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
                                </svg>
                            </button>
                        </div>

                        <div class="flex items-center gap-2">
                            <span class="text-2xl font-bold text-orange-600">$${item.price}</span>
                            ${item.originalPrice > item.price
                    ? `
                                <span class="text-lg text-gray-400 line-through">$${item.originalPrice}</span>
                            `
                    : ""
                }
                            ${item.freeShipping
                    ? `
                                <span class="text-green-600 border border-green-200 text-xs px-2 py-1 rounded flex items-center gap-1">
                                    <svg class="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0z"/>
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8a1 1 0 011-1h2.586a1 1 0 01.707.293l3.414 3.414a1 1 0 01.293.707V16a1 1 0 01-1 1h-1m-6-1a1 1 0 001 1h1M5 17a2 2 0 104 0m-4 0a2 2 0 114 0m6 0a2 2 0 104 0m-4 0a2 2 0 114 0"/>
                                    </svg>
                                    Free Shipping
                                </span>
                            `
                    : ""
                }
                        </div>

                        <div class="flex items-center justify-between">
                            <!-- Quantity Controls -->
                            <div class="flex items-center gap-3">
                                <span class="text-sm font-medium text-gray-600">Quantity:</span>
                                <div class="flex items-center border-2 border-orange-200 rounded-xl overflow-hidden">
                                    <button onclick="updateQuantity(${item.id}, ${item.quantity - 1})" 
                                            class="h-10 w-10 hover:bg-orange-100 transition-colors ${item.quantity <= 1 ? "opacity-50 cursor-not-allowed" : ""}"
                                            ${item.quantity <= 1 ? "disabled" : ""}>
                                        <svg class="h-4 w-4 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 12H4"/>
                                        </svg>
                                    </button>
                                    <span class="px-4 py-2 font-semibold min-w-[3rem] text-center">${item.quantity}</span>
                                    <button onclick="updateQuantity(${item.id}, ${item.quantity + 1})" 
                                            class="h-10 w-10 hover:bg-orange-100 transition-colors">
                                        <svg class="h-4 w-4 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
                                        </svg>
                                    </button>
                                </div>
                            </div>

                            <!-- Actions -->
                            <div class="flex gap-2">
                                <button class="text-gray-500 hover:text-orange-600 hover:bg-orange-50 transition-all px-3 py-2 rounded-lg flex items-center gap-1">
                                    <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"/>
                                    </svg>
                                    Save
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `,
        )
        .join("")

    updateSummary()
}

// Update quantity
function updateQuantity(id, newQuantity) {
    if (newQuantity < 1) return

    const item = cartItems.find((item) => item.id === id)
    if (item) {
        item.quantity = newQuantity
        renderCartItems()
    }
}

// Remove item
function removeItem(id) {
    cartItems = cartItems.filter((item) => item.id !== id)
    renderCartItems()
}

// Update summary
function updateSummary() {
    const subtotal = cartItems.reduce((sum, item) => sum + item.price * item.quantity, 0)
    const shipping = subtotal > 100 ? 0 : 9.99
    const tax = subtotal * 0.08
    const total = subtotal + shipping + tax

    document.getElementById("cart-item-count").textContent = cartItems.length
    document.getElementById("summary-item-count").textContent = cartItems.length
    document.getElementById("subtotal").textContent = subtotal.toFixed(2)
    document.getElementById("shipping-cost").innerHTML =
        shipping === 0 ? '<span class="text-green-600">FREE</span>' : `$${shipping.toFixed(2)}`
    document.getElementById("tax").textContent = tax.toFixed(2)
    document.getElementById("total").textContent = total.toFixed(2)

    // Disable checkout if cart is empty
    const checkoutBtn = document.getElementById("checkout-btn")
    if (cartItems.length === 0) {
        checkoutBtn.disabled = true
        checkoutBtn.classList.add("opacity-50", "cursor-not-allowed")
    } else {
        checkoutBtn.disabled = false
        checkoutBtn.classList.remove("opacity-50", "cursor-not-allowed")
    }
}

// Initialize
document.addEventListener("DOMContentLoaded", () => {
    renderCartItems()

    // Promo code
    document.getElementById("apply-promo-btn").addEventListener("click", () => {
        const code = document.getElementById("promo-code-input").value
        if (code) {
            alert("Promo code applied: " + code)
        }
    })

    // Checkout
    document.getElementById("checkout-btn").addEventListener("click", () => {
        if (cartItems.length > 0) {
            alert("Proceeding to checkout...")
        }
    })
})
