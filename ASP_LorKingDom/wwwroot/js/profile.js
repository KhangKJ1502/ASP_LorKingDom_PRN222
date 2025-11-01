(function () {
    const tabs = document.querySelectorAll(".profile-tab");
    const content = document.getElementById("profile-content");

    function runEmbeddedScripts(container) {
        const scripts = container.querySelectorAll("script");
        scripts.forEach((oldScript) => {
            const s = document.createElement("script");
    
            for (const attr of oldScript.attributes) {
                s.setAttribute(attr.name, attr.value);
            }
            if (oldScript.src) {

                s.async = false;
            } else {
                s.textContent = oldScript.textContent;
            }
            document.body.appendChild(s);
            oldScript.remove();
        });
    }

    async function load(url) {
        content.style.opacity = "0.5";
        try {
            const res = await fetch(url, { credentials: "same-origin" });

            if (res.status === 401) {
                window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search);
                return;
            }

            if (!res.ok) throw new Error("Network error");
            const html = await res.text();
            content.innerHTML = html;

            runEmbeddedScripts(content);
        } catch (err) {
            content.innerHTML = `<div class="p-6 text-red-600 text-center">Lỗi tải dữ liệu. Vui lòng thử lại.</div>`;
        } finally {
            content.style.opacity = "1";
        }
    }

    async function handleTab(tab) {
        tabs.forEach(t => t.classList.remove("active"));
        tab.classList.add("active");
        const name = tab.dataset.tab;

        if (name === "addresses") {
            await load("/Address/ListPartial");
        } else if (name === "overview") {
            content.innerHTML = `<div class="p-6"><h3 class="text-xl font-bold mb-2">Tổng quan</h3><p class="text-gray-600">Chào mừng bạn trở lại!</p></div>`;
        } else if (name === "orders") {
            content.innerHTML = `<div class="p-6 text-gray-500">Đơn hàng: Sắp ra mắt.</div>`;
        } else if (name === "wishlist") {
            content.innerHTML = `<div class="p-6 text-gray-500">Yêu thích: Sắp ra mắt.</div>`;
        } else if (name === "settings") {
            content.innerHTML = `<div class="p-6 text-gray-500">Cài đặt: Sắp ra mắt.</div>`;
        } else if (name === "wallets") {
            await load("/Wallet/Partial");
        }
    }

    tabs.forEach(tab => tab.addEventListener("click", () => handleTab(tab)));

    window.addEventListener("DOMContentLoaded", () => {
        const defaultTab = document.querySelector(".profile-tab.active") || tabs[0];
        if (defaultTab) handleTab(defaultTab);
    });
})();
