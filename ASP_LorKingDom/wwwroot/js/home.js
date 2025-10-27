// Hero Slider
let currentSlide = 0
const slides = [
    {
        title: "Discover Amazing Toys World",
        subtitle: "Thousands of high-quality products for your beloved children",
        image: "colorful-toys-and-children-playing.jpg",
        cta: "Shop Now",
        discount: "50% OFF",
        badge: "Best Seller",
    },
    {
        title: "Smart Educational Toys",
        subtitle: "Develop intelligence and creativity for children",
        image: "educational-toys-and-learning-games.jpg",
        cta: "Explore",
        discount: "New Arrival",
        badge: "Educational",
    },
    {
        title: "Trusted Global Brands",
        subtitle: "Lego, Barbie, Hot Wheels and many more",
        image: "famous-toy-brands-display.jpg",
        cta: "View All",
        discount: "Hot Deal",
        badge: "Premium",
    },
]

function updateSlide() {
    // Update images
    document.querySelectorAll(".hero-image").forEach((img, index) => {
        img.classList.toggle("active", index === currentSlide)
    })

    // Update dots
    document.querySelectorAll(".hero-dot").forEach((dot, index) => {
        dot.classList.toggle("active", index === currentSlide)
    })

    // Update content
    const slide = slides[currentSlide]
    document.getElementById("heroTitle").textContent = slide.title
    document.getElementById("heroSubtitle").textContent = slide.subtitle
    document.getElementById("heroCtaText").textContent = slide.cta
    document.getElementById("heroBadge1").textContent = slide.discount
    document.getElementById("heroBadge2").textContent = slide.badge
}

function nextSlide() {
    currentSlide = (currentSlide + 1) % slides.length
    updateSlide()
}

function prevSlide() {
    currentSlide = (currentSlide - 1 + slides.length) % slides.length
    updateSlide()
}

function goToSlide(index) {
    currentSlide = index
    updateSlide()
}

// Auto-advance slides
setInterval(nextSlide, 5000)

// Products Dropdown
document.addEventListener("DOMContentLoaded", () => {
    const productsDropdown = document.getElementById("productsDropdown")
    const productsDropdownMenu = document.getElementById("productsDropdownMenu")

    if (productsDropdown && productsDropdownMenu) {
        productsDropdown.addEventListener("mouseenter", () => {
            productsDropdownMenu.style.display = "block"
        })

        productsDropdown.addEventListener("mouseleave", () => {
            setTimeout(() => {
                if (!productsDropdownMenu.matches(":hover")) {
                    productsDropdownMenu.style.display = "none"
                }
            }, 100)
        })

        productsDropdownMenu.addEventListener("mouseenter", () => {
            productsDropdownMenu.style.display = "block"
        })

        productsDropdownMenu.addEventListener("mouseleave", () => {
            productsDropdownMenu.style.display = "none"
        })
    }

    // Category items animation
    const categoryItems = document.querySelectorAll(".category-item")
    categoryItems.forEach((item, index) => {
        const delay = item.getAttribute("data-delay")
        item.style.animationDelay = delay + "ms"
    })

    //// Add to cart functionality
    //const addToCartButtons = document.querySelectorAll(".product-card .btn-primary")
    //addToCartButtons.forEach((button) => {
    //    button.addEventListener("click", function (e) {
    //        e.preventDefault()

    //        // Add animation
    //        this.textContent = "Added!"
    //        this.style.background = "#10b981"

    //        // Update cart count
    //        const cartBadge = document.querySelector("#cartBtn .action-badge-primary")
    //        if (cartBadge) {
    //            const currentCount = Number.parseInt(cartBadge.textContent)
    //            cartBadge.textContent = currentCount + 1
    //        }

    //        // Reset button after 2 seconds
    //        setTimeout(() => {
    //            this.textContent = "Add to Cart"
    //            this.style.background = ""
    //        }, 2000)
    //    })
    //})

    //// Wishlist functionality
    //const wishlistButtons = document.querySelectorAll(".product-wishlist")
    //wishlistButtons.forEach((button) => {
    //    button.addEventListener("click", function (e) {
    //        e.preventDefault()
    //        e.stopPropagation()

    //        // Toggle active state
    //        this.classList.toggle("active")

    //        // Update wishlist count
    //        const wishlistBadge = document.querySelector("#wishlistBtn .action-badge")
    //        if (wishlistBadge) {
    //            const currentCount = Number.parseInt(wishlistBadge.textContent)
    //            wishlistBadge.textContent = this.classList.contains("active") ? currentCount + 1 : currentCount - 1
    //        }
    //    })
    //})
})
