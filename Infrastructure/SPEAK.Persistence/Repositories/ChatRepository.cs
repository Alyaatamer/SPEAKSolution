using Microsoft.EntityFrameworkCore;
using SPEAK.Abstraction.IRepositories;
using SPEAK.Domain.Models.Chat;
using SPEAK.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPEAK.Shared.DTO_s.ChatDto;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Persistence.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly UserIdentityDbContext _context;
        public ChatRepository(UserIdentityDbContext context)
        {
            _context = context;
        }
        public async Task<Message> SaveMessageAsync(Message message)
        {
            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();
            return message;
        }
        public async Task<IEnumerable<Message>> GetChatHistoryAsync(string user1Id, string user2Id)
        {
            return await _context.Messages.Where(m => (m.SenderId == user1Id && m.ReceiverId == user2Id) ||
                                                      (m.SenderId == user2Id && m.ReceiverId == user1Id))
                                                        .OrderBy(m => m.Timestamp) 
                                                        .ToListAsync();
        }

        public async Task MarkMessagesAsDeliveredAsync(string senderId, string receiverId)
        {
            var undeliveredMessages = await _context.Messages
                .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId && !m.IsDelivered)
                .ToListAsync();

            if (undeliveredMessages.Any())
            {
                var now = DateTime.UtcNow;
                foreach (var msg in undeliveredMessages)
                {
                    msg.IsDelivered = true;
                    msg.DeliveredAt = now;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkMessagesAsReadAsync(string senderId, string receiverId)
        {
            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId && !m.IsRead)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                    msg.ReadAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(string userId)
        {
            var rawConversations = await _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Select(g => new
                {
                    OtherUserId = g.Key,
                    LastMessage = g.OrderByDescending(m => m.Timestamp).FirstOrDefault(),
                    UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead)
                })
                .ToListAsync();

            var conversationDtos = new List<ConversationDto>();

            foreach (var conv in rawConversations)
            {
                var otherUser = await _context.Users.FindAsync(conv.OtherUserId);
                if (otherUser != null && conv.LastMessage != null)
                {
                    conversationDtos.Add(new ConversationDto
                    {
                        OtherUserId = conv.OtherUserId,
                        OtherUserName = otherUser.DisplayName ?? "Unknown",
                        LastMessage = conv.LastMessage.Content,
                        LastMessageTime = conv.LastMessage.Timestamp,
                        UnreadCount = conv.UnreadCount,
                        LastMessageType = (int)conv.LastMessage.MessageType
                    });
                }
            }

            return conversationDtos.OrderByDescending(c => c.LastMessageTime);
        }
    }
}
