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
