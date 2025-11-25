using System.Data;
using MySql.Data.MySqlClient;
using ReportGenerator.Database.DbModels;

namespace ReportGenerator.Database.DbControllers
{
    // Перечисляемый тип для периодов
    public enum ReportPeriod
    {
        OneDay = 1,     // За 1 день
        Month = 30,     // Месяц
        ThreeMonths = 90, // Три месяца
        AllTime = 0     // Весь период
    }

    public class DbIncidentController
    {
        private string _connectionString;

        public string ConnectionString
        {
            get => _connectionString;
            set => _connectionString = value;
        }

        public DbIncidentController(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void SetConnectionString(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Основной асинхронный метод для получения инцидентов
        public async Task<List<DbIncidentData>> GetIncidentsByPeriodAsync(ReportPeriod period)
        {
            var incidents = new List<DbIncidentData>();

            await using (var connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var query = BuildQuery(period);
                    await using (var command = new MySqlCommand(query, connection))
                    {
                        // Добавляем параметры если они нужны
                        if (period == ReportPeriod.ThreeMonths)
                        {
                            command.Parameters.AddWithValue("@startDate", DateTime.Now.AddMonths(-3).ToString("yyyy-MM-dd"));
                            command.Parameters.AddWithValue("@endDate", DateTime.Now.ToString("yyyy-MM-dd"));
                        }

                        await using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var incident = MapReaderToIncidentData(reader);
                                incidents.Add(incident);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Логирование ошибки
                    Console.WriteLine($"Ошибка при получении данных: {ex.Message}");
                    throw;
                }
            }

            return incidents;
        }

        // Асинхронный метод для получения данных по диапазону дат
        public async Task<List<DbIncidentData>> GetIncidentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var incidents = new List<DbIncidentData>();

            await using (var connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var query = @"
                    SELECT 
                        id,
                        incident_number,
                        registration_time,
                        service,
                        short_description,
                        applicant,
                        priority,
                        executor,
                        decision_time,
                        status
                    FROM report_inscents 
                    WHERE DATE(registration_time) BETWEEN @startDate AND @endDate";

                    await using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd"));

                        await using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var incident = MapReaderToIncidentData(reader);
                                incidents.Add(incident);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при получении данных: {ex.Message}");
                    throw;
                }
            }

            return incidents;
        }

        // Метод для получения инцидентов с строковым параметром (для обратной совместимости)
        public async Task<List<DbIncidentData>> GetIncidentsByPeriodAsync(string period)
        {
            if (Enum.TryParse(period, true, out ReportPeriod reportPeriod))
            {
                return await GetIncidentsByPeriodAsync(reportPeriod);
            }

            // Попытка преобразования по числовому значению
            if (int.TryParse(period, out int periodValue))
            {
                reportPeriod = periodValue switch
                {
                    1 => ReportPeriod.OneDay,
                    30 => ReportPeriod.Month,
                    90 => ReportPeriod.ThreeMonths,
                    0 => ReportPeriod.AllTime,
                    _ => throw new ArgumentException("Неизвестный период выборки")
                };
                return await GetIncidentsByPeriodAsync(reportPeriod);
            }

            throw new ArgumentException("Неизвестный период выборки");
        }

        private string BuildQuery(ReportPeriod period)
        {
            var baseQuery = @"
            SELECT 
                id,
                incident_number,
                registration_time,
                service,
                short_description,
                applicant,
                priority,
                executor,
                decision_time,
                status
            FROM report_inscents";

            return period switch
            {
                ReportPeriod.OneDay => $"{baseQuery} WHERE DATE(registration_time) = CURDATE()",
                ReportPeriod.Month => $"{baseQuery} WHERE MONTH(registration_time) = MONTH(CURDATE()) AND YEAR(registration_time) = YEAR(CURDATE())",
                ReportPeriod.ThreeMonths => $"{baseQuery} WHERE DATE(registration_time) BETWEEN @startDate AND @endDate",
                ReportPeriod.AllTime => baseQuery,
                _ => throw new ArgumentException("Неизвестный период выборки")
            };
        }

        private DbIncidentData MapReaderToIncidentData(IDataReader reader)
        {
            return new DbIncidentData
            {
                Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                NumerIncident = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                RegistrationTime = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Service = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                ShortDescription = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Applicant = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Priority = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                Executor = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                DecisionTime = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                Status = reader.IsDBNull(9) ? 0 : reader.GetInt32(9)
            };
        }

        // Дополнительные вспомогательные методы

        // Получение статистики по периодам (пример использования async/await)
        public async Task<Dictionary<ReportPeriod, int>> GetIncidentsCountByPeriodsAsync()
        {
            var results = new Dictionary<ReportPeriod, int>();
            var periods = new[] { ReportPeriod.OneDay, ReportPeriod.Month, ReportPeriod.ThreeMonths, ReportPeriod.AllTime };

            // Запускаем все задачи параллельно
            var tasks = new List<Task<(ReportPeriod Period, int Count)>>();

            foreach (var period in periods)
            {
                tasks.Add(GetIncidentCountForPeriodAsync(period));
            }

            // Ожидаем завершения всех задач
            var resultsArray = await Task.WhenAll(tasks);

            foreach (var result in resultsArray)
            {
                results[result.Period] = result.Count;
            }

            return results;
        }

        private async Task<(ReportPeriod Period, int Count)> GetIncidentCountForPeriodAsync(ReportPeriod period)
        {
            await using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = BuildQuery(period).Replace("SELECT *", "SELECT COUNT(*)");

                await using (var command = new MySqlCommand(query, connection))
                {
                    if (period == ReportPeriod.ThreeMonths)
                    {
                        command.Parameters.AddWithValue("@startDate", DateTime.Now.AddMonths(-3).ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("@endDate", DateTime.Now.ToString("yyyy-MM-dd"));
                    }

                    var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return (period, count);
                }
            }
        }
    }
}
