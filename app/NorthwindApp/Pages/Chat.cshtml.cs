using Microsoft.AspNetCore.Mvc.RazorPages;
using NorthwindApp.Services;

namespace NorthwindApp.Pages;

public class ChatModel : PageModel
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatModel> _logger;

    public ChatModel(IChatService chatService, ILogger<ChatModel> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public bool IsGenAIEnabled => _chatService.IsGenAIEnabled;

    public void OnGet()
    {
    }
}
