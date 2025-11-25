namespace ReportGenerator.Models
{
    public class PerformanceMetrics
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public double CpuUsagePercent { get; set; }
        public long MemoryUsageBytes { get; set; }
        public long MemoryAvailableBytes { get; set; }
        public int ActiveRequests { get; set; }
        public double RequestProcessingTimeMs { get; set; }
        public int ExceptionCount { get; set; }
        public int DatabaseQueryCount { get; set; }
        public double DatabaseQueryTimeMs { get; set; }
    }
}
