namespace DAL.Models
{
    public class ChatUser
    {
        public string UserId { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public bool IsStaff { get; set; }

    }
}
