let abortController = null; // To track request cancellation
let typingInterval = null; // To stop typing animation
let isResponding = false; // Prevent multiple requests

function handleKeyPress(event) {
    if (event.key === "Enter") {
        sendMessage();
    }
}

/*window.onload = function () {
    document.getElementById("userMessage").focus();
};*/

function sendMessage() {
    var userMessage = $("#userMessage").val().trim();
    if (userMessage === "" || isResponding) return;

    isResponding = true; // Block multiple requests

    // Change button to "Stop Response"
    $("#sendButton").text("Stop Response").addClass("btn-danger").removeClass("btn-primary").attr("onclick", "stopResponse()");

    // Hide default message (if it's still visible)
    $(".default-message").hide();

    // Append user message to chat
    $("#chatBox").append('<div class="message user-message">' + userMessage + '</div>');

    // Create a unique ID for the loading message
    var loadingId = "loading_" + new Date().getTime();
    var loadingMessage = `
            <div class="message bot-message bg-transparent" id="${loadingId}">
                <div class="spinner-border spinner-border-sm text-light" role="status"></div>
            </div>`;
    $("#chatBox").append(loadingMessage);

    // Clear input field
    $("#userMessage").val("");

    // Auto-scroll
    $(".chat-box").scrollTop($(".chat-box")[0].scrollHeight);

    // Create an AbortController for request cancellation
    abortController = new AbortController();

    $.ajax({
        url: "/ChatBot/GetChatResponse", // Fixed URL to match controller route
        type: "POST",
        data: { userMessage: userMessage },
        success: function (response) {
            $("#" + loadingId).remove(); // Remove loading spinner

            // Create a container for AI response
            var botMessageDiv = $('<div class="message bot-message"></div>');
            $("#chatBox").append(botMessageDiv);

            // Animate response letter by letter
            typeResponse(response.reply, botMessageDiv, function () {
                resetSendButton(); // Restore button after animation
            });
        },
        error: function (xhr, status, error) {
            if (status === "abort") {
                $("#" + loadingId).replaceWith('<div class="message bot-message text-warning">Response stopped.</div>');
            } else {
                console.error("Error details:", error);
                $("#" + loadingId).replaceWith('<div class="message bot-message text-danger">Error: Unable to get response. Please try again later.</div>');
            }
            resetSendButton();
        }
    });
}

// Function to stop response
function stopResponse() {
    if (abortController) {
        abortController.abort(); // Cancel AJAX request
        abortController = null;
    }
    if (typingInterval) {
        clearInterval(typingInterval); // Stop text animation
        typingInterval = null;
    }
    resetSendButton();
}

// Function to reset button to "Send"
function resetSendButton() {
    isResponding = false; // Allow new requests
    $("#sendButton").text("Send").removeClass("btn-danger").addClass("btn-primary").attr("onclick", "sendMessage()");
}

// Function to animate response letter by letter
function typeResponse(text, targetDiv, callback) {
    let i = 0;
    typingInterval = setInterval(() => {
        if (i < text.length) {
            targetDiv.append(text.charAt(i));
            $(".chat-box").scrollTop($(".chat-box")[0].scrollHeight);
            i++;
        } else {
            clearInterval(typingInterval);
            typingInterval = null;
            if (callback) callback();
        }
    }, 3); // Adjust typing speed
}

