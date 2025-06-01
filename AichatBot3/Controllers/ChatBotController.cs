using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AichatBot3.Service;

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
        return Json(new { reply = response });
    }
}
