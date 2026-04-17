using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Shared.DTO_s.ChatDto
{
    public class ChatBotRequestDto
    {
        public string? Message { get; set; }
        public string? SessionId { get; set; } // ممكن تستخدمها لو عايز تحتفظ بسياق المحادثة بين الطلبات

    }
}
