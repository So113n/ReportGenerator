using ReportGenerator.Models;

namespace ReportGenerator.Services
{
    public class NotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly NotificationConfig _config;
        private readonly List<Alert> _alerts = new();
        private readonly object _lock = new();

        public NotificationService(ILogger<NotificationService> logger, NotificationConfig config)
        {
            _logger = logger;
            _config = config;
        }

        public void SendAlert(Alert alert)
        {
            lock (_lock)
            {
                _alerts.Add(alert);

                // Храним только последние 50 уведомлений
                if (_alerts.Count > 50)
                {
                    _alerts.RemoveAt(0);
                }
            }

            // Логирование
            if (_config.EnableLogging)
            {
                var logMessage = $"[{alert.Level}] {alert.Title}: {alert.Message}";

                switch (alert.Level)
                {
                    case AlertLevel.Info:
                        _logger.LogInformation(logMessage);
                        break;
                    case AlertLevel.Warning:
                        _logger.LogWarning(logMessage);
                        break;
                    case AlertLevel.Error:
                        _logger.LogError(logMessage);
                        break;
                    case AlertLevel.Critical:
                        _logger.LogCritical(logMessage);
                        break;
                }
            }

            // In-App уведомления
            if (_config.EnableInAppNotifications)
            {
                // Здесь можно добавить логику для веб-сокетов или SignalR
                _logger.LogInformation("In-App уведомление: {Alert}", alert.Title);
            }

            // Email уведомления
            if (_config.EnableEmailNotifications && _config.EmailRecipients.Any())
            {
                SendEmailNotification(alert);
            }
        }

        private void SendEmailNotification(Alert alert)
        {
            try
            {
                // Упрощенная реализация email отправки
                _logger.LogInformation("Отправка email уведомления: {Subject}", alert.Title);

                // Здесь можно интегрировать с System.Net.Mail или другим email клиентом
                /*
                using var client = new SmtpClient("smtp.example.com");
                var mailMessage = new MailMessage
                {
                    Subject = $"[{alert.Level}] {alert.Title}",
                    Body = alert.Message,
                    IsBodyHtml = false
                };

                foreach (var recipient in _config.EmailRecipients)
                {
                    mailMessage.To.Add(recipient);
                }

                client.Send(mailMessage);
                */
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке email уведомления");
            }
        }

        public List<Alert> GetAlerts(bool includeResolved = false)
        {
            lock (_lock)
            {
                return includeResolved
                    ? new List<Alert>(_alerts)
                    : _alerts.Where(a => !a.IsResolved).ToList();
            }
        }

        public void MarkAsResolved(Guid alertId)
        {
            lock (_lock)
            {
                var alert = _alerts.FirstOrDefault(a => a.Id == alertId);
                if (alert != null)
                {
                    alert.IsResolved = true;
                    alert.ResolvedAt = DateTime.UtcNow;
                }
            }
        }

        public int GetUnreadAlertsCount() => GetAlerts().Count;
    }
}
