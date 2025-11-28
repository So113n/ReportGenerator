using System;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;


namespace ReportGenerator.Services
{
    public class RuntimeMetricsListener : IDisposable
    {
        private readonly MeterListener _listener;
        // Потокобезопасное хранилище последних значений метрик
        private readonly ConcurrentDictionary<string, double> _latestValues = new();

        public RuntimeMetricsListener()
        {
            _listener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    // Подписываемся на метрики runtime
                    if (instrument.Meter.Name == "System.Runtime")
                    {
                        listener.EnableMeasurementEvents(instrument, null);
                    }
                }
            };

            // Подписка для разных типов измерений
            _listener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
            _listener.SetMeasurementEventCallback<double>(OnMeasurementRecorded);

            _listener.Start();
        }

        private void OnMeasurementRecorded<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
            where T : struct
        {
            // ключ — имя инструмента (например, "dotnet.process.cpu.time", "dotnet.process.memory.working_set")
            // measurement — значение
            if (measurement is IConvertible convertible)
            {
                double value = convertible.ToDouble(null);
                _latestValues[instrument.Name] = value;
            }
        }

        public bool TryGetMetric(string name, out double value)
        {
            return _latestValues.TryGetValue(name, out value);
        }

        public void Dispose()
        {
            _listener.Dispose();
        }
    }
}