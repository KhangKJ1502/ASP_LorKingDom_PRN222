// Profile data
const recentOrders = [
    {
        id: "ORD-001",
        date: "2024-01-15",
        status: "Delivered",
        total: 129.99,
        items: 3,
        image: "/images/lego-set-order.jpg",
    },
    {
        id: "ORD-002",
        date: "2024-01-10",
        status: "Shipped",
        total: 79.99,
        items: 2,
        image: "/images/barbie-doll-order.jpg",
    },
    {
        id: "ORD-003",
        date: "2024-01-05",
        status: "Processing",
        total: 199.99,
        items: 1,
        image: "/images/remote-car-order.jpg",
    },
]

const wishlistItems = [
    {
        id: 1,
        name: "LEGO Technic Bugatti Chiron",
        price: 349.99,
        image: "/images/lego-bugatti-chiron.jpg",
        inStock: true,
    },
    {
        id: 2,
        name: "Nintendo Switch OLED",
        price: 299.99,
        image: "/images/nintendo-switch-oled.jpg",
        inStock: false,
    },
    {
        id: 3,
        name: "Barbie Dream Camper",
        price: 89.99,
        image: "/images/barbie-dream-camper.jpg",
        inStock: true,
    },
]

let isEditing = false

// Render tab content
function renderTabContent(tab) {
    const content = document.getElementById("profile-content")

    switch (tab) {
        case "overview":
            content.innerHTML = renderOverviewTab()
            break
        case "orders":
            content.innerHTML = renderOrdersTab()
            break
        case "wishlist":
            content.innerHTML = renderWishlistTab()
            break
        
        case "settings":
            content.innerHTML = renderSettingsTab()
            attachSettingsListeners()
            break
    }
}

function renderOverviewTab() {
    return `
        <div class="space-y-6">
            <div class="grid md:grid-cols-2 gap-6">
                <!-- Recent Orders Card -->
                <div class="border-2 border-orange-100 hover:shadow-lg transition-shadow rounded-xl bg-white">
                    <div class="p-6">
                        <h3 class="flex items-center gap-2 text-gradient text-xl font-bold mb-4">
                            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z"/>
                            </svg>
                            Recent Orders
                        </h3>
                        <div class="space-y-4">
                            ${recentOrders
            .slice(0, 3)
            .map(
                (order) => `
                                <div class="flex items-center gap-4 p-3 rounded-xl bg-orange-50/50 hover:bg-orange-50 transition-colors">
                                    <img src="${order.image || `/placeholder.svg?height=50&width=50&query=toy order`}" 
                                         alt="Order" 
                                         class="w-12 h-12 rounded-lg object-cover"/>
                                    <div class="flex-1">
                                        <div class="flex justify-between items-start">
                                            <div>
                                                <p class="font-semibold text-sm">${order.id}</p>
                                                <p class="text-xs text-gray-600">${order.date}</p>
                                            </div>
                                            <span class="text-xs px-2 py-1 rounded ${order.status === "Delivered" ? "bg-green-100 text-green-800" : "bg-gray-100 text-gray-800"}">
                                                ${order.status}
                                            </span>
                                        </div>
                                        <p class="text-sm font-semibold text-orange-600">$${order.total}</p>
                                    </div>
                                </div>
                            `,
            )
            .join("")}
                            <button class="w-full border-2 border-orange-200 hover:bg-orange-50 bg-transparent py-2 rounded-xl transition-all">
                                View All Orders
                            </button>
                        </div>
                    </div>
                </div>

                <!-- Wishlist Card -->
                <div class="border-2 border-orange-100 hover:shadow-lg transition-shadow rounded-xl bg-white">
                    <div class="p-6">
                        <h3 class="flex items-center gap-2 text-gradient text-xl font-bold mb-4">
                            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"/>
                            </svg>
                            Wishlist Items
                        </h3>
                        <div class="space-y-4">
                            ${wishlistItems
            .slice(0, 3)
            .map(
                (item) => `
                                <div class="flex items-center gap-4 p-3 rounded-xl bg-orange-50/50 hover:bg-orange-50 transition-colors">
                                    <img src="${item.image || `/placeholder.svg?height=50&width=50&query=${item.name}`}" 
                                         alt="${item.name}" 
                                         class="w-12 h-12 rounded-lg object-cover"/>
                                    <div class="flex-1">
                                        <p class="font-semibold text-sm line-clamp-1">${item.name}</p>
                                        <p class="text-sm font-semibold text-orange-600">$${item.price}</p>
                                        <span class="text-xs px-2 py-1 rounded ${item.inStock ? "bg-green-100 text-green-800" : "bg-gray-100 text-gray-800"}">
                                            ${item.inStock ? "In Stock" : "Out of Stock"}
                                        </span>
                                    </div>
                                </div>
                            `,
            )
            .join("")}
                            <button class="w-full border-2 border-orange-200 hover:bg-orange-50 bg-transparent py-2 rounded-xl transition-all">
                                View All Wishlist
                            </button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Quick Actions -->
            <div class="border-2 border-orange-100 rounded-xl bg-white">
                <div class="p-6">
                    <h3 class="text-gradient text-xl font-bold mb-4">Quick Actions</h3>
                    <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
                        <button class="h-20 flex flex-col items-center justify-center gap-2 border-2 border-orange-200 hover:bg-orange-50 bg-transparent rounded-xl transition-all">
                            <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"/>
                            </svg>
                            <span class="text-sm">Payment Methods</span>
                        </button>
                        
                        <button class="h-20 flex flex-col items-center justify-center gap-2 border-2 border-orange-200 hover:bg-orange-50 bg-transparent rounded-xl transition-all">
                            <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v13m0-13V6a2 2 0 112 2h-2zm0 0V5.5A2.5 2.5 0 109.5 8H12zm-7 4h14M5 12a2 2 0 110-4h14a2 2 0 110 4M5 12v7a2 2 0 002 2h10a2 2 0 002-2v-7"/>
                            </svg>
                            <span class="text-sm">Gift Cards</span>
                        </button>
                        <button class="h-20 flex flex-col items-center justify-center gap-2 border-2 border-orange-200 hover:bg-orange-50 bg-transparent rounded-xl transition-all">
                            <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"/>
                            </svg>
                            <span class="text-sm">Notifications</span>
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `
}

