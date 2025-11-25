using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ReportGenerator.Database.DbControllers;
using ReportGenerator.Models;
using ReportGenerator.Services;
using ReportGenerator.Services.Crypt;
using ReportGenerator.Utils;
using Utils;

namespace ReportGenerator.Controllers
{
    public class HomeController : Controller
    {
        private string _connectionString = "Server=host.docker.internal;Database=glpi;Uid=docker_user;Pwd=root;";
        private string _pathConnStrConfFile = "";

        private readonly DbIncidentController _dbIncidentController;
        private readonly ReportGeneratorService _reportGeneratorService;
        private readonly SimpleEncryptionService _simpleEncryptionService;

        private static Vector<string> _vector = new Vector<string>();
        private static Utils.List<string> _list = new Utils.List<string>();

        public HomeController()
        {
            _dbIncidentController = new(_connectionString);
            _reportGeneratorService = new();

            Startup();
        }

        #region === Open Windows ===

        public IActionResult Index()
        {
            Logger.Instance.Info($"Открытие страницы - Index");
            return View();
        }

        public IActionResult LogInfo()
        {
            Logger.Instance.Info($"Открытие страницы - LogInfo");

            var model = new LogModel();

            try
            {
                // Получаем путь к файлу лога (замените на ваш реальный путь)
                string logFilePath;
                logFilePath = Logger.Instance.LogFilePath;

                model.LogFilePath = logFilePath;

                if (System.IO.File.Exists(logFilePath))
                {
                    // Читаем все строки из файла
                    var logEntries = System.IO.File.ReadAllLines(logFilePath);
                    model.LogEntries = logEntries.Reverse().ToList(); // Последние записи сначала
                }
                else
                {
                    model.ErrorMessage = $"Файл лога не найден: {logFilePath}";
                }
            }
            catch (Exception ex)
            {
                model.ErrorMessage = $"Ошибка при чтении лога: {ex.Message}";
                Logger.Instance.Error($"Ошибка при чтении файла лога: {ex.Message}");
            }

            return View(model);
        }

        public IActionResult VectorView()
        {
            Logger.Instance.Info($"Открытие страницы - VectorView");
            return View(_vector);
        }

        public IActionResult ListView()
        {
            Logger.Instance.Info($"Открытие страницы - ListView");
            return View(_list);
        }

