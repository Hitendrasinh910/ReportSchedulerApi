using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;

namespace CNCMachineService.Helper
{
    public class PdfColumn
    {
        public string Header { get; set; }
        public float Width { get; set; }
        public int Alignment { get; set; } = Element.ALIGN_CENTER;
    }

    public class PdfSummaryItem
    {
        public string Label { get; set; }
        public string Value { get; set; }
    }

    public static class AtlasReportHelper
    {
        public static MemoryStream GenerateTableReport(
            string title,
            DateTime fromDate,
            DateTime toDate,
            List<PdfColumn> columns,
            List<List<string>> rows)
        {
            return GenerateTableReport(title, fromDate, toDate, null, columns, rows);
        }

        public static MemoryStream GenerateTableReport(
            string title,
            DateTime fromDate,
            DateTime toDate,
            List<PdfSummaryItem> summaryItems,
            List<PdfColumn> columns,
            List<List<string>> rows)
        {
            var ms = new MemoryStream();

            var doc = new Document(PageSize.A4.Rotate(), 10, 10, 20, 20);
            var writer = PdfWriter.GetInstance(doc, ms);
            writer.CloseStream = false;

            doc.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8);

            doc.Add(new Paragraph(title, titleFont));
            doc.Add(new Paragraph($"From Date : {fromDate:dd-MM-yyyy}", normalFont));
            doc.Add(new Paragraph($"To Date : {toDate:dd-MM-yyyy}", normalFont));
            doc.Add(new Paragraph($"Generated : {DateTime.Now:dd-MM-yyyy HH:mm}", normalFont));
            doc.Add(new Paragraph("This is a system generated report", normalFont));
            doc.Add(new Paragraph(" "));

            if (summaryItems != null && summaryItems.Count > 0)
            {
                PdfPTable summaryTable = new PdfPTable(2);
                summaryTable.WidthPercentage = 35;
                summaryTable.HorizontalAlignment = Element.ALIGN_LEFT;
                summaryTable.SetWidths(new float[] { 3f, 2f });

                foreach (var item in summaryItems)
                {
                    AddCell(summaryTable, item.Label, boldFont, true, Element.ALIGN_LEFT);
                    AddCell(summaryTable, item.Value, normalFont, false, Element.ALIGN_RIGHT);
                }

                doc.Add(summaryTable);
                doc.Add(new Paragraph(" "));
            }

            PdfPTable table = new PdfPTable(columns.Count);
            table.WidthPercentage = 100;

            float[] widths = new float[columns.Count];

            for (int i = 0; i < columns.Count; i++)
                widths[i] = columns[i].Width;

            table.SetWidths(widths);

            foreach (var col in columns)
                AddCell(table, col.Header, boldFont, true, col.Alignment);

            foreach (var row in rows)
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    string value = i < row.Count ? row[i] : "";
                    AddCell(table, value, normalFont, false, columns[i].Alignment);
                }
            }

            doc.Add(table);
            doc.Close();

            ms.Position = 0;
            return ms;
        }

        private static void AddCell(
            PdfPTable table,
            string text,
            Font font,
            bool isHeader,
            int horizontalAlignment)
        {
            var cell = new PdfPCell(new Phrase(text ?? "", font))
            {
                HorizontalAlignment = horizontalAlignment,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 4,
                BorderWidth = 0.5f,
                NoWrap = false
            };

            if (isHeader)
                cell.BackgroundColor = new BaseColor(220, 220, 220);

            table.AddCell(cell);
        }
    }
}