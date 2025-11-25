using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using ReportGenerator.Database.DbModels;

namespace ReportGenerator.Services
{
    public enum ReportFormat
    {
        Excel,
        Pdf
    }

    public class ReportGeneratorService
    {
        public async Task<byte[]> GenerateExcelReportAsync(List<DbIncidentData> incidents)
        {
            return await Task.Run(() =>
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Инциденты");

                    // Заголовки столбцов
                    var headers = new[]
                    {
                    "Номер инцидента", "Время регистрации", "Услуга",
                    "Краткое описание", "Заявитель", "Приоритет",
                    "Исполнитель", "Время решения", "Статус"
                };

                    // Стиль для заголовков
                    var headerStyle = workbook.Style;
                    headerStyle.Font.Bold = true;
                    headerStyle.Fill.BackgroundColor = XLColor.LightGray;
                    headerStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Добавляем заголовки
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                        worksheet.Cell(1, i + 1).Style = headerStyle;
                    }

                    // Заполняем данные
                    int row = 2;
                    foreach (var incident in incidents)
                    {
                        worksheet.Cell(row, 1).Value = incident.NumerIncident;
                        worksheet.Cell(row, 2).Value = incident.RegistrationTime;
                        worksheet.Cell(row, 3).Value = incident.Service;
                        worksheet.Cell(row, 4).Value = incident.ShortDescription;
                        worksheet.Cell(row, 5).Value = incident.Applicant;
                        worksheet.Cell(row, 6).Value = incident.Priority;
                        worksheet.Cell(row, 7).Value = incident.Executor;
                        worksheet.Cell(row, 8).Value = incident.DecisionTime;
                        worksheet.Cell(row, 9).Value = incident.Status;
                        row++;
                    }

                    // Автоподбор ширины столбцов
                    worksheet.Columns().AdjustToContents();

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

                    // Установка шрифта для Linux
                    var fontPath = "/usr/share/fonts/truetype/freefont/FreeSans.ttf";

                    // Если шрифт не установлен, используем fallback
                    BaseFont baseFont;
                    if (File.Exists(fontPath))
                    {
                        baseFont = BaseFont.CreateFont(
                            fontPath,
                            BaseFont.IDENTITY_H,
                            BaseFont.EMBEDDED
                        );
                    }
                    else
                    {
                        // Fallback: используем встроенный шрифт с UTF-8 кодировкой
                        baseFont = BaseFont.CreateFont(
                            BaseFont.HELVETICA,
                            BaseFont.CP1250, // Более подходящая кодировка для кириллицы
                            BaseFont.EMBEDDED
                        );
                    }

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

                    // Создаем таблицу
                    var table = new PdfPTable(9)
                    {
                        WidthPercentage = 100,
                        SpacingBefore = 15f,
                        SpacingAfter = 20f,
                        HeaderRows = 1,
                        KeepTogether = true
                    };

                    // Настройка ширины колонок
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

                    // Данные
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

                    // Футер
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
