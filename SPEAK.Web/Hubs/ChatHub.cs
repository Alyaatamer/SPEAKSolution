using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SPEAK.Abstraction.IRepositories;
using SPEAK.Domain.Models.Chat;
using SPEAK.Shared.DTO_s.ChatDto;
using System.Security.Claims;

namespace SPEAK.Web.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatRepository _chatRepository;
        public ChatHub(IChatRepository chatRepository)
        {
            _chatRepository = chatRepository;
        }
        
        public async Task SendMessage(SendMessageDto dto)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (senderId == null) return;

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
            
            await Clients.User(dto.ReceiverId).SendAsync("ReceiveMessage", messageDto);
            await Clients.User(senderId).SendAsync("ReceiveMessage", messageDto);
        }
    }
}
