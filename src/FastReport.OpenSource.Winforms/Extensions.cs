using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using FastReport.Export.Image;

namespace FastReport.OpenSource.Winforms
{
    public static class Extensions
    {
        public static void Print(this Report report, PrinterSettings settings = null)
        {
            if (report.PreparedPages.Count < 1)
            {
                report.Prepare();
                if (report.PreparedPages.Count < 1) return;
            }

            MemoryStream ms = null;
            var page = 0;
            var exp = new ImageExport { ImageFormat = ImageExportFormat.Png, Resolution = 600 };

            var doc = new PrintDocument { DocumentName = report.Name };
            if (settings != null)
                doc.PrinterSettings = settings;

            doc.QueryPageSettings += (sender, args) =>
            {
                var rPage = report.PreparedPages.GetPage(page);

                args.PageSettings.Landscape = rPage.Landscape;

                var width = (int)Math.Round(rPage.PaperWidth /= 0.254f);
                var height = (int)Math.Round(rPage.PaperHeight /= 0.254f);
                args.PageSettings.PaperSize = new PaperSize("Custom", width, height);
            };

            doc.BeginPrint += (sender, args) => ms?.Dispose();

            doc.PrintPage += (sender, args) =>
            {
                ms = new MemoryStream();
                exp.PageRange = PageRange.PageNumbers;
                exp.PageNumbers = $"{page + 1}";
                exp.Export(report, ms);

                args.Graphics.DrawImage(Image.FromStream(ms), args.PageBounds);
                page++;

                args.HasMorePages = page < report.PreparedPages.Count;
            };

            doc.EndPrint += (sender, args) => page = 0;

            doc.Disposed += (sender, args) =>
            {
                ms.Dispose();
                exp.Dispose();
            };

            doc.Print();
            doc.Dispose();
        }

        public static void Preview(this Report report, FRPrintPreviewControl preview)
        {
            if (report.PreparedPages.Count < 1)
            {
                report.Prepare();
                if (report.PreparedPages.Count < 1) return;
            }

            MemoryStream ms = null;
            var page = 0;
            var exp = new ImageExport { ImageFormat = ImageExportFormat.Png, Resolution = 600 };

            var doc = new PrintDocument { DocumentName = report.Name };

            doc.QueryPageSettings += (sender, args) =>
            {
                var rPage = report.PreparedPages.GetPage(page);

                args.PageSettings.Landscape = rPage.Landscape;

                var width = (int)Math.Round(rPage.PaperWidth /= 0.254f);
                var height = (int)Math.Round(rPage.PaperHeight /= 0.254f);
                args.PageSettings.PaperSize = new PaperSize("Custom", width, height);
            };

            doc.BeginPrint += (sender, args) => ms?.Dispose();

            doc.PrintPage += (sender, args) =>
            {
                ms = new MemoryStream();
                exp.PageRange = PageRange.PageNumbers;
                exp.PageNumbers = $"{page + 1}";
                exp.Export(report, ms);

                args.Graphics.DrawImage(Image.FromStream(ms), args.PageBounds);
                page++;

                args.HasMorePages = page < report.PreparedPages.Count;
            };

            doc.EndPrint += (sender, args) => page = 0;

            doc.Disposed += (sender, args) =>
            {
                ms.Dispose();
                exp.Dispose();
            };

            preview.Document = doc;
        }

        public static void Preview(this Report report)
        {
            if (report.PreparedPages.Count < 1)
            {
                report.Prepare();
                if (report.PreparedPages.Count < 1) return;
            }

            MemoryStream ms = null;
            var page = 0;
            var exp = new ImageExport { ImageFormat = ImageExportFormat.Png, Resolution = 400 };

            var doc = new PrintDocument { DocumentName = report.Name };

            doc.QueryPageSettings += (sender, args) =>
            {
                var rPage = report.PreparedPages.GetPage(page);

                args.PageSettings.Landscape = rPage.Landscape;

                var width = (int)Math.Round(rPage.PaperWidth /= 0.254f);
                var height = (int)Math.Round(rPage.PaperHeight /= 0.254f);
                args.PageSettings.PaperSize = new PaperSize("Custom", width, height);
            };

            doc.BeginPrint += (sender, args) => ms?.Dispose();

            doc.PrintPage += (sender, args) =>
            {
                ms = new MemoryStream();
                exp.PageRange = PageRange.PageNumbers;
                exp.PageNumbers = $"{page + 1}";
                exp.Export(report, ms);

                args.Graphics.DrawImage(Image.FromStream(ms), args.PageBounds);
                page++;

                args.HasMorePages = page < report.PreparedPages.Count;
            };

            doc.EndPrint += (sender, args) => page = 0;

            doc.Disposed += (sender, args) =>
            {
                ms.Dispose();
                exp.Dispose();
            };

            using (var preview = new FRPrintPreviewDialog() { Document = doc })
                preview.ShowDialog();
            doc.Dispose();
        }
    }
}