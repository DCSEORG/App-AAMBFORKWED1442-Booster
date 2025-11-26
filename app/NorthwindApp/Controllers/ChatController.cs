using Microsoft.AspNetCore.Mvc;
using NorthwindApp.Services;

namespace NorthwindApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Send a chat message and get AI response
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Message cannot be empty" });
        }

        _logger.LogInformation("Chat request received: {Message}", request.Message);

        var response = await _chatService.GetChatResponseAsync(request.Message, request.History);

        return Ok(new ChatResponse
        {
            Response = response,
            GenAIEnabled = _chatService.IsGenAIEnabled
        });
    }

    /// <summary>
    /// Check if GenAI is enabled
    /// </summary>
    [HttpGet("status")]
    public ActionResult<GenAIStatus> GetStatus()
    {
        return Ok(new GenAIStatus
        {
            Enabled = _chatService.IsGenAIEnabled
        });
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatMessageInfo>? History { get; set; }
}

public class ChatResponse
{
    public string Response { get; set; } = string.Empty;
    public bool GenAIEnabled { get; set; }
}

public class GenAIStatus
{
    public bool Enabled { get; set; }
}
