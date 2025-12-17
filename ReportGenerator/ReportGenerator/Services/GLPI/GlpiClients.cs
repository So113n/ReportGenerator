using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ReportGenerator.Models;

namespace ReportGenerator.Services
{
    public sealed class GlpiClient
    {
        private readonly HttpClient _http;
        private readonly GlpiOptions _opt;
        private readonly ILogger<GlpiClient> _logger;

        public GlpiClient(HttpClient http, GlpiOptions opt, ILogger<GlpiClient> logger)
        {
            _http = http;
            _opt = opt;
            _logger = logger;
        }

        private string Base(string path)
        {
            var b = _opt.BaseUrl.TrimEnd('/');
            path = path.TrimStart('/');
            return $"{b}/{path}";
        }

        public async Task<string> InitSessionAsync(CancellationToken ct)
        {
            var url = Base("initSession");

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("App-Token", _opt.AppToken);
            req.Headers.Add("Authorization", $"user_token {_opt.UserToken}");

            _logger.LogInformation("GLPI initSession -> {Url}", url);

            using var resp = await _http.SendAsync(req, ct);
            var text = await resp.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("GLPI initSession <- {Status} {Body}", (int)resp.StatusCode, text);

            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(text);
            if (!doc.RootElement.TryGetProperty("session_token", out var st))
                throw new InvalidOperationException($"GLPI initSession: session_token not found. Body={text}");

            var token = st.GetString();
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException($"GLPI initSession: session_token empty. Body={text}");

            return token;
        }

        public async Task KillSessionAsync(string sessionToken, CancellationToken ct)
        {
            var url = Base("killSession");

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("App-Token", _opt.AppToken);
            req.Headers.Add("Session-Token", sessionToken);

            _logger.LogInformation("GLPI killSession -> {Url}", url);

            using var resp = await _http.SendAsync(req, ct);
            var text = await resp.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("GLPI killSession <- {Status} {Body}", (int)resp.StatusCode, text);
        }

        public async Task<int> CreateTicketAsync(
            string title,
            string content,
            int categoryId,
            int recipientUserId,
            int entityId,
            int status,
            int priority,
            CancellationToken ct)
        {
            var sessionToken = await InitSessionAsync(ct);

            try
            {
                var url = Base("Ticket");

                var payload = new
                {
                    input = new
                    {
                        name = title,
                        content = content,
                        itilcategories_id = categoryId,
                        users_id_recipient = recipientUserId,
                        entities_id = entityId,
                        status = status,
                        priority = priority
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Add("App-Token", _opt.AppToken);
                req.Headers.Add("Session-Token", sessionToken);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("GLPI create Ticket -> {Url}", url);
                _logger.LogInformation("GLPI create Ticket payload -> {Json}", json);

                using var resp = await _http.SendAsync(req, ct);
                var text = await resp.Content.ReadAsStringAsync(ct);

                _logger.LogInformation("GLPI create Ticket <- {Status} {Body}", (int)resp.StatusCode, text);

                resp.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var id))
                    return id;

                if (doc.RootElement.TryGetProperty("data", out var dataEl) &&
                    dataEl.ValueKind == JsonValueKind.Object &&
                    dataEl.TryGetProperty("id", out var id2El) &&
                    id2El.TryGetInt32(out var id2))
                    return id2;

                throw new InvalidOperationException($"GLPI create Ticket: ticket id not found. Body={text}");
            }
            finally
            {
                await KillSessionAsync(sessionToken, ct);
            }
        }
    }
}
