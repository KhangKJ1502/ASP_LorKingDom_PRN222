namespace DAL.Models
{
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string ConversationId { get; set; } = default!;
        public string SenderId { get; set; } = default!;
        public string Text { get; set; } = default!;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

    }
}
