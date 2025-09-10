using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;

namespace EventSphere.Services
{
    public static class CertificateGenerator
    {
        // Static constructor để set license Community
        static CertificateGenerator()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static byte[] GenerateCertificate(string studentName, string eventTitle, DateTime issuedOn)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(20));
                    page.Background("#FDFEFE");

                    page.Content().AlignCenter().Column(col =>
                    {
                        col.Spacing(20);

                        col.Item().Text("CERTIFICATE OF ACHIEVEMENT")
                            .FontSize(36).Bold().AlignCenter();

                        col.Item().Text("This is to certify that")
                            .FontSize(20).AlignCenter();

                        col.Item().Text(studentName)
                            .FontSize(32).Bold().FontColor("#2E86C1").AlignCenter();

                        col.Item().Text("has successfully participated in the event")
                            .FontSize(20).AlignCenter();

                        col.Item().Text(eventTitle)
                            .FontSize(28).Italic().AlignCenter();

                        col.Item().Text($"Issued on: {issuedOn:dd/MM/yyyy}")
                            .FontSize(18).AlignCenter().FontColor("#555");

                        col.Item().Text("Authorized Signature")
                            .AlignRight().FontSize(16).FontColor("#999");
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
