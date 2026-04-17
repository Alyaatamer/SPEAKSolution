using System;

namespace SPEAK.Shared.DTO_s.ChatDto
{
    public class ConversationDto
    {
        public string OtherUserId { get; set; } = string.Empty;
        public string OtherUserName { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
        public int LastMessageType { get; set; }
    }
}
