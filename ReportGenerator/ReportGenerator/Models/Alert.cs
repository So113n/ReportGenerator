using Org.BouncyCastle.Tls;

namespace ReportGenerator.Models
{
    public class Alert
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public AlertLevel Level { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public enum AlertLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }
}
