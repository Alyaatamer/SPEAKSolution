using SPEAK.Domain.Models.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Abstraction.IRepositories
{
    public interface IChatRepository
    {
        Task<Message> SaveMessageAsync(Message message);
        Task<IEnumerable<Message>> GetChatHistoryAsync(string user1Id, string user2Id);
        Task MarkMessagesAsDeliveredAsync(string senderId, string receiverId);
        Task MarkMessagesAsReadAsync(string senderId, string receiverId);
        Task<IEnumerable<SPEAK.Shared.DTO_s.ChatDto.ConversationDto>> GetUserConversationsAsync(string userId);
    }
}
