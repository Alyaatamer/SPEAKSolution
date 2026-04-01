using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SPEAK.Abstraction.IRepositories;
using SPEAK.Domain.Models.Chat;
using SPEAK.Shared.DTO_s.ChatDto;
using SPEAK.Web.Hubs;
using System.Security.Claims;

namespace SPEAK.Web.Controllers
{
    [Route("api/TestChat")]
    [ApiController] 
    [Authorize] 
    public class TestChatController : ControllerBase
    {
        private readonly IChatRepository _chatRepository;
        private readonly IHubContext<ChatHub> _hubContext;

        public TestChatController(IChatRepository chatRepository, IHubContext<ChatHub> hubContext)
        {
            _chatRepository = chatRepository;
            _hubContext = hubContext;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessageTest([FromBody] SendMessageDto dto)
        {
            var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (senderId == null) return Unauthorized();

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = dto.ReceiverId,
                Content = dto.Content,
                Timestamp = DateTime.UtcNow 
            };
            
            var savedMessage = await _chatRepository.SaveMessageAsync(message);
            
            var messageDto = new MessageDto
            {
                Id = savedMessage.Id,
                SenderId = savedMessage.SenderId,
                ReceiverId = savedMessage.ReceiverId,
                Content = savedMessage.Content,
                Timestamp = savedMessage.Timestamp
            };
            
            // This is the Magic line that instantly broadcasts it to Flutter!
            await _hubContext.Clients.User(dto.ReceiverId).SendAsync("ReceiveMessage", messageDto);
            
            return Ok(messageDto);
        }
    }
}
