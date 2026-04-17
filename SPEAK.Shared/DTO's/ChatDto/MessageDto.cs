using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Shared.DTO_s.ChatDto
{
    public class MessageDto
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        
        public bool IsDelivered { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        
        // Use int to make it easier for Flutter parsing, or string if prefer
        public int MessageType { get; set; } 
        public string? MediaUrl { get; set; }
    }
}
