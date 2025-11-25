using ReportGenerator.Models;
using System.Diagnostics;

namespace ReportGenerator.Services
{
    public class PerformanceMonitorService : IHostedService, IDisposable
    {
        private readonly ILogger<PerformanceMonitorService> _logger;
        private readonly NotificationService _notificationService;
        private readonly NotificationConfig _config;

        private Timer? _timer;
        private readonly List<PerformanceMetrics> _metricsHistory = new();
        private int _requestCounter;
        private int _exceptionCounter;
        private DateTime _lastResetTime = DateTime.UtcNow;
        private readonly object _lock = new();

        public PerformanceMonitorService(ILogger<PerformanceMonitorService> logger,
                                       NotificationService notificationService,
                                       NotificationConfig config)
        {
            _logger = logger;
            _notificationService = notificationService;
            _config = config;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Сервис мониторинга производительности запущен");

            _timer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Dispose();
            _logger.LogInformation("Сервис мониторинга производительности остановлен");

            return Task.CompletedTask;
        }

        private void CollectMetrics(object? state)
        {
            try
            {
                var metrics = new PerformanceMetrics
                {
                    CpuUsagePercent = GetCpuUsage(),
                    MemoryUsageBytes = Process.GetCurrentProcess().WorkingSet64,
                    MemoryAvailableBytes = GC.GetTotalMemory(false),
                    ActiveRequests = _requestCounter,
                    ExceptionCount = _exceptionCounter,
                    DatabaseQueryCount = 0, // Будет обновляться из DbContext
                    DatabaseQueryTimeMs = 0 // Будет обновляться из DbContext
                };

                lock (_lock)
                {
                    _metricsHistory.Add(metrics);

                    // Храним только последние 100 записей
                    if (_metricsHistory.Count > 100)
                    {
                        _metricsHistory.RemoveAt(0);
                    }
                }

                CheckThresholds(metrics);

                // Сбрасываем счетчики каждую минуту
                if ((DateTime.UtcNow - _lastResetTime).TotalMinutes >= 1)
                {
                    Interlocked.Exchange(ref _requestCounter, 0);
                    Interlocked.Exchange(ref _exceptionCounter, 0);
                    _lastResetTime = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сборе метрик производительности");
            }
        }

        private double GetCpuUsage()
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                var startTime = DateTime.UtcNow;
                var startCpuUsage = process.TotalProcessorTime;

                Thread.Sleep(500);

                var endTime = DateTime.UtcNow;
                var endCpuUsage = process.TotalProcessorTime;

                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                return Math.Round(cpuUsageTotal * 100, 2);
            }
            catch
            {
                return 0;
            }
        }

        public void IncrementRequestCounter() => Interlocked.Increment(ref _requestCounter);
        public void IncrementExceptionCounter() => Interlocked.Increment(ref _exceptionCounter);
        public void AddDatabaseMetric(int queryCount, double queryTimeMs)
        {
            // Можно добавить логику для учета метрик БД
        }

        private void CheckThresholds(PerformanceMetrics metrics)
        {
            // Проверка CPU
            if (metrics.CpuUsagePercent > _config.CpuThresholdPercent)
            {
                _notificationService.SendAlert(new Alert
                {
                    Title = "Высокая загрузка CPU",
                    Message = $"Загрузка CPU: {metrics.CpuUsagePercent}% (порог: {_config.CpuThresholdPercent}%)",
                    Level = AlertLevel.Warning,
                    Category = "Performance"
                });
            }

            // Проверка памяти
            if (metrics.MemoryUsageBytes > _config.MemoryThresholdBytes)
            {
                var memoryMb = metrics.MemoryUsageBytes / (1024 * 1024);
                var thresholdMb = _config.MemoryThresholdBytes / (1024 * 1024);

                _notificationService.SendAlert(new Alert
                {
                    Title = "Высокое использование памяти",
                    Message = $"Используется памяти: {memoryMb}MB (порог: {thresholdMb}MB)",
                    Level = AlertLevel.Warning,
                    Category = "Memory"
                });
            }

            // Проверка исключений
            if (metrics.ExceptionCount > _config.ExceptionThresholdPerMinute)
            {
                _notificationService.SendAlert(new Alert
                {
                    Title = "Много исключений",
                    Message = $"Исключений в минуту: {metrics.ExceptionCount} (порог: {_config.ExceptionThresholdPerMinute})",
                    Level = AlertLevel.Error,
                    Category = "Exceptions"
                });
            }
        }

        public List<PerformanceMetrics> GetMetricsHistory()
        {
            lock (_lock)
            {
                return new List<PerformanceMetrics>(_metricsHistory);
            }
        }

        public PerformanceMetrics GetCurrentMetrics()
        {
            return _metricsHistory.LastOrDefault() ?? new PerformanceMetrics();
        }

        public void Dispose() => _timer?.Dispose();
    }
}
