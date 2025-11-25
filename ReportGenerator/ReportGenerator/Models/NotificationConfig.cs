namespace ReportGenerator.Models
{
    public class NotificationConfig
    {
        public double CpuThresholdPercent { get; set; } = 80;
        public long MemoryThresholdBytes { get; set; } = 1024 * 1024 * 1024; // 1GB
        public int RequestTimeThresholdMs { get; set; } = 1000;
        public int ExceptionThresholdPerMinute { get; set; } = 10;
        public int DatabaseQueryTimeThresholdMs { get; set; } = 500;

        public List<string> EmailRecipients { get; set; } = new();
        public bool EnableEmailNotifications { get; set; }
        public bool EnableInAppNotifications { get; set; } = true;
        public bool EnableLogging { get; set; } = true;
    }
}
