using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReportGenerator.Models;

namespace ReportGenerator.Services
{
    public sealed class FileWatcherService : BackgroundService
    {
        private static readonly Regex LineRx = new(
            @"^(?<ts>\d{2}\.\d{2}\.\d{4}\s+\d{2}:\d{2}:\d{2},\d{1,3})\s+SOURCE=(?<src>\S+)\s+EVENTID=(?<id>\d+)\s+MESSAGE=(?<msg>.*)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly ILogger<FileWatcherService> _logger;
        private readonly GlpiIncidentService _incidentService;
        private readonly string _logFilePath;

        private long _position;

        public FileWatcherService(
            ILogger<FileWatcherService> logger,
            GlpiIncidentService incidentService,
            IConfiguration cfg)
        {
            _logger = logger;
            _incidentService = incidentService;
            _logFilePath = cfg.GetValue<string>("LogWatcher:LogFilePath") ?? "/app/Scripts/events.log";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var dir = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            _logger.LogInformation("FileWatcherService started. LogFilePath={LogFilePath}", _logFilePath);

            if (File.Exists(_logFilePath))
            {
                var initialSize = new FileInfo(_logFilePath).Length;
                _position = initialSize;
                _logger.LogInformation("Initial file size={Size}, start position set to end.", initialSize);
            }
            else
            {
                _position = 0;
                _logger.LogWarning("Log file does not exist yet: {LogFilePath}", _logFilePath);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!File.Exists(_logFilePath))
                    {
                        await Task.Delay(500, stoppingToken);
                        continue;
                    }

                    var fi = new FileInfo(_logFilePath);

                    if (fi.Length < _position)
                    {
                        _logger.LogWarning("Log file was truncated. OldPos={OldPos}, NewSize={NewSize}. Reset position to 0.",
                            _position, fi.Length);
                        _position = 0;
                    }

                    if (fi.Length > _position)
                    {
                        await ReadNewPartAndProcessAsync(fi.Length, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "FileWatcherService loop error");
                }

                await Task.Delay(500, stoppingToken);
            }

            _logger.LogInformation("FileWatcherService stopped.");
        }

        private async Task ReadNewPartAndProcessAsync(long endPos, CancellationToken ct)
        {
            string chunk;

            using (var fs = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(_position, SeekOrigin.Begin);

                using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                chunk = await sr.ReadToEndAsync();

                // фиксируем позицию по размеру файла на момент проверки
                _position = endPos;
            }

            if (string.IsNullOrWhiteSpace(chunk))
                return;

            var lines = chunk
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            _logger.LogInformation("Detected {Count} new line(s) in log.", lines.Length);

            foreach (var line in lines)
            {
                if (!TryParse(line, out var evt))
                {
                    _logger.LogWarning("Unparsed log line: {Line}", line);
                    continue;
                }

                _logger.LogInformation("Parsed event: Time={Time}, Source={Source}, EventId={EventId}, Message={Message}",
                    evt.Timestamp, evt.Source, evt.EventId, evt.Message);

                await _incidentService.CreateFromLogEventAsync(evt, ct);
            }
        }

        private static bool TryParse(string line, out LogEventIncident evt)
        {
            evt = new LogEventIncident { RawLine = line };

            var m = LineRx.Match(line);
            if (!m.Success) return false;

            var tsText = m.Groups["ts"].Value;
            var src = m.Groups["src"].Value;
            var idText = m.Groups["id"].Value;
            var msg = m.Groups["msg"].Value;

            if (!int.TryParse(idText, out var id))
                return false;

            var ru = CultureInfo.GetCultureInfo("ru-RU");
            var formats = new[]
            {
                "dd.MM.yyyy HH:mm:ss,fff",
                "dd.MM.yyyy HH:mm:ss,ff",
                "dd.MM.yyyy HH:mm:ss,f"
            };

            if (!DateTime.TryParseExact(tsText, formats, ru, DateTimeStyles.None, out var dt))
                return false;

            evt = new LogEventIncident
            {
                Timestamp = dt,
                Source = src,
                EventId = id,
                Message = msg,
                RawLine = line
            };

            return true;
        }
    }
}
