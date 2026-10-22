using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace Clinic_Management_System.Export
{
    public record ExcelColumn<T>(string Header, Func<T, object?> Value);

    public static class ExcelFile
    {
        public const int MaxRows = 10_000;

        public const string SpreadsheetContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public static FileContentResult Build<T>(
            string sheetName, 
            IReadOnlyList<ExcelColumn<T>> columns, 
            IReadOnlyList<T> rows)
        {
            using XLWorkbook workbook = new XLWorkbook();
            IXLWorksheet sheet = workbook.AddWorksheet(sheetName);

            for (int column = 0; column < columns.Count; column++)
                sheet.Cell(1, column + 1).Value = columns[column].Header;
            
            sheet.Row(1).Style.Font.Bold = true;
            sheet.SheetView.FreezeRows(1);

            for (int row = 0; row < rows.Count; row++)
            {
                for (int column = 0; column < columns.Count; column++)
                    sheet.Cell(row + 2, column + 1).Value = ToCellValue(columns[column].Value(rows[row]));
            }

            sheet.Columns().AdjustToContents();

            using MemoryStream stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new FileContentResult(stream.ToArray(), SpreadsheetContentType)
            {
                FileDownloadName = $"{sheetName}-{DateTime.Now:yyyy-MM-dd}.xlsx"
            };
        }

        /// <summary>
        /// Keeps a value's type on the way into the sheet, so dates sort as dates
        /// and numbers total as numbers. Anything unrecognised falls back to text,
        /// which is wrong-looking rather than an exception mid-export.
        /// </summary>
        private static XLCellValue ToCellValue(object? value) => value switch
        {
            null => Blank.Value,
            string text => text,
            bool flag => flag,
            DateTimeOffset moment => moment.LocalDateTime,
            DateTime timestamp => timestamp,
            DateOnly date => date.ToDateTime(TimeOnly.MinValue),
            TimeSpan span => span,
            int number => number,
            long number => number,
            decimal number => number,
            double number => number,
            _ => value.ToString() ?? string.Empty
        };
    }
}
