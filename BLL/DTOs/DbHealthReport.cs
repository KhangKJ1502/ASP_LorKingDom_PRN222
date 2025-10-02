namespace BLL.DTOs
{
    public class DbHealthReport
    {
        public bool Ok { get; set; }
        public string? Message { get; set; }
        public string? Server { get; set; }
        public string? Database { get; set; }
        public string? Version { get; set; }
    }
}