        public IActionResult Monitoring()
        {
            Logger.Instance.Info($"Открытие страницы - Monitoring");
            return View(_list);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            Logger.Instance.Info($"Открытие страницы - Error");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        #endregion

        #region === Report Generator ===

        [HttpPost]
        public async Task<IActionResult> GenerateReport([FromBody] ReportRequestModel request)
        {
            try
            {
                Logger.Instance.Info($"Запрос на генерацию отчета: Period={request.Period}, Format={request.Format}");

                // Получаем данные из базы
                var incidents = await _dbIncidentController.GetIncidentsByPeriodAsync(request.Period);

                if (incidents == null || incidents.Count == 0)
                {
                    return Json(new { success = false, message = "Нет данных для выбранного периода" });
                }

                // Генерируем отчет
                byte[] reportData;
                string contentType;
                string fileName;

                if (request.Format.ToLower() == "excel")
                {
                    reportData = await _reportGeneratorService.GenerateExcelReportAsync(incidents);
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    fileName = $"incidents_report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                }
                else if (request.Format.ToLower() == "pdf")
                {
                    reportData = await _reportGeneratorService.GeneratePdfReportAsync(incidents);
                    contentType = "application/pdf";
                    fileName = $"incidents_report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                }
                else
                {
                    return Json(new { success = false, message = "Неизвестный формат отчета" });
                }

                Logger.Instance.Info($"Отчет сгенерирован успешно: {incidents.Count} записей, формат: {request.Format}");

                // Возвращаем файл для скачивания
                return File(reportData, contentType, fileName);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Ошибка при генерации отчета: {ex.Message}");
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }
        #endregion

        #region === Modal Settings window ===

        [HttpGet]
        public IActionResult GetConnectionString()
        {
            return Ok(new { connectionString = _connectionString });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateConnectionString([FromBody] ConnectionStringModel model)
        {
            if (string.IsNullOrEmpty(model.ConnectionString))
            {
                return BadRequest(new { success = false, message = "Строка подключения не может быть пустой" });
            }

            _connectionString = model.ConnectionString;

            // Пересоздаем контроллер с новой строкой подключения
            //_dbIncidentController = new(_connectionString);
            _dbIncidentController.ConnectionString = _connectionString;

            await SimpleEncryptionService.Instance.WriteEncryptedFileAsync(_pathConnStrConfFile, _connectionString, "pass_");

            Logger.Instance.Info($"Строка подключения обновлена, значение: {_connectionString}");
            return Ok(new { success = true, message = $"Строка подключения успешно обновлена " });
        }

        [HttpPost]
        public async Task<IActionResult> TestConnection([FromBody] ConnectionStringModel model)
        {
            if (string.IsNullOrEmpty(model.ConnectionString))
            {
                return Json(new { success = false, message = "Строка подключения не может быть пустой" });
            }

            try
            {
                using (var connection = new MySqlConnection(model.ConnectionString))
                {
                    await connection.OpenAsync();

                    // Проверяем, что база данных доступна
                    using (var command = new MySqlCommand("SELECT 1", connection))
                    {
                        await command.ExecuteScalarAsync();
                    }

                    return Json(new { success = true, message = "Подключение к базе данных успешно установлено" });
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Ошибка тестирования подключения: {ex.Message}");
                return Json(new { success = false, message = $"Ошибка подключения: {ex.Message}" });
            }
        }
        #endregion

        #region === Diagnostics ===

        [HttpGet("diagnostics")]
        public async Task<IActionResult> Diagnostics()
        {
            var sb = new StringBuilder();

            // Сетевая диагностика
            sb.AppendLine("=== СЕТЕВАЯ ДИАГНОСТИКА ===");
            try
            {
                var hostEntry = System.Net.Dns.GetHostEntry("host.docker.internal");
                sb.AppendLine($"host.docker.internal resolved to: {string.Join(", ", hostEntry.AddressList.Select(a => a.ToString()))}");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"DNS resolution failed: {ex.Message}");
            }

            // Тест подключения
            sb.AppendLine("\n=== ТЕСТ ПОДКЛЮЧЕНИЯ MYSQL ===");
            var hosts = new[] { "host.docker.internal", "172.17.0.1", "localhost" };

            foreach (var host in hosts)
            {
                try
                {
                    using var connection = new MySqlConnection($"Server={host};Port=3306;Database=461_db;Uid=root;Pwd=;ConnectionTimeout=3;");
                    await connection.OpenAsync();
                    sb.AppendLine($"{host}: OK");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"{host}: {ex.Message}");
                }
            }

            return Content(sb.ToString(), "text/plain");
        }
        #endregion

        #region === Viewer Log ===

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearLog()
        {
            try
            {
                string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "log.txt");

                if (System.IO.File.Exists(logFilePath))
                {
                    System.IO.File.WriteAllText(logFilePath, string.Empty);
                    Logger.Instance.Info("Лог-файл был очищен");
                    return Json(new { success = true, message = "Лог-файл успешно очищен" });
                }

                return Json(new { success = false, message = "Файл лога не найден" });
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Ошибка при очистке лога: {ex.Message}");
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult DownloadLog()
        {
            try
            {
                string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "log.txt");

                if (System.IO.File.Exists(logFilePath))
                {
                    var fileBytes = System.IO.File.ReadAllBytes(logFilePath);
                    return File(fileBytes, "text/plain", $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                }

                return NotFound("Файл лога не найден");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Ошибка при скачивании лога: {ex.Message}");
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
        }

        #endregion

        #region === Vector ===

        [HttpPost]
        public IActionResult AddToVector(string item)
        {
            if (!string.IsNullOrEmpty(item))
            {
                _vector.Add(item);
                Logger.Instance.Info($"Выполнено действие - add to vector");
            }
            return RedirectToAction("VectorView");
        }

        [HttpPost]
        public IActionResult RemoveFromVector(int index)
        {
            if (index >= 0 && index < _vector.Count)
            {
                _vector.RemoveAt(index);
                Logger.Instance.Info($"Выполнено действие - remove from vector");
            }
            return RedirectToAction("VectorView");
        }

        [HttpPost]
        public IActionResult ClearVector()
        {
            _vector.Clear();
            Logger.Instance.Info($"Выполнено действие - сlear vector");

            return RedirectToAction("VectorView");
        }
        #endregion

        #region === List ===

        [HttpPost]
        public IActionResult AddToList(string item)
        {
            if (!string.IsNullOrEmpty(item))
            {
                _list.Add(item);
                Logger.Instance.Info($"Выполнено действие - add to list");
            }
            return RedirectToAction("ListView");
        }

        [HttpPost]
        public IActionResult RemoveFromList(string item)
        {
            if (!string.IsNullOrEmpty(item))
            {
                _list.Remove(item);
                Logger.Instance.Info($"Выполнено действие - remove from list");
            }
            return RedirectToAction("ListView");
        }

        [HttpPost]
        public IActionResult ClearList()
        {
            _list.Clear();
            Logger.Instance.Info($"Выполнено действие - сlear list");

            return RedirectToAction("ListView");
        }
        #endregion

        #region === Private Methods ===

        private void Startup()
        {
            _pathConnStrConfFile = GetDefaultConnStrConfPath();

            if (SimpleEncryptionService.Instance.IsFileExists(_pathConnStrConfFile))
                _connectionString = SimpleEncryptionService.Instance.ReadEncryptedFileAsync(_pathConnStrConfFile, "pass_").Result;

        }

        private string GetDefaultConnStrConfPath()
        {
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(assemblyDirectory ?? Directory.GetCurrentDirectory(), "ConnStrConf.txt");
        }
        #endregion
    }
}