function renderOrdersTab() {
    return `
        <div class="border-2 border-orange-100 rounded-xl bg-white">
            <div class="p-6">
                <h3 class="text-gradient text-xl font-bold mb-4">Order History</h3>
                <div class="space-y-4">
                    ${recentOrders
            .map(
                (order) => `
                        <div class="flex items-center gap-6 p-4 rounded-xl bg-orange-50/50 hover:bg-orange-50 transition-colors">
                            <img src="${order.image || `/placeholder.svg?height=80&width=80&query=toy order`}" 
                                 alt="Order" 
                                 class="w-20 h-20 rounded-xl object-cover"/>
                            <div class="flex-1">
                                <div class="flex justify-between items-start mb-2">
                                    <div>
                                        <h4 class="font-bold text-lg">${order.id}</h4>
                                        <p class="text-gray-600">Ordered on ${order.date}</p>
                                    </div>
                                    <span class="px-3 py-1 rounded ${order.status === "Delivered" ? "bg-green-100 text-green-800" : "bg-gray-100 text-gray-800"}">
                                        ${order.status}
                                    </span>
                                </div>
                                <div class="flex justify-between items-center">
                                    <div>
                                        <p class="text-sm text-gray-600">${order.items} items</p>
                                        <p class="text-xl font-bold text-orange-600">$${order.total}</p>
                                    </div>
                                    <div class="flex gap-2">
                                        <button class="border-2 border-orange-200 hover:bg-orange-50 bg-transparent px-4 py-2 rounded-xl text-sm transition-all">
                                            View Details
                                        </button>
                                        <button class="border-2 border-orange-200 hover:bg-orange-50 bg-transparent px-4 py-2 rounded-xl text-sm transition-all">
                                            Reorder
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    `,
            )
            .join("")}
                </div>
            </div>
        </div>
    `
}

function renderWishlistTab() {
    return `
        <div class="border-2 border-orange-100 rounded-xl bg-white">
            <div class="p-6">
                <h3 class="text-gradient text-xl font-bold mb-4">My Wishlist</h3>
                <div class="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
                    ${wishlistItems
            .map(
                (item) => `
                        <div class="group cursor-pointer">
                            <div class="border-2 border-orange-100 hover:border-orange-300 hover:shadow-lg transition-all rounded-xl bg-white">
                                <div class="p-4">
                                    <img src="${item.image || `/placeholder.svg?height=200&width=200&query=${item.name}`}" 
                                         alt="${item.name}" 
                                         class="w-full h-48 object-cover rounded-xl mb-4"/>
                                    <h4 class="font-semibold mb-2 line-clamp-2">${item.name}</h4>
                                    <p class="text-xl font-bold text-orange-600 mb-3">$${item.price}</p>
                                    <div class="flex gap-2">
                                        <button class="flex-1 bg-gradient-warm text-white py-2 rounded-xl hover:opacity-90 transition-all">
                                            Add to Cart
                                        </button>
                                        <button class="border-2 border-orange-200 bg-transparent p-2 rounded-xl hover:bg-orange-50 transition-all">
                                            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"/>
                                            </svg>
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    `,
            )
            .join("")}
                </div>
            </div>
        </div>
    `
}



