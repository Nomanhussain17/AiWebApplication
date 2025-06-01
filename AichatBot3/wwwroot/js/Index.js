document.addEventListener('DOMContentLoaded', () => {

    // --- Dark Mode Toggle ---
    const themeToggle = document.getElementById('theme-toggle');
    const body = document.body;
    // Icons are directly in the button in the HTML example
    // const sunIcon = themeToggle.querySelector('.sun-icon');
    // const moonIcon = themeToggle.querySelector('.moon-icon');

    // Function to apply the theme and save preference
    function applyTheme(theme) {
        if (theme === 'dark') {
            body.classList.add('dark-mode');
            localStorage.setItem('theme', 'dark');
            if (themeToggle) themeToggle.setAttribute('title', 'Switch to light mode');
            // Icon visibility handled by CSS based on body class
        } else {
            body.classList.remove('dark-mode');
            localStorage.setItem('theme', 'light');
            if (themeToggle) themeToggle.setAttribute('title', 'Switch to dark mode');
            // Icon visibility handled by CSS
        }
    }

    // Event Listener for the toggle button
    if (themeToggle) {
        themeToggle.addEventListener('click', () => {
            const currentTheme = body.classList.contains('dark-mode') ? 'light' : 'dark';
            applyTheme(currentTheme);
        });
    }

    // Check local storage or OS preference on initial load
    const savedTheme = localStorage.getItem('theme');
    const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;

    if (savedTheme) {
        applyTheme(savedTheme); // Apply saved theme
    } else if (prefersDark) {
        applyTheme('dark'); // Apply OS preference if no saved theme
    } else {
        applyTheme('light'); // Default to light
    }
    // --- End Dark Mode Toggle ---


    // --- Existing JavaScript Code Starts Here ---

    // Header scroll effect
    window.addEventListener('scroll', function () {
        const header = document.querySelector('header');
        if (header) { // Check if header exists
            if (window.scrollY > 50) {
                header.classList.add('header-scrolled');
            } else {
                header.classList.remove('header-scrolled');
            }
        }
    });

    // Mobile menu toggle
    const mobileMenuBtn = document.querySelector('.mobile-menu-btn');
    const navMenu = document.querySelector('nav ul');

    if (mobileMenuBtn && navMenu) { // Check if elements exist
        mobileMenuBtn.addEventListener('click', function () {
            navMenu.classList.toggle('active');
            // Optional: Add ARIA attribute toggling
            const isExpanded = navMenu.classList.contains('active');
            mobileMenuBtn.setAttribute('aria-expanded', isExpanded);
        });
    }


    // Smooth scrolling for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const href = this.getAttribute('href');
            if (href === '#' || href === '') { // Prevent default for empty or # links
                e.preventDefault();
                return;
            }

            const targetId = href;
            try {
                const targetElement = document.querySelector(targetId);
                if (targetElement) {
                    e.preventDefault(); // Only prevent default if target exists
                    const headerOffset = document.querySelector('header')?.offsetHeight || 80; // Get header height dynamically or fallback
                    const elementPosition = targetElement.getBoundingClientRect().top;
                    const offsetPosition = elementPosition + window.pageYOffset - headerOffset;

                    window.scrollTo({
                        top: offsetPosition,
                        behavior: 'smooth'
                    });

                    // Close mobile menu if open and active
                    if (navMenu && navMenu.classList.contains('active')) {
                        navMenu.classList.remove('active');
                        if (mobileMenuBtn) mobileMenuBtn.setAttribute('aria-expanded', 'false');
                    }
                }
            } catch (error) {
                console.warn(`Smooth scroll target not found or invalid selector: ${targetId}`, error);
            }
        });
    });

    // Chatbot functionality (Simplified - using existing demo structure)
    const chatInput = document.querySelector('.chat-input input');
    const chatSendBtn = document.querySelector('.chat-input button');
    const chatMessages = document.querySelector('.chat-messages');

    function addMessage(message, isUser = false) {
        if (!chatMessages) return; // Exit if chat area doesn't exist

        const messageDiv = document.createElement('div');
        messageDiv.classList.add('message', isUser ? 'user' : 'bot');

        const messageContent = document.createElement('div');
        messageContent.classList.add('message-content');
        messageContent.textContent = message; // Use textContent for security

        messageDiv.appendChild(messageContent);
        chatMessages.appendChild(messageDiv);

        // Scroll to bottom smoothly
        chatMessages.scrollTo({ top: chatMessages.scrollHeight, behavior: 'smooth' });
    }

    function handleChatSend() {
        if (!chatInput || !chatSendBtn) return; // Exit if elements don't exist

        const message = chatInput.value.trim();
        if (message) {
            addMessage(message, true);
            chatInput.value = '';
            chatInput.focus(); // Keep focus on input

            // Simulate typing indicator (optional)
            // addMessage("...", false); // Placeholder typing message

            // Simulate AI response
            setTimeout(() => {
                // Remove typing indicator if added
                // const typingIndicator = chatMessages.querySelector('.message.bot:last-child .message-content');
                // if (typingIndicator && typingIndicator.textContent === "...") {
                //     typingIndicator.parentElement.remove();
                // }

                const responses = [
                    "I'd be happy to help with that!",
                    "That's an interesting question. Let me check...",
                    "Here's what I found.",
                    "I can definitely assist with this.",
                    "Tell me more about what you need."
                ];
                const randomResponse = responses[Math.floor(Math.random() * responses.length)];
                addMessage(randomResponse);
            }, 1000 + Math.random() * 500); // Random delay
        }
    }

    if (chatSendBtn) chatSendBtn.addEventListener('click', handleChatSend);
    if (chatInput) {
        chatInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                handleChatSend();
            }
        });
    }


    // Image generator functionality (Simplified - using existing demo structure)
    const promptInput = document.querySelector('.prompt-input input');
    const generatedImageContainer = document.querySelector('.generated-image');
    const generatedImage = generatedImageContainer?.querySelector('img');
    const loadingOverlay = generatedImageContainer?.querySelector('.loading-overlay');
    const styleOptions = document.querySelectorAll('.generator-options .option-item');

    function generateImage() {
        if (!promptInput || !generatedImage || !loadingOverlay || !generatedImageContainer) return; // Ensure elements exist

        if (promptInput.value.trim()) {
            loadingOverlay.classList.add('active');
            generatedImage.style.opacity = '0.5'; // Dim image while loading new one

            // Simulate image generation (replace with actual API call later)
            setTimeout(() => {
                const sampleImages = [ // Use images from your wwwroot/images folder
                    '/images/sample-gen-1.jpg', // Example path
                    '/images/sample-gen-2.jpg',
                    '/images/sample-gen-3.jpg',
                    '/images/ai-brain-network.jpg', // Keep one original
                    '/images/ai-wireframe-head.jpg'
                ];
                // Add placeholder images to your wwwroot/images folder with these names
                // or update the names here to match images you have.

                const randomImage = sampleImages[Math.floor(Math.random() * sampleImages.length)];
                generatedImage.src = randomImage;
                generatedImage.alt = `AI Generated Image based on prompt: ${promptInput.value}`; // Update alt text

                // Wait for image to load before removing overlay
                generatedImage.onload = () => {
                    loadingOverlay.classList.remove('active');
                    generatedImage.style.opacity = '1';
                };
                // Handle image load errors
                generatedImage.onerror = () => {
                    console.error(`Failed to load image: ${randomImage}`);
                    loadingOverlay.classList.remove('active');
                    generatedImage.style.opacity = '1'; // Still show old image or placeholder
                    // Optionally show an error message
                };

            }, 1500 + Math.random() * 1000); // Shorter delay
        } else {
            // Optional: Add feedback if prompt is empty
            promptInput.focus();
            // You could add a temporary shake animation or border color change
        }
    }

    if (promptInput) {
        promptInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter' && promptInput.value.trim()) {
                generateImage();
            }
        });
        // Add button click listener if you add a generate button
        // const generateBtn = document.querySelector('#generate-image-btn'); // Example ID
        // if (generateBtn) generateBtn.addEventListener('click', generateImage);
    }

    // Style options selection
    if (styleOptions.length > 0) {
        styleOptions.forEach(option => {
            option.addEventListener('click', function () {
                styleOptions.forEach(opt => opt.classList.remove('active'));
                this.classList.add('active');
                // Optional: Trigger image generation again with the new style
                // generateImage();
            });
        });
    }


    // Testimonial slider
    const testimonialTrack = document.querySelector('.testimonial-track');
    const testimonialNav = document.querySelector('.testimonial-nav');
    let currentSlide = 0;
    let autoSlideInterval;

    const testimonials = [
        { content: "The AI chatbot is indispensable! It helps brainstorm, draft, and edit. My productivity increased by 40%.", name: "Sarah Johnson", title: "Content Strategist", image: "https://randomuser.me/api/portraits/women/43.jpg" },
        { content: "As a designer, the AI image generator revolutionized my workflow. Quick concept art and inspiration 24/7.", name: "Michael Chen", title: "Graphic Designer", image: "https://randomuser.me/api/portraits/men/32.jpg" },
        { content: "Our marketing team uses both tools for campaigns. ROI is incredible - production time halved, creative output improved.", name: "Jessica Williams", title: "Marketing Director", image: "https://randomuser.me/api/portraits/women/65.jpg" },
        { content: "Integrating the chatbot into our support system reduced response times significantly. Highly recommend!", name: "David Lee", title: "Support Manager", image: "https://randomuser.me/api/portraits/men/54.jpg" } // Added one more
    ];

    function createTestimonialCard(testimonial) {
        // Basic security: escape HTML in user-generated content if applicable
        // For now, assuming these are controlled strings
        return `
            <div class="testimonial-card">
                <div class="testimonial-content">
                    <p>"${testimonial.content}"</p>
                </div>
                <div class="testimonial-author">
                    <div class="author-image">
                        <img src="${testimonial.image}" alt="${testimonial.name}" loading="lazy"> </div>
                    <div class="author-info">
                        <h4>${testimonial.name}</h4>
                        <p>${testimonial.title}</p>
                    </div>
                </div>
            </div>
            `;
    }

    function goToSlide(index) {
        if (!testimonialTrack || !testimonialNav) return;

        currentSlide = (index + testimonials.length) % testimonials.length; // Ensure index wraps around correctly
        testimonialTrack.style.transform = `translateX(-${currentSlide * 100}%)`;

        const dots = testimonialNav.querySelectorAll('.nav-dot');
        dots.forEach((dot, i) => {
            dot.classList.toggle('active', i === currentSlide);
        });
    }

    function startAutoSlide() {
        stopAutoSlide(); // Clear existing interval first
        if (testimonials.length > 1) { // Only auto-slide if more than one testimonial
            autoSlideInterval = setInterval(() => {
                goToSlide(currentSlide + 1);
            }, 5000); // 5 seconds
        }
    }

    function stopAutoSlide() {
        clearInterval(autoSlideInterval);
    }

    // Initialize testimonial slider
    function initTestimonialSlider() {
        if (!testimonialTrack || !testimonialNav) return; // Exit if elements don't exist

        let testimonialHTML = '';
        let dotsHTML = '';
        testimonials.forEach((testimonial, index) => {
            testimonialHTML += createTestimonialCard(testimonial);
            dotsHTML += `<button class="nav-dot ${index === 0 ? 'active' : ''}" aria-label="Go to slide ${index + 1}"></button>`; // Use buttons for accessibility
        });

        testimonialTrack.innerHTML = testimonialHTML;
        testimonialNav.innerHTML = dotsHTML;

        // Add event listeners to new dots
        const dots = testimonialNav.querySelectorAll('.nav-dot');
        dots.forEach((dot, index) => {
            dot.addEventListener('click', () => {
                goToSlide(index);
                stopAutoSlide(); // Optional: Stop auto-slide on manual navigation
                // startAutoSlide(); // Optional: Restart timer after manual nav
            });
        });

        // Add listeners to pause on hover
        const container = document.querySelector('.testimonial-container');
        if (container) {
            container.addEventListener('mouseenter', stopAutoSlide);
            container.addEventListener('mouseleave', startAutoSlide);
        }

        startAutoSlide(); // Start auto-sliding
    }

    initTestimonialSlider();


    // Animation on scroll (Simplified for performance)
    const animatedElements = document.querySelectorAll('.feature-card, .section-title, .chatbot-content, .generator-content, .pricing-card'); // Add more selectors if needed

    const observerOptions = {
        root: null, // relative to the viewport
        rootMargin: '0px',
        threshold: 0.1 // Trigger when 10% of the element is visible
    };

    const observerCallback = (entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
                observer.unobserve(entry.target); // Stop observing once animated
            }
        });
    };

    const scrollObserver = new IntersectionObserver(observerCallback, observerOptions);

    if (animatedElements.length > 0) {
        animatedElements.forEach(element => {
            // Initial styles set inline for immediate effect before JS/CSS loads fully
            element.style.opacity = '0';
            element.style.transform = 'translateY(30px)';
            element.style.transition = 'opacity 0.6s ease-out, transform 0.6s ease-out';
            scrollObserver.observe(element);
        });
    }

}); // End DOMContentLoaded


