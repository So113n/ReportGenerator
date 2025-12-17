namespace ReportGenerator.Models
{
    public sealed class GlpiOptions
    {
        public string BaseUrl { get; set; } = "http://host.docker.internal/glpi/apirest.php";
        public string AppToken { get; set; } = "";
        public string UserToken { get; set; } = "";

        public int EntityId { get; set; } = 0;
        public int CategoryId { get; set; } = 15;
        public int RecipientUserId { get; set; } = 6;

        public int Priority { get; set; } = 3;
        public int Status { get; set; } = 1;
    }
}
