using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Shared.DTO_s.ChatDto
{
    public class SendMessageDto
    {
        public string ReceiverId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        
        public int MessageType { get; set; } = 0; // Default to Text
        public string? MediaUrl { get; set; }
    }
}
