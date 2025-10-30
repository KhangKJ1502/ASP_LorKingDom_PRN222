// Site-wide JavaScript

// Mobile menu toggle
document.addEventListener("DOMContentLoaded", () => {
    const mobileMenuBtn = document.getElementById("mobileMenuBtn")
    const navLinks = document.querySelector(".nav-links")

    if (mobileMenuBtn && navLinks) {
        mobileMenuBtn.addEventListener("click", () => {
            navLinks.classList.toggle("active")
        })
    }

    // Smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach((anchor) => {
        anchor.addEventListener("click", function (e) {
            e.preventDefault()
            const target = document.querySelector(this.getAttribute("href"))
            if (target) {
                target.scrollIntoView({
                    behavior: "smooth",
                    block: "start",
                })
            }
        })
    })

    // Add scroll effect to header
    let lastScroll = 0
    const header = document.querySelector(".site-header")

    window.addEventListener("scroll", () => {
        const currentScroll = window.pageYOffset

        if (currentScroll > 100) {
            header.classList.add("scrolled")
        } else {
            header.classList.remove("scrolled")
        }

        lastScroll = currentScroll
    })
})
function showToast(type, message, timeout = 4000) {
    const root = document.getElementById('toast-root');
    if (!root) return;

    const div = document.createElement('div');
    div.className = `toast toast-${type}`;
    div.innerHTML = `
    <span style="font-weight:700; font-size:14px;">${message}</span>
    <button class="toast-close" aria-label="Close">✕</button>
  `;
    root.appendChild(div);

    // show
    requestAnimationFrame(() => div.classList.add('show'));

    // close handlers
    const close = () => {
        div.classList.remove('show');
        setTimeout(() => div.remove(), 200);
    };
    div.querySelector('.toast-close').addEventListener('click', close);
    if (timeout > 0) setTimeout(close, timeout);
}
