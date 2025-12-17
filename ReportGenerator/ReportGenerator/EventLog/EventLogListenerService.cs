using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ReportGenerator.Services
{
    public sealed class EventLogListenerService : BackgroundService
    {
        private readonly ILogger<EventLogListenerService> _logger;

        public EventLogListenerService(ILogger<EventLogListenerService> logger)
        {
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // EventLog в Linux-контейнере не работает (поддержка только Windows). 
            if (!OperatingSystem.IsWindows())
            {
                _logger.LogInformation("EventLogListenerService disabled (container/Linux). Using events.log watcher instead.");
                return Task.CompletedTask;
            }

            _logger.LogInformation("EventLogListenerService enabled on Windows host.");
            return Task.CompletedTask;
        }
    }
}
