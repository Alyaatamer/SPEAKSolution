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
                Timestamp = DateTime.UtcNow,
                MessageType = (MessageType)dto.MessageType,
                MediaUrl = dto.MediaUrl
            };
            
            var savedMessage = await _chatRepository.SaveMessageAsync(message);
            
            var messageDto = new MessageDto
            {
                Id = savedMessage.Id,
                SenderId = savedMessage.SenderId,
                ReceiverId = savedMessage.ReceiverId,
                Content = savedMessage.Content,
                Timestamp = savedMessage.Timestamp,
                IsDelivered = savedMessage.IsDelivered,
                DeliveredAt = savedMessage.DeliveredAt,
                IsRead = savedMessage.IsRead,
                ReadAt = savedMessage.ReadAt,
                MessageType = (int)savedMessage.MessageType,
                MediaUrl = savedMessage.MediaUrl
            };
            
            await Clients.User(dto.ReceiverId).SendAsync("ReceiveMessage", messageDto);
            await Clients.User(senderId).SendAsync("ReceiveMessage", messageDto);
        }

        public async Task MarkAsRead(string senderId)
        {
            var receiverId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (receiverId == null || senderId == null) return;

            // Mark in the database that 'receiverId' has read messages sent by 'senderId'
            await _chatRepository.MarkMessagesAsReadAsync(senderId, receiverId);

            // Notify the original sender that their messages were read
            await Clients.User(senderId).SendAsync("MessagesRead", receiverId);
        }

        public async Task MarkAsDelivered(string senderId)
        {
            var receiverId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (receiverId == null || senderId == null) return;

            await _chatRepository.MarkMessagesAsDeliveredAsync(senderId, receiverId);
            await Clients.User(senderId).SendAsync("MessagesDelivered", receiverId);
        }
        // --- WebRTC Signaling Methods ---

        public async Task CallUser(string receiverId, string sdpOffer, bool isVideo)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (senderId == null) return;

            // Notify the receiver about the incoming call
            // We pass senderId so the receiver knows who is calling
            await Clients.User(receiverId).SendAsync("IncomingCall", senderId, sdpOffer, isVideo);
        }

        public async Task AnswerCall(string callerId, string sdpAnswer)
        {
            var receiverId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (receiverId == null) return;

            // Notify the caller that the call was answered, and pass the SDP Answer
            // We pass receiverId to let the caller know who answered
            await Clients.User(callerId).SendAsync("CallAnswered", receiverId, sdpAnswer);
        }

        public async Task SendIceCandidate(string targetUserId, string candidate, string sdpMid, int sdpMLineIndex)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (senderId == null) return;

            // Pass the ICE candidate to the target user
            await Clients.User(targetUserId).SendAsync("ReceiveIceCandidate", senderId, candidate, sdpMid, sdpMLineIndex);
        }

        public async Task EndCall(string targetUserId)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (senderId == null) return;

            await Clients.User(targetUserId).SendAsync("CallEnded", senderId);
        }

        public async Task RejectCall(string callerId)
        {
            var receiverId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (receiverId == null) return;

            await Clients.User(callerId).SendAsync("CallRejected", receiverId);
        }
    }
}
