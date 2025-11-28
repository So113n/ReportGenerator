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

                    // --- Шапка отчёта ---
                    worksheet.Cell("A1").Value =
                        "Отчет об инцидентах за " + DateTime.Now.ToString("dd.MM.yyyy");
                    worksheet.Cell("A3").Value =
                        $"Дата и время формирования: {DateTime.Now:dd.MM.yyyy HH:mm:ss}; Отдел ОПП г. Новый Уренгой";
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
    }
}








