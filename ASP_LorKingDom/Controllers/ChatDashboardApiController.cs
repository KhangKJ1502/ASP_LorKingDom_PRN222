using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers;

[ApiController]
[Route("api/chat-dashboard")]
public class ChatDashboardApiController : ControllerBase
{
    private readonly IChatService _chatService;
    public ChatDashboardApiController(IChatService chatService)
    {
        _chatService = chatService;
    }
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] string? staffId)
    {
        // Ưu tiên query; nếu không có thì lấy từ cookie
        staffId ??= Request.Cookies["staffId"];

        if (string.IsNullOrWhiteSpace(staffId))
            return BadRequest("Missing staffId");

        var items = await _chatService.GetStaffConversationsAsync(staffId);
        return Ok(items);
    }
}
