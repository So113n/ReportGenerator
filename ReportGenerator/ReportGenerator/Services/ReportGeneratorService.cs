using ClosedXML.Excel;
using Google.Protobuf.WellKnownTypes;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Routing.Template;
using ReportGenerator.Database.DbModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ReportGenerator.Services
{
    public enum ReportFormat
    {
        Excel,
        Pdf
    }

    public class ReportGeneratorService
    {
        private const string TemplatePath = "/app/Templates/template.xlsx";
        public async Task<byte[]> GenerateExcelReportAsync(List<DbIncidentData> incidents)
        {
            return await Task.Run(() =>
            {
                using (var workbook = new XLWorkbook(TemplatePath))
                {
                    var worksheet = workbook.Worksheet(1); // первый лист шаблона

                    // --- Подсчёт статистики по статусу ---
                    int total = incidents.Count;
                    int closed = incidents.Count(i => !string.IsNullOrWhiteSpace(i.Status));
                    int inProgress = total - closed;

                    // Выбираем часовой пояс (UTC+5, Екатеринбург)
                    var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Yekaterinburg");
                    // Берём текущее UTC-время и конвертируем в локальное
                    var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                    // --- Шапка отчёта ---
                    worksheet.Cell("A1").Value =
                        $"Отчет об инцидентах за {now:dd.MM.yyyy}";
                    worksheet.Cell("A3").Value =
                        $"Дата и время формирования: {now:dd.MM.yyyy HH:mm:ss}; Отдел ОПП г. Новый Уренгой";
                    worksheet.Cell("A5").Value =
                        $"Всего инцидентов: «{total}», Закрыто: «{closed}», На исполнении: «{inProgress}»";

                    // --- Разметка таблицы в шаблоне ---
                    const int headerRow = 7;            // строка заголовков таблицы
                    const int firstDataRow = 8;         // первая строка с данными
                    const int lastTemplateDataRow = 15; // последняя строка таблицы в шаблоне
                    const int dataColumnCount = 9;      // от "№ инцидента" до "Статус"

                    // В шаблоне уже есть несколько строк под данные
                    int templateCapacity = lastTemplateDataRow - firstDataRow + 1; // 8 строк

                    // Сколько строк реально нужно под данные
                    int rowsNeeded = Math.Max(total, 1); // хотя бы одна строка оставим

                    // --- Подгоняем количество строк под наши данные ---

                    if (rowsNeeded > templateCapacity)
                    {
                        // Случай "за весь период" или когда данных много:
                        // добавляем недостающие строки, как у тебя было.
                        int extraRows = rowsNeeded - templateCapacity;

                        var lastTemplateRow = worksheet.Row(lastTemplateDataRow);
                        lastTemplateRow.InsertRowsBelow(extraRows);
                    }
                    else if (rowsNeeded < templateCapacity)
                    {
                        // Случай "1 месяц", "3 месяца", когда данных меньше шаблонных строк:
                        // удаляем лишние нижние строки, чтобы не было пустых строк таблицы.

                        int rowsToDelete = templateCapacity - rowsNeeded;
                        int deleteFrom = firstDataRow + rowsNeeded;   // первая лишняя строка
                        int deleteTo = lastTemplateDataRow;         // последняя шаблонная строка

                        worksheet.Rows(deleteFrom, deleteTo).Delete();
                        // Блок с "Составил / Проверил" просто поднимется выше.
                    }

                    // После вставок/удалений индекс последней строки с данными:
                    int lastDataRow = firstDataRow + rowsNeeded - 1;

                    // --- Очищаем только значения в диапазоне данных (стили и границы не трогаем) ---
                    worksheet.Range(firstDataRow, 1, lastDataRow, dataColumnCount)
                             .Clear(XLClearOptions.Contents);

                    // --- Заполняем данные ---
                    for (int i = 0; i < total; i++)
                    {
                        var incident = incidents[i];
                        int row = firstDataRow + i;

                        worksheet.Cell(row, 1).Value = incident.NumerIncident;
                        worksheet.Cell(row, 2).Value = incident.RegistrationTime;
                        worksheet.Cell(row, 3).Value = incident.Service;
                        worksheet.Cell(row, 4).Value = incident.ShortDescription;
                        worksheet.Cell(row, 5).Value = incident.Applicant;
                        worksheet.Cell(row, 6).Value = incident.Priority;
                        worksheet.Cell(row, 7).Value = incident.Executor;
                        worksheet.Cell(row, 8).Value = incident.DecisionTime;
                        worksheet.Cell(row, 9).Value =
                            string.IsNullOrWhiteSpace(incident.Status) ? "—" : incident.Status;
                    }

                    // --- Центрирование и перенос текста в ячейках данных ---
                    var dataRange = worksheet.Range(firstDataRow, 1, lastDataRow, dataColumnCount);

                    dataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    dataRange.Style.Alignment.WrapText = true;

                    // Ширину колонок по-прежнему берём из шаблона (под печать они у тебя уже нормальные).

                    using (var memoryStream = new MemoryStream())
                    {
                        workbook.SaveAs(memoryStream);
                        return memoryStream.ToArray();
                    }
                }
            });
        }
        public async Task<byte[]> GeneratePdfReportAsync(List<DbIncidentData> incidents)
        {
            return await Task.Run(() =>
            {
                // Часовой пояс как в Excel-отчёте
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Yekaterinburg");
                var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                // Подготовка шрифта с поддержкой кириллицы
                // Fonts/arial.ttf у тебя копируется в output, поэтому берём из AppContext.BaseDirectory
                var fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "arial.ttf");
                BaseFont baseFont;

                if (File.Exists(fontPath))
                {
                    baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                }
                else
                {
                    // Фолбэк: системный шрифт (на Linux-контейнере обычно есть DejaVu)
                    // Если и он не подхватится — будет без кириллицы, поэтому лучше держать arial.ttf в output.
                    baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                }

                var titleFont = new Font(baseFont, 14, Font.BOLD);
                var headerFont = new Font(baseFont, 10, Font.BOLD);
                var cellFont = new Font(baseFont, 9, Font.NORMAL);

                using var ms = new MemoryStream();

                // Много колонок → альбомная ориентация
                using var document = new Document(PageSize.A4.Rotate(), 20, 20, 20, 20);
                var writer = PdfWriter.GetInstance(document, ms);
                writer.CloseStream = false;

                document.Open();

                // Заголовок
                document.Add(new Paragraph($"Отчет об инцидентах за {now:dd.MM.yyyy}", titleFont));
                document.Add(new Paragraph($"Дата и время формирования: {now:dd.MM.yyyy HH:mm:ss}; Отдел ОПП г. Новый Уренгой", cellFont));
                document.Add(new Paragraph(" ", cellFont));

                // Статистика (как в Excel)
                int total = incidents.Count;
                int closed = incidents.Count(i => !string.IsNullOrWhiteSpace(i.Status));
                int inProgress = total - closed;

                document.Add(new Paragraph($"Всего инцидентов: «{total}», Закрыто: «{closed}», На исполнении: «{inProgress}»", cellFont));
                document.Add(new Paragraph(" ", cellFont));

                // Таблица (9 колонок как в Excel)
                var table = new PdfPTable(9)
                {
                    WidthPercentage = 100
                };

                // Ширины колонок (подбираем разумно)
                table.SetWidths(new float[] { 1.2f, 2.2f, 2.0f, 3.2f, 2.2f, 1.2f, 2.2f, 2.2f, 1.4f });

                // Заголовки колонок
                AddHeaderCell(table, "№", headerFont);
                AddHeaderCell(table, "Время регистрации", headerFont);
                AddHeaderCell(table, "Сервис", headerFont);
                AddHeaderCell(table, "Краткое описание", headerFont);
                AddHeaderCell(table, "Заявитель", headerFont);
                AddHeaderCell(table, "Приоритет", headerFont);
                AddHeaderCell(table, "Исполнитель", headerFont);
                AddHeaderCell(table, "Время решения", headerFont);
                AddHeaderCell(table, "Статус", headerFont);

                // Данные
                foreach (var incident in incidents)
                {
                    AddBodyCell(table, incident.NumerIncident.ToString(), cellFont);
                    AddBodyCell(table, incident.RegistrationTime ?? "", cellFont);
                    AddBodyCell(table, incident.Service ?? "", cellFont);
                    AddBodyCell(table, incident.ShortDescription ?? "", cellFont);
                    AddBodyCell(table, incident.Applicant ?? "", cellFont);
                    AddBodyCell(table, incident.Priority ?? "", cellFont);
                    AddBodyCell(table, incident.Executor ?? "", cellFont);
                    AddBodyCell(table, incident.DecisionTime ?? "", cellFont);
                    AddBodyCell(table, string.IsNullOrWhiteSpace(incident.Status) ? "—" : incident.Status, cellFont);
                }

                document.Add(table);
                document.Close();

                return ms.ToArray();
            });
        }

        // Хелперы для ячеек (добавь тоже внутрь класса ReportGeneratorService)
        private static void AddHeaderCell(PdfPTable table, string text, Font font)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                BackgroundColor = new BaseColor(230, 230, 230),
                Padding = 5f
            };
            table.AddCell(cell);
        }

        private static void AddBodyCell(PdfPTable table, string text, Font font)
        {
            var cell = new PdfPCell(new Phrase(text ?? "", font))
            {
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 4f
            };
            table.AddCell(cell);
        }
    }
}








