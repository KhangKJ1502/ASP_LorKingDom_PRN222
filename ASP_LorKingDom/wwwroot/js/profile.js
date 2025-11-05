/**
 * Profile Page - Tab Management & AJAX Loading
 * Handles all profile page tabs with dynamic content loading
 */

(function () {
    "use strict";

    const tabs = document.querySelectorAll(".profile-tab");
    const content = document.getElementById("profile-content");

    // Tab content mapping (fallback for tabs without dynamic content)
    const tabContents = {
        wishlist: `
            <div class="bg-white rounded-xl border-2 border-orange-100 shadow-sm p-8">
                <h2 class="text-2xl font-bold text-orange-600 mb-4">Danh sách yêu thích</h2>
                <p class="text-gray-600">Danh sách yêu thích của bạn đang trống. Hãy thêm sản phẩm yêu thích!</p>
            </div>
        `,
    };

    /**
     * Execute embedded scripts from AJAX-loaded content
     * Required because jQuery .html() doesn't auto-execute <script> tags
     */
    function runEmbeddedScripts(container) {
        const scripts = container.querySelectorAll("script");
        scripts.forEach((oldScript) => {
            const newScript = document.createElement("script");

            // Copy all attributes
            for (const attr of oldScript.attributes) {
                newScript.setAttribute(attr.name, attr.value);
            }

            // Copy content or src
            if (oldScript.src) {
                newScript.async = false;
            } else {
                newScript.textContent = oldScript.textContent;
            }

            document.body.appendChild(newScript);
            oldScript.remove();
        });
    }

    /**
     * Load content via AJAX with error handling and script execution
     */
    async function loadContent(url, onSuccess) {
        content.style.opacity = "0.5";

        try {
            const response = await fetch(url, {
                credentials: "same-origin",
                headers: {
                    "X-Requested-With": "XMLHttpRequest",
                },
            });

            // Handle authentication redirect
            if (response.status === 401) {
                window.location.href =
                    "/Auth/Login?returnUrl=" + encodeURIComponent(location.pathname + location.search);
                return;
            }

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            const html = await response.text();
            content.innerHTML = html;

            // Execute scripts in loaded content
            runEmbeddedScripts(content);

            // Call success callback if provided
            if (typeof onSuccess === "function") {
                onSuccess();
            }
        } catch (error) {
            console.error(`Error loading ${url}:`, error);
            content.innerHTML = `
                <div class="bg-white rounded-xl border-2 border-orange-100 shadow-sm p-8 text-center">
                    <h2 class="text-2xl font-bold text-red-600 mb-4">Lỗi</h2>
                    <p class="text-gray-600">Không thể tải nội dung. Vui lòng thử lại.</p>
                    <button onclick="window.location.reload()" class="mt-4 px-4 py-2 bg-orange-500 text-white rounded-lg hover:bg-orange-600">
                        Tải lại trang
                    </button>
                </div>
            `;
        } finally {
            content.style.opacity = "1";
        }
    }

    /**
     * Check URL hash to determine active tab
     */
    function getActiveTabFromHash() {
        const hash = window.location.hash.substring(1);
        return hash || "overview";
    }

    /**
     * Set URL hash when tab changes
     */
    function setTabHash(tabName) {
        window.history.replaceState(null, null, "#" + tabName);
    }

    /**
     * Main tab handler - routes to appropriate content loader
     */
    async function handleTab(tabElement) {
        // Update active state
        tabs.forEach((t) => t.classList.remove("active"));
        tabElement.classList.add("active");

        const tabName = tabElement.dataset.tab;
        console.log("Loading tab:", tabName);

        // Update URL hash
        setTabHash(tabName);

        // Route to appropriate content
        switch (tabName) {
            case "overview":
                await loadContent("/Home/ProfileOverview", () => {
                    // Initialize profile overview handlers
                    if (typeof window.initializeProfileOverview === "function") {
                        window.initializeProfileOverview();
                    }
                    if (typeof window.handleChangePassword === "function") {
                        window.handleChangePassword();
                    }
                });
                break;

            case "addresses":
                await loadContent("/Address/ListPartial", () => {
                    // Trigger DOMContentLoaded for address scripts
                    const evt = new Event("DOMContentLoaded");
                    document.dispatchEvent(evt);
                });
                break;

            case "orders":
                await loadContent("/Order/OrderHistory", () => {
                    // Initialize order filters
                    if (typeof window.initializeOrderFilters === "function") {
                        window.initializeOrderFilters();
                    }
                });
                break;

            case "wallets":
                await loadContent("/Wallet/Partial");
                break;

            case "reviews":
                await loadContent("/Review/Partial", () => {
                    // Handle stored navigation state
                    setTimeout(() => {
                        const tabType = sessionStorage.getItem("reviewsTabType");
                        const productId = sessionStorage.getItem("highlightProductId");

                        if (tabType === "reviewed") {
                            const reviewedTab = document.querySelector('.review-tab[data-tab="reviewed"]');
                            if (reviewedTab) reviewedTab.click();
                        } else if (tabType === "pending") {
                            const pendingTab = document.querySelector('.review-tab[data-tab="pending"]');
                            if (pendingTab) pendingTab.click();
                        }

                        if (productId && typeof highlightReviewByProductId === "function") {
                            highlightReviewByProductId(parseInt(productId));
                            sessionStorage.removeItem("highlightProductId");
                        }

                        sessionStorage.removeItem("reviewsTabType");
                    }, 200);
                });
                break;

            //default:
            //    // Use fallback content if available
            //    if (tabContents[tabName]) {
            //        content.innerHTML = tabContents[tabName];
            //    } else {
            //        content.innerHTML = `
            //            <div class="bg-white rounded-xl border-2 border-orange-100 shadow-sm p-8">
            //                <h2 class="text-2xl font-bold text-orange-600 mb-4">Sắp ra mắt</h2>
            //                <p class="text-gray-600">Tính năng này đang được phát triển.</p>
            //            </div>
            //        `;
            //    }
        }
    }

    /**
     * Initialize tab click handlers
     */
    tabs.forEach((tab) => {
        tab.addEventListener("click", (e) => {
            e.preventDefault();
            handleTab(tab);
        });
    });

    /**
     * Expose handleTab globally for external calls
     */
    window.loadTab = function (tabNameOrElement) {
        if (typeof tabNameOrElement === "string") {
            const tabElement = document.querySelector(`[data-tab="${tabNameOrElement}"]`);
            if (tabElement) {
                handleTab(tabElement);
            }
        } else if (tabNameOrElement instanceof HTMLElement) {
            handleTab(tabNameOrElement);
        }
    };

    /**
     * Load initial tab on page load
     */
    window.addEventListener("DOMContentLoaded", () => {
        // Check for hash in URL
        const hash = window.location.hash.substring(1);
        if (hash) {
            const targetTab = document.querySelector(`[data-tab="${hash}"]`);
            if (targetTab) {
                handleTab(targetTab);
                return;
            }
        }

        // Load default tab
        const defaultTab = document.querySelector(".profile-tab.active") || tabs[0];
        if (defaultTab) {
            handleTab(defaultTab);
        }
    });
})();
