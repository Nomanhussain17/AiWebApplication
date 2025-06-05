using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AichatBot3.Service;
using Markdig;
using Markdig.SyntaxHighlighting;

[Authorize(Roles = "User")]
public class ChatBotController : Controller
{
    private readonly ChatGptService _chatGptService;

    public ChatBotController(ChatGptService chatGptService)
    {
        _chatGptService = chatGptService;
    }

    public IActionResult Index()
    {
        return View("ChatBot");
    }

    [HttpPost]
    public async Task<IActionResult> GetChatResponse(string userMessage)
    {
        var response = await _chatGptService.GetChatResponse(userMessage);

        // Convert Markdown to HTML
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseGenericAttributes()
            .Build();

        var htmlResponse = Markdown.ToHtml(response, pipeline);

        return Json(new { reply = htmlResponse });
    }
}