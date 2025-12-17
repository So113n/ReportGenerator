using Microsoft.Extensions.Logging;
using ReportGenerator.Models;

namespace ReportGenerator.Services
{
    public sealed class GlpiIncidentService
    {
        private readonly GlpiClient _client;
        private readonly GlpiOptions _opt;
        private readonly ILogger<GlpiIncidentService> _logger;

        public GlpiIncidentService(GlpiClient client, GlpiOptions opt, ILogger<GlpiIncidentService> logger)
        {
            _client = client;
            _opt = opt;
            _logger = logger;
        }

        public async Task CreateFromLogEventAsync(LogEventIncident e, CancellationToken ct)
        {
            var title = $"{e.Source} (EventId={e.EventId})";

            var description =
                $"Time: {e.Timestamp:yyyy-MM-dd HH:mm:ss}\n" +
                $"Source: {e.Source}\n" +
                $"EventId: {e.EventId}\n" +
                $"Message: {e.Message}\n" +
                $"Raw: {e.RawLine}";

            _logger.LogInformation("Creating GLPI ticket from log event: {Title}", title);

            var ticketId = await _client.CreateTicketAsync(
                title: title,
                content: description,
                categoryId: _opt.CategoryId,
                recipientUserId: _opt.RecipientUserId,
                entityId: _opt.EntityId,
                status: _opt.Status,
                priority: _opt.Priority,
                ct: ct);

            _logger.LogInformation("GLPI ticket created. TicketId={TicketId}", ticketId);
        }
    }
}
