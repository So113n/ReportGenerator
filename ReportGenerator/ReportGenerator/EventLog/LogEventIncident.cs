namespace ReportGenerator.Models
{
    public sealed class LogEventIncident
    {
        public DateTime Timestamp { get; init; }
        public string Source { get; init; } = "";
        public int EventId { get; init; }
        public string Message { get; init; } = "";
        public string RawLine { get; init; } = "";
    }
}
