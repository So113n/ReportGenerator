using ReportGenerator.Services;
using System.Diagnostics;

namespace ReportGenerator.Middleware
{
    public class PerformanceMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly PerformanceMonitorService _monitorService;

        public PerformanceMonitoringMiddleware(RequestDelegate next, PerformanceMonitorService monitorService)
        {
            _next = next;
            _monitorService = monitorService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            _monitorService.IncrementRequestCounter();

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _monitorService.IncrementExceptionCounter();
                throw;
            }
            finally
            {
                stopwatch.Stop();

                // Логирование медленных запросов
                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    _monitorService.IncrementExceptionCounter(); // Учитываем как "медленный запрос"
                }
            }
        }
    }
}
