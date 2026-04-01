using Microsoft.EntityFrameworkCore;
using SPEAK.Abstraction.IRepositories;
using SPEAK.Domain.Models.Chat;
using SPEAK.Persistence.Contexts;
using System;
using System.Collections.Generic;
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
    }
}
