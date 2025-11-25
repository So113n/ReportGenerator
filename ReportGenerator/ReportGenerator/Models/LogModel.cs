namespace ReportGenerator.Models
{
    public class LogModel
    {
        public List<string> LogEntries { get; set; } = new List<string>();
        public string LogFilePath { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
