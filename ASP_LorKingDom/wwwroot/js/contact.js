// Contact form handling
document.addEventListener("DOMContentLoaded", () => {
    const contactForm = document.querySelector(".contact-form")

    if (contactForm) {
        contactForm.addEventListener("submit", (e) => {
            // Form validation is handled by HTML5 required attributes
            // Additional custom validation can be added here if needed

            // You can add loading state to the submit button
            const submitButton = contactForm.querySelector(".form-submit")
            submitButton.textContent = "Đang gửi..."
            submitButton.disabled = true

            // The form will submit normally to the server
            // If you want to handle it with AJAX, prevent default and use fetch/XMLHttpRequest
        })
    }

    // Add smooth scroll animation for better UX
    const observerOptions = {
        threshold: 0.1,
        rootMargin: "0px 0px -50px 0px",
    }

    const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = "1"
                entry.target.style.transform = "translateY(0)"
            }
        })
    }, observerOptions)

    // Observe all cards for animation on scroll
    const cards = document.querySelectorAll(".story-card, .value-card, .contact-info-item")
    cards.forEach((card) => {
        card.style.opacity = "0"
        card.style.transform = "translateY(20px)"
        card.style.transition = "opacity 0.6s ease, transform 0.6s ease"
        observer.observe(card)
    })
})