// --- Make Bootstrap Dropdowns Open on Hover ---
document.addEventListener('DOMContentLoaded', function () {

    // Find all dropdown elements (or target your specific one if needed)
    document.querySelectorAll('.dropdown').forEach(function (dropdownElement) {

        let timeoutId; // Variable to manage hide delay timer
        const hideDelay = 50; // Delay in milliseconds before hiding on mouseleave

        // Get the toggle element within this dropdown
        let toggleElement = dropdownElement.querySelector('[data-bs-toggle="dropdown"]');
        if (!toggleElement) return; // Skip if no toggle found

        // --- Show on Mouse Enter ---
        dropdownElement.addEventListener('mouseenter', function () {
            clearTimeout(timeoutId); // Clear any pending hide timer
            let dropdownInstance = bootstrap.Dropdown.getOrCreateInstance(toggleElement);
            // Use a tiny delay before showing to prevent accidental triggers
            setTimeout(() => {
                // Check if mouse is still over after delay
                if (dropdownElement.matches(':hover')) {
                    dropdownInstance.show();
                }
            }, 0); // Small delay before showing
        });

        // --- Hide on Mouse Leave (with Delay) ---
        dropdownElement.addEventListener('mouseleave', function () {
            let dropdownInstance = bootstrap.Dropdown.getOrCreateInstance(toggleElement);
            // Set a timer to hide the dropdown
            timeoutId = setTimeout(() => {
                dropdownInstance.hide();
            }, hideDelay);
        });

        // --- Keep Open if Mouse Enters Menu ---
        // Optional but improves usability: Prevent hiding if the mouse moves onto the menu itself
        const menuElement = dropdownElement.querySelector('.dropdown-menu');
        if (menuElement) {
            menuElement.addEventListener('mouseenter', function () {
                clearTimeout(timeoutId); // Cancel the hide timer if mouse enters menu
            });
            // Hide when mouse leaves the menu area as well
            menuElement.addEventListener('mouseleave', function () {
                let dropdownInstance = bootstrap.Dropdown.getOrCreateInstance(toggleElement);
                timeoutId = setTimeout(() => {
                    dropdownInstance.hide();
                }, hideDelay);
            });
        }
    });

    // ... your other DOMContentLoaded code like the Enter key handler ...

}); // End DOMContentLoaded (or document.ready if using jQuery)