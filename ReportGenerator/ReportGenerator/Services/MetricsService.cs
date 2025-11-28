using System.Diagnostics;
using ReportGenerator.Models;

namespace ReportGenerator.Services
{
    public class MetricsService
    {
        private readonly object _lock = new();

        // Эти счётчики ты потом можешь дёргать из middleware/контроллеров
        private int _activeRequests;
        private int _exceptionCount;
        private int _databaseQueryCount;
        private double _databaseQueryTimeMs;
        private double _lastRequestProcessingTimeMs;

        public PerformanceMetrics GetCurrentMetrics()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var process = Process.GetCurrentProcess();

                var cpuPercent = GetCpuUsagePercent(process);
                var workingSet = process.WorkingSet64;
                var gcMemory = GC.GetTotalMemory(false);

                return new PerformanceMetrics
                {
                    Timestamp = now,
                    CpuUsagePercent = cpuPercent,
                    MemoryUsageBytes = workingSet,
                    MemoryAvailableBytes = gcMemory,
                    ActiveRequests = _activeRequests,
                    RequestProcessingTimeMs = _lastRequestProcessingTimeMs,
                    ExceptionCount = _exceptionCount,
                    DatabaseQueryCount = _databaseQueryCount,
                    DatabaseQueryTimeMs = _databaseQueryTimeMs
                };
            }
        }

        /// <summary>
        /// Пример простого расчёта средней загрузки CPU за короткий интервал.
        /// Блокирует поток примерно на 500 мс, но для страницы мониторинга это ок.
        /// </summary>
        private double GetCpuUsagePercent(Process process)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var startCpu = process.TotalProcessorTime;

                Thread.Sleep(500); // короткая выборка

                var endTime = DateTime.UtcNow;
                var endCpu = process.TotalProcessorTime;

                var cpuUsedMs = (endCpu - startCpu).TotalMilliseconds;
                var totalMs = (endTime - startTime).TotalMilliseconds * Environment.ProcessorCount;

                if (totalMs <= 0)
                    return 0;

                var percent = cpuUsedMs / totalMs * 100.0;
                return Math.Round(percent, 2);
            }
            catch
            {
                return 0;
            }
        }

        // --- Дополнительные методы для инкрементов (можешь подключить потом через middleware) ---

        public void IncrementActiveRequests() => Interlocked.Increment(ref _activeRequests);
        public void DecrementActiveRequests() => Interlocked.Decrement(ref _activeRequests);

        public void RegisterRequestTime(double ms)
        {
            lock (_lock)
            {
                _lastRequestProcessingTimeMs = ms;
            }
        }

        public void IncrementExceptionCount() => Interlocked.Increment(ref _exceptionCount);

        public void AddDatabaseMetrics(int queries, double totalTimeMs)
        {
            lock (_lock)
            {
                _databaseQueryCount += queries;
                _databaseQueryTimeMs += totalTimeMs;
            }
        }
    }
}

