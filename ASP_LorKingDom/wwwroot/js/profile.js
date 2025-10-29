(function () {
  const tabs = document.querySelectorAll(".profile-tab");
  const content = document.getElementById("profile-content");

  async function load(url) {
    content.style.opacity = "0.5";
    try {
      const res = await fetch(url, { credentials: "same-origin" });
      const html = await res.text();
      content.innerHTML = html;
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
      content.innerHTML = `
        <div class="p-6">
          <h3 class="text-xl font-bold mb-2">Overview</h3>
          <p class="text-gray-600">Welcome back! Select a tab to manage your account.</p>
        </div>`;
    } else if (name === "orders") {
      content.innerHTML = `<div class="p-6 text-gray-500">Orders: coming soon.</div>`;
    } else if (name === "wishlist") {
      content.innerHTML = `<div class="p-6 text-gray-500">Wishlist: coming soon.</div>`;
    } else if (name === "settings") {
      content.innerHTML = `<div class="p-6 text-gray-500">Settings: coming soon.</div>`;
    }
  }

  tabs.forEach(tab => tab.addEventListener("click", () => handleTab(tab)));

  window.addEventListener("DOMContentLoaded", () => {
    const defaultTab = document.querySelector(".profile-tab.active") || tabs[0];
    handleTab(defaultTab);
  });
})();
