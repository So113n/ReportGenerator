using Microsoft.AspNetCore.Mvc;
using ReportGenerator.Services;
using Utils;

namespace ReportGenerator.Controllers
{
    [ApiController]
    [Route("api/monitoring")]
    public class MonitoringController : ControllerBase
    {
        private readonly PerformanceMonitorService _monitorService;
        private readonly NotificationService _notificationService;

        public MonitoringController(PerformanceMonitorService monitorService,
                                  NotificationService notificationService)
        {
            _monitorService = monitorService;
            _notificationService = notificationService;
        }  

        [HttpGet("metrics")]
        public IActionResult GetMetrics()
        {
            var metrics = _monitorService.GetMetricsHistory();
            return Ok(metrics);
        }

        [HttpGet("metrics/debug")]
        public IActionResult GetDebug()
        {         
            var m = _monitorService.GetCurrentMetrics();
            return Ok($"CPU {m.CpuUsagePercent}%, Memory {m.MemoryUsageBytes / (1024 * 1024)} MB, Time {DateTime.UtcNow}");
        }
   
        [HttpGet("metrics/current")]
        public IActionResult GetCurrentMetrics()
        {
            var metrics = _monitorService.GetCurrentMetrics();
            return Ok(metrics);
        }

        [HttpGet("alerts")]
        public IActionResult GetAlerts([FromQuery] bool includeResolved = false)
        {
            var alerts = _notificationService.GetAlerts(includeResolved);
            return Ok(alerts);
        }

        [HttpPost("alerts/{id}/resolve")]
        public IActionResult ResolveAlert(Guid id)
        {
            _notificationService.MarkAsResolved(id);
            return Ok();
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var metrics = _monitorService.GetCurrentMetrics();
            var alerts = _notificationService.GetAlerts();

            return Ok(new
            {
                Status = alerts.Any() ? "Warning" : "Healthy",
                Metrics = metrics,
                ActiveAlerts = alerts.Count,
                LastUpdated = DateTime.UtcNow
            });
        }
    }
}
