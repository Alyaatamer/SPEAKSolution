using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Domain.Models.Chat
{
    public class Message
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Read Receipts
        public bool IsDelivered { get; set; } = false;
        public DateTime? DeliveredAt { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        // Media Sharing
        public MessageType MessageType { get; set; } = MessageType.Text;
        public string? MediaUrl { get; set; }
    }
}
