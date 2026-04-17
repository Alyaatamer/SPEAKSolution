using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPEAK.Abstraction.IRepositories;
using SPEAK.Shared.DTO_s.ChatDto;
using System.Security.Claims;
using SPEAK.Abstraction.IServices;
using SPEAK.Domain.Models.Chat;
using Microsoft.AspNetCore.SignalR;

namespace SPEAK.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController] 
    [Authorize] 
    public class ChatController : ControllerBase
    {
        private readonly IChatRepository _chatRepository;
        private readonly IAdminService _adminService;
        
        public ChatController(IChatRepository chatRepository, IAdminService adminService)
        {
            _chatRepository = chatRepository;
            _adminService = adminService;
        }

        [HttpGet("doctors")]
        public async Task<IActionResult> GetAvailableDoctors()
        {
            var doctors = await _adminService.GetAllDoctorsAsync();
            return Ok(doctors);
        }
        
        [HttpGet("history/{otherUserId}")]
        public async Task<IActionResult> GetChatHistory(string otherUserId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return Unauthorized(); 
            }

            var messages = await _chatRepository.GetChatHistoryAsync(currentUserId, otherUserId);
            var messageDtos = messages.Select(m => new MessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Content = m.Content,
                Timestamp = m.Timestamp,
                IsRead = m.IsRead,
                ReadAt = m.ReadAt,
                MessageType = (int)m.MessageType,
                MediaUrl = m.MediaUrl
            });
            return Ok(messageDtos);
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var conversations = await _chatRepository.GetUserConversationsAsync(userId);
            return Ok(conversations);
        }
    }
}
