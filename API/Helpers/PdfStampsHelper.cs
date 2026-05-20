using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace API.Helpers
{
	public class PdfStampInfo
	{
		public string CertNumber { get; set; } = string.Empty;
		public string Owner { get; set; } = string.Empty;
		public DateTime ValidFrom { get; set; }
		public DateTime ValidTo { get; set; }
	}

	public static class PdfStampsHelper
	{
		public static MemoryStream StampPdf(Stream originalPdfStream, List<PdfStampInfo> stamps)
		{
			var outputStream = new MemoryStream();
			originalPdfStream.Position = 0;
			originalPdfStream.CopyTo(outputStream);
			outputStream.Position = 0;

			using (var pdfDocument = PdfReader.Open(outputStream, PdfDocumentOpenMode.Modify))
			{
				double startX = (595 - 400) / 2; // Центрируем по ширине A4 (595 точек)
				double startY = 50; // Верхний отступ
				double stampHeight = 90;
				double gap = 15;
				int stampsPerPage = 7;

				XGraphics? gfx = null;

				for (int i = 0; i < stamps.Count; i++)
				{
					if (i % stampsPerPage == 0)
					{
						var page = pdfDocument.AddPage();
						page.Size = PdfSharpCore.PageSize.A4;
						gfx = XGraphics.FromPdfPage(page);
					}

					int indexOnPage = i % stampsPerPage;
					double y = startY + indexOnPage * (stampHeight + gap);

					var stamp = stamps[i];
					DrawSignatureStamp(gfx!, startX, y, stamp.CertNumber, stamp.Owner, stamp.ValidFrom, stamp.ValidTo);
				}

				pdfDocument.Save(outputStream);
			}

			outputStream.Position = 0;
			return outputStream;
		}

		private static void DrawSignatureStamp(XGraphics gfx, double x, double y, string certNumber, string ownerName, DateTime validFrom, DateTime validTo)
		{
			double width = 400;
			double height = 90;

			// Основной синий цвет для штампов ЭЦП
			XColor stampColor = XColor.FromArgb(0, 84, 165);
			XPen borderPen = new XPen(stampColor, 1.5);
			XPen innerPen = new XPen(stampColor, 0.5);

			// Внешняя рамка
			gfx.DrawRectangle(borderPen, x, y, width, height);
			// Внутренняя тонкая рамка
			gfx.DrawRectangle(innerPen, x + 3, y + 3, width - 6, height - 6);

			// Шрифты
			XFont titleFont = new XFont("Arial", 9, XFontStyle.Bold);
			XFont labelFont = new XFont("Arial", 8, XFontStyle.Regular);

			XSolidBrush brush = new XSolidBrush(stampColor);

			// Заголовок штампа
			var title1 = "ДОКУМЕНТ ПОДПИСАН";
			var title2 = "ЭЛЕКТРОННОЙ ПОДПИСЬЮ";

			var size1 = gfx.MeasureString(title1, titleFont);
			gfx.DrawString(title1, titleFont, brush, x + (width - size1.Width) / 2, y + 15);

			var size2 = gfx.MeasureString(title2, titleFont);
			gfx.DrawString(title2, titleFont, brush, x + (width - size2.Width) / 2, y + 27);

			// Разделительная линия
			gfx.DrawLine(innerPen, x + 10, y + 33, x + width - 10, y + 33);

			// Данные сертификата
			string certText = $"Сертификат: {TruncateString(certNumber, 50)}";
			string ownerText = $"Владелец: {TruncateString(ownerName, 60)}";
			string validityText = $"Действителен: с {validFrom:dd.MM.yyyy} по {validTo:dd.MM.yyyy}";

			gfx.DrawString(certText, labelFont, brush, x + 15, y + 46);
			gfx.DrawString(ownerText, labelFont, brush, x + 15, y + 58);
			gfx.DrawString(validityText, labelFont, brush, x + 15, y + 70);
		}

		private static string TruncateString(string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value)) return value;
			return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
		}
	}
}