function renderSettingsTab() {
    return `
        <div class="space-y-6">
            <div class="border-2 border-orange-100 rounded-xl bg-white">
                <div class="p-6">
                    <div class="flex items-center justify-between mb-6">
                        <h3 class="text-gradient text-xl font-bold">Personal Information</h3>
                        <button id="edit-profile-btn" class="border-2 border-orange-200 hover:bg-orange-50 px-4 py-2 rounded-xl text-sm flex items-center gap-2 transition-all">
                            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/>
                            </svg>
                            <span id="edit-btn-text">Edit</span>
                        </button>
                    </div>
                    <div class="grid md:grid-cols-2 gap-6">
                        <div>
                            <label class="text-sm font-medium text-gray-600">Full Name</label>
                            <input type="text" value="John Doe" disabled id="input-name" class="w-full mt-1 px-4 py-2 border-2 border-orange-200 rounded-xl focus:outline-none focus:border-orange-400 disabled:bg-gray-50"/>
                        </div>
                        <div>
                            <label class="text-sm font-medium text-gray-600">Email</label>
                            <input type="email" value="john.doe@example.com" disabled id="input-email" class="w-full mt-1 px-4 py-2 border-2 border-orange-200 rounded-xl focus:outline-none focus:border-orange-400 disabled:bg-gray-50"/>
                        </div>
                        <div>
                            <label class="text-sm font-medium text-gray-600">Phone</label>
                            <input type="tel" value="+1 (555) 123-4567" disabled id="input-phone" class="w-full mt-1 px-4 py-2 border-2 border-orange-200 rounded-xl focus:outline-none focus:border-orange-400 disabled:bg-gray-50"/>
                        </div>
                        <div>
                            <label class="text-sm font-medium text-gray-600">Date of Birth</label>
                            <input type="date" value="1990-01-01" disabled id="input-dob" class="w-full mt-1 px-4 py-2 border-2 border-orange-200 rounded-xl focus:outline-none focus:border-orange-400 disabled:bg-gray-50"/>
                        </div>
                    </div>
                </div>
            </div>

            <div class="border-2 border-orange-100 rounded-xl bg-white">
                <div class="p-6">
                    <h3 class="text-gradient text-xl font-bold mb-4">Account Settings</h3>
                    <div class="space-y-4">
                        <div class="flex items-center justify-between p-4 rounded-xl bg-orange-50/50">
                            <div class="flex items-center gap-3">
                                <svg class="h-5 w-5 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"/>
                                </svg>
                                <div>
                                    <p class="font-semibold">Email Notifications</p>
                                    <p class="text-sm text-gray-600">Receive updates about orders and promotions</p>
                                </div>
                            </div>
                            <button class="border-2 border-orange-200 bg-transparent px-4 py-2 rounded-xl text-sm hover:bg-orange-50 transition-all">
                                Manage
                            </button>
                        </div>
                        <div class="flex items-center justify-between p-4 rounded-xl bg-orange-50/50">
                            <div class="flex items-center gap-3">
                                <svg class="h-5 w-5 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
                                </svg>
                                <div>
                                    <p class="font-semibold">Privacy Settings</p>
                                    <p class="text-sm text-gray-600">Control your data and privacy preferences</p>
                                </div>
                            </div>
                            <button class="border-2 border-orange-200 bg-transparent px-4 py-2 rounded-xl text-sm hover:bg-orange-50 transition-all">
                                Manage
                            </button>
                        </div>
                        <div class="flex items-center justify-between p-4 rounded-xl bg-orange-50/50">
                            <div class="flex items-center gap-3">
                                <svg class="h-5 w-5 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"/>
                                </svg>
                                <div>
                                    <p class="font-semibold">Payment Methods</p>
                                    <p class="text-sm text-gray-600">Manage your saved payment methods</p>
                                </div>
                            </div>
                            <button class="border-2 border-orange-200 bg-transparent px-4 py-2 rounded-xl text-sm hover:bg-orange-50 transition-all">
                                Manage
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `
}

function attachSettingsListeners() {
    const editBtn = document.getElementById("edit-profile-btn");
    const inputs = ["input-name", "input-email", "input-phone", "input-dob"];

    editBtn.addEventListener("click", () => {
        isEditing = !isEditing;
        const btnText = document.getElementById("edit-btn-text");

        inputs.forEach((id) => {
            const input = document.getElementById(id);
            input.disabled = !isEditing;
        });

        btnText.textContent = isEditing ? "Save" : "Edit";
        if (!isEditing) alert("Profile updated successfully!");
    });
}

// Initialize
document.addEventListener("DOMContentLoaded", () => {
    const tabs = document.querySelectorAll(".profile-tab");

    tabs.forEach((tabBtn) => {
        tabBtn.addEventListener("click", function () {
            const tab = this.dataset.tab;

            // Active state
            tabs.forEach((t) => t.classList.remove("active"));
            this.classList.add("active");

            // Các tab còn lại render như cũ
            renderTabContent(tab);
        });
    });

    // Tab mặc định
    renderTabContent("overview");
});
