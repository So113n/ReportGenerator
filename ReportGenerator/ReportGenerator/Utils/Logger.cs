using System.Text;

namespace Utils
{
    public enum LogLevel
    {
        Info,       // Информация
        Warning,    // Внимание
        Error       // Ошибка
    }


    public sealed class Logger
    {
        #region === Singleton Implementation ===

        private static readonly Lazy<Logger> _instance = new Lazy<Logger>(() => new Logger());
        public static Logger Instance => _instance.Value;

        private Logger()
        {
            // Настройки по умолчанию
            _outputPorts = new HashSet<OutputPort> { OutputPort.File };
            _logFilePath = GetDefaultLogPath();
            _minimumLogLevel = LogLevel.Info;
        }

        #endregion

        #region === Fields and properties ===

        private readonly object _lockObject = new object();
        private string _logFilePath;
        private LogLevel _minimumLogLevel;
        private readonly HashSet<OutputPort> _outputPorts;

        public string LogFilePath { get => _logFilePath; set => _logFilePath = value; }
        public event Action<string> OnLogMessage;

        // Порты вывода
        [Flags]
        public enum OutputPort
        {
            None = 0,
            Console = 1,
            File = 2,
            Event = 4
        }

        #endregion

        #region === Public methods ===

        /// <summary>
        /// Настройка параметров логирования
        /// </summary>
        public void Configure(string logFilePath = null,
                             LogLevel minimumLogLevel = LogLevel.Info,
                             OutputPort outputPorts = OutputPort.File)
        {
            lock (_lockObject)
            {
                if (!string.IsNullOrEmpty(logFilePath))
                    _logFilePath = logFilePath;

                _minimumLogLevel = minimumLogLevel;

                _outputPorts.Clear();
                foreach (OutputPort port in Enum.GetValues(typeof(OutputPort)))
                {
                    if (port != OutputPort.None && outputPorts.HasFlag(port))
                        _outputPorts.Add(port);
                }
            }
        }

        /// <summary>
        /// Логирование сообщения
        /// </summary>
        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (level < _minimumLogLevel)
                return;

            var logMessage = FormatMessage(message, level);

            lock (_lockObject)
            {
                if (_outputPorts.Contains(OutputPort.Console))
                    WriteToConsole(logMessage, level);

                if (_outputPorts.Contains(OutputPort.File))
                    WriteToFile(logMessage);

                if (_outputPorts.Contains(OutputPort.Event))
                    RaiseLogEvent(logMessage);
            }
        }

        /// <summary>
        /// Логирование информационного сообщения
        /// </summary>
        public void Info(string message) => Log(message, LogLevel.Info);

        /// <summary>
        /// Логирование предупреждения
        /// </summary>
        public void Warning(string message) => Log(message, LogLevel.Warning);

        /// <summary>
        /// Логирование ошибки
        /// </summary>
        public void Error(string message) => Log(message, LogLevel.Error);

        /// <summary>
        /// Получение текущего пути к файлу лога
        /// </summary>
        public string GetCurrentLogPath() => _logFilePath;

        /// <summary>
        /// Задать новый путь к файлу лога
        /// </summary>
        /// <param name="new_path"></param>
        public void SetCurrentLogPath(string new_path) => _logFilePath = new_path;

        #endregion

        #region === Private methods ===

        private string GetDefaultLogPath()
        {
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(assemblyDirectory ?? Directory.GetCurrentDirectory(), "log.txt");
        }

        private string FormatMessage(string message, LogLevel level)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var levelString = GetLevelString(level);
            return $"[{timestamp}] [{levelString}] {message}";
        }

        private string GetLevelString(LogLevel level)
        {
            return level switch
            {
                LogLevel.Info => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "ERROR",
                _ => "UNKNOWN"
            };
        }

        private void WriteToConsole(string message, LogLevel level)
        {
            var originalColor = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = level switch
                {
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Error => ConsoleColor.Red,
                    _ => ConsoleColor.White
                };

                Console.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }

        private void WriteToFile(string message)
        {
            try
            {
                // Создаем директорию, если она не существует
                var directory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.AppendAllText(_logFilePath, message + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // Если не удалось записать в файл, выводим ошибку в консоль
                Console.WriteLine($"Ошибка записи в лог-файл: {ex.Message}");
            }
        }

        private void RaiseLogEvent(string message)
        {
            OnLogMessage?.Invoke(message);
        }

        #endregion
    }

    #region === Пример использования ===

    /*
     class Program
    {
        static void Main()
        {
            // Настройка логгера
            Logger.Instance.Configure(
                logFilePath: @"C:\Logs\myapp\log.txt", // опционально
                minimumLogLevel: LogLevel.Info,
                outputPorts: Logger.OutputPort.Console | Logger.OutputPort.File | Logger.OutputPort.Event
            );
        
            // Подписка на событие логирования
            Logger.Instance.OnLogMessage += message => 
            {
                // Дополнительная обработка сообщений
                Console.WriteLine($"Событие: {message}");
            };
        
            // Примеры логирования
            Logger.Instance.Info("Приложение запущено");
            Logger.Instance.Warning("Нехватка памяти");
            Logger.Instance.Error("Критическая ошибка в модуле X");
        
            // Или через прямой вызов Log
            Logger.Instance.Log("Тестовое сообщение", LogLevel.Info);
        }
    }
    */
    #endregion
}
