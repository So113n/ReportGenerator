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
                // Открываем шаблон Excel
                using (var workbook = new XLWorkbook(TemplatePath))
                {
                    var worksheet = workbook.Worksheet(1);  // Открываем первый лист

                    int total = incidents.Count;
                    int closed = incidents.Count(i => !string.IsNullOrEmpty(i.Status));
                    int inProgress = total - closed;

                    // Устанавливаем значения шапки и других текстов
                    worksheet.Cell("A1").Value = "Отчет об инцидентах за " + DateTime.Now.ToString("dd/MM/yyyy");
                    worksheet.Cell("A3").Value = $"Дата и время формирования: {DateTime.Now:dd.MM.yyyy HH:mm:ss}; Отдел ОПП г. Новый Уренгой";
                    worksheet.Cell("A5").Value = $"Всего инцидентов: «{total}», Закрыто: «{closed}», На исполнении: «{inProgress}»";

                    var headers = new[]
                    {
                        "Номер инцидента", "Время регистрации", "Услуга", "Краткое описание",
                        "Заявитель", "Приоритет", "Исполнитель", "Время решения", "Статус"
                    };

                    // Установка значений заголовков
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(7, i + 1).Value = headers[i]; // Заголовки начинаются с 7 строки
                        worksheet.Cell(7, i + 1).Style.Font.Bold = true;
                        worksheet.Cell(7, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    // Заполняем данные в таблице начиная с 8 строки
                    int currentRow = 8;
                 
                    foreach (var incident in incidents)
                    {
                        // Проверка, если строк больше чем в шаблоне - вставляем новые строки
                        if (currentRow > 15)
                        {
                            worksheet.Row(15).InsertRowsBelow(1); // Вставляем новую строку
                            worksheet.Row(currentRow).Style = worksheet.Row(15).Style;  // Копируем стиль
                        }

                        // Заполняем данные
                        worksheet.Cell(currentRow, 1).Value = incident.NumerIncident;
                        worksheet.Cell(currentRow, 2).Value = incident.RegistrationTime;
                        worksheet.Cell(currentRow, 3).Value = incident.Service;
                        worksheet.Cell(currentRow, 4).Value = incident.ShortDescription;
                        worksheet.Cell(currentRow, 5).Value = incident.Applicant;
                        worksheet.Cell(currentRow, 6).Value = incident.Priority;
                        worksheet.Cell(currentRow, 7).Value = incident.Executor;
                        worksheet.Cell(currentRow, 8).Value = incident.DecisionTime;
                        worksheet.Cell(currentRow, 9).Value = incident.Status;
                        worksheet.Cell(currentRow, 9).Value = incident.Status ?? "—";

                        // Центрируем данные в ячейках
                        for (int col = 1; col <= 9; col++)
                        {
                            worksheet.Cell(currentRow, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }

                        currentRow++;  // Переходим к следующей строке
                    }

                    // Автоподбор ширины столбцов
                    worksheet.Columns(1, 9).AdjustToContents();

                    worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Сохраняем в MemoryStream
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
                using (var memoryStream = new MemoryStream())
                {
                    var document = new Document(PageSize.A4.Rotate(), 15f, 15f, 15f, 15f);
                    var writer = PdfWriter.GetInstance(document, memoryStream);

                    var baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1250, BaseFont.EMBEDDED);
                    var titleFont = new Font(baseFont, 16, Font.BOLD, BaseColor.BLACK);
                    var headerFont = new Font(baseFont, 9, Font.BOLD, BaseColor.WHITE);
                    var dataFont = new Font(baseFont, 8, Font.NORMAL, BaseColor.BLACK);
                    var footerFont = new Font(baseFont, 9, Font.ITALIC, BaseColor.GRAY);

                    document.Open();

                    // Заголовок
                    var title = new Paragraph("ОТЧЕТ ПО ИНЦИДЕНТАМ", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 25f
                    };
                    document.Add(title);

                    var table = new PdfPTable(9)
                    {
                        WidthPercentage = 100,
                        SpacingBefore = 15f,
                        SpacingAfter = 20f,
                        HeaderRows = 1,
                        KeepTogether = true
                    };

                    table.SetWidths(new float[] { 1f, 1.2f, 1.2f, 1.8f, 1.2f, 0.7f, 1.2f, 1.2f, 0.7f });

                    // Добавляем заголовки
                    AddHeaderCell(table, "Номер инцидента", headerFont);
                    AddHeaderCell(table, "Время регистрации", headerFont);
                    AddHeaderCell(table, "Услуга", headerFont);
                    AddHeaderCell(table, "Краткое описание", headerFont);
                    AddHeaderCell(table, "Заявитель", headerFont);
                    AddHeaderCell(table, "Приоритет", headerFont);
                    AddHeaderCell(table, "Исполнитель", headerFont);
                    AddHeaderCell(table, "Время решения", headerFont);
                    AddHeaderCell(table, "Статус", headerFont);

                    foreach (var incident in incidents)
                    {
                        AddDataCell(table, incident.NumerIncident.ToString(), dataFont, Element.ALIGN_CENTER);
                        AddDataCell(table, incident.RegistrationTime, dataFont, Element.ALIGN_LEFT);
                        AddDataCell(table, incident.Service, dataFont, Element.ALIGN_LEFT);
                        AddDataCell(table, incident.ShortDescription, dataFont, Element.ALIGN_LEFT);
                        AddDataCell(table, incident.Applicant, dataFont, Element.ALIGN_LEFT);
                        AddDataCell(table, incident.Priority.ToString(), dataFont, Element.ALIGN_CENTER);
                        AddDataCell(table, incident.Executor, dataFont, Element.ALIGN_LEFT);
                        AddDataCell(table, incident.DecisionTime, dataFont, Element.ALIGN_LEFT);
                        AddDataCell(table, incident.Status.ToString(), dataFont, Element.ALIGN_CENTER);
                    }

                    document.Add(table);

                    var footer = new Paragraph()
                    {
                        new Chunk($"Всего записей: {incidents.Count} | ", footerFont),
                        new Chunk($"Сгенерирован: {DateTime.Now:dd.MM.yyyy HH:mm}", footerFont)
                    };
                    footer.Alignment = Element.ALIGN_RIGHT;
                    footer.SpacingBefore = 20f;
                    document.Add(footer);

                    document.Close();
                    return memoryStream.ToArray();
                }
            });
        }

        private void AddHeaderCell(PdfPTable table, string text, Font font)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                BackgroundColor = new BaseColor(79, 129, 189),
                Padding = 8f,
                BorderWidth = 0.5f,
                BorderColor = BaseColor.GRAY
            };
            table.AddCell(cell);
        }

        private void AddDataCell(PdfPTable table, string text, Font font, int alignment)
        {
            var value = string.IsNullOrEmpty(text) || text == "-" ? "—" : text;
            var cell = new PdfPCell(new Phrase(value, font))
            {
                HorizontalAlignment = alignment,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 6f,
                BorderWidth = 0.25f,
                BorderColor = new BaseColor(220, 220, 220)
            };

            if (table.Rows.Count % 2 == 1)
            {
                cell.BackgroundColor = new BaseColor(248, 248, 248);
            }

            table.AddCell(cell);
        }

        public async Task<byte[]> GenerateReportAsync(List<DbIncidentData> incidents, string format)
        {
            return format.ToLower() switch
            {
                "excel" => await GenerateExcelReportAsync(incidents),
                "pdf" => await GeneratePdfReportAsync(incidents),
                _ => throw new ArgumentException("Неизвестный формат отчета. Поддерживаются: excel, pdf")
            };
        }

        public async Task<byte[]> GenerateReportAsync(List<DbIncidentData> incidents, ReportFormat format)
        {
            return format switch
            {
                ReportFormat.Excel => await GenerateExcelReportAsync(incidents),
                ReportFormat.Pdf => await GeneratePdfReportAsync(incidents),
                _ => throw new ArgumentException("Неизвестный формат отчета. Поддерживаются: excel, pdf")
            };
        }
    }
}
