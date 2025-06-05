// ===== GLOBAL VARIABLES =====
let abortController = null;
let isResponding = false;
let typingInterval = null; // Added for animation

// ===== EVENT HANDLERS =====
function handleKeyPress(event) {
    if (event.key === "Enter") {
        sendMessage();
    }
}

// ===== MAIN CHAT FUNCTION =====
function sendMessage() {
    var userMessage = $("#userMessage").val().trim();
    if (userMessage === "" || isResponding) return;

    isResponding = true;

    // Update send button to "Stop Response"
    $("#sendButton")
        .text("Stop Response")
        .addClass("btn-danger")
        .removeClass("btn-primary")
        .attr("onclick", "stopResponse()");

    // Hide default message
    $(".default-message").hide();

    // Add user message to chat
    $("#chatBox").append('<div class="message user-message">' + escapeHtml(userMessage) + '</div>');

    // Create loading spinner
    var loadingId = "loading_" + new Date().getTime();
    var loadingMessage = `
        <div class="message bot-message bg-transparent" id="${loadingId}">
            <div class="spinner-border spinner-border-sm text-light" role="status"></div>
        </div>`;
    $("#chatBox").append(loadingMessage);

    // Clear input and scroll
    $("#userMessage").val("");
    $(".chat-box").scrollTop($(".chat-box")[0].scrollHeight);

    // ===== AJAX REQUEST =====
    $.ajax({
        url: "/ChatBot/GetChatResponse",
        type: "POST",
        data: { userMessage: userMessage },
        success: function (response) {
            // Remove loading spinner
            $("#" + loadingId).remove();

            // Create bot message container
            var botMessageDiv = $('<div class="message bot-message chat-message"></div>');
            $("#chatBox").append(botMessageDiv);

            // Check if response contains complex HTML (tables, code blocks)
            if (containsComplexHTML(response.reply)) {
                // Show complex HTML immediately
                botMessageDiv.html(response.reply);
                finishResponse();
            } else {
                // Use typing animation for simple text
                typeMessage(response.reply, botMessageDiv);
            }
        },
        error: function (xhr, status, error) {
            $("#" + loadingId).replaceWith('<div class="message bot-message text-danger">Error: Unable to get response.</div>');
            resetSendButton();
        }
    });
}

// ===== ANIMATION FUNCTIONS =====
function containsComplexHTML(text) {
    // Check for tables, code blocks, lists, headers
    return text.includes('<table') || text.includes('<pre') || text.includes('<code') ||
        text.includes('<ul') || text.includes('<ol') || text.includes('<h1') ||
        text.includes('<h2') || text.includes('<h3');
}

function typeMessage(htmlContent, targetDiv) {
    // For HTML content, we'll extract text and type it, then apply HTML
    var tempDiv = $('<div>').html(htmlContent);
    var textContent = tempDiv.text(); // Get plain text

    var i = 0;
    targetDiv.html(''); // Clear content

    typingInterval = setInterval(function () {
        if (i < textContent.length) {
            // Add one character at a time
            var currentText = textContent.substring(0, i + 1);
            targetDiv.text(currentText);

            // Auto-scroll every 10 characters
            if (i % 10 === 0) {
                $(".chat-box").scrollTop($(".chat-box")[0].scrollHeight);
            }

            i++;
        } else {
            // Animation complete - now apply full HTML formatting
            clearInterval(typingInterval);
            typingInterval = null;
            targetDiv.html(htmlContent); // Apply full HTML
            finishResponse();
        }
    }, 10); // 20ms per character (adjust for speed)
}

function finishResponse() {
    // Scroll to bottom
    $(".chat-box").scrollTop($(".chat-box")[0].scrollHeight);

    // Apply syntax highlighting
    if (typeof Prism !== 'undefined') {
        Prism.highlightAll();
    }

    // Reset button
    resetSendButton();
}

// ===== CONTROL FUNCTIONS =====
function stopResponse() {
    if (abortController) {
        abortController.abort();
        abortController = null;
    }

    // Stop typing animation
    if (typingInterval) {
        clearInterval(typingInterval);
        typingInterval = null;
    }

    resetSendButton();
}

function resetSendButton() {
    isResponding = false;
    $("#sendButton")
        .text("Send")
        .removeClass("btn-danger")
        .addClass("btn-primary")
        .attr("onclick", "sendMessage()");
}

// ===== UTILITY FUNCTIONS =====
function escapeHtml(text) {
    var map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, function (m) { return map[m]; });
}


// ===== CODE COPY FUNCTIONALITY =====
$(document).on('click', '.chat-message pre', function (e) {
    // Only trigger if clicking the copy area (top-right)
    var rect = this.getBoundingClientRect();
    var clickX = e.clientX - rect.left;
    var clickY = e.clientY - rect.top;

    // Check if click is in top-right area (copy button area)
    if (clickX > rect.width - 80 && clickY < 40) {
        var codeText = $(this).find('code').text() || $(this).text();

        // Copy to clipboard
        navigator.clipboard.writeText(codeText).then(function () {
            // Show success feedback
            var $pre = $(e.target).closest('pre');
            var originalContent = $pre.attr('data-original') || $pre.css('content');

            // Change button text temporarily
            $pre.css('--copy-text', '"Copied!"');
            setTimeout(function () {
                $pre.css('--copy-text', '"Copy"');
            }, 2000);

        }).catch(function () {
            alert('Failed to copy code');
        });
    }
});



