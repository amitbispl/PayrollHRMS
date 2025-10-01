using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PayrollEmailWorker;

public class PdfService
{
    public PdfService()
    {
        // Set the free Community license
        QuestPDF.Settings.License = LicenseType.Community;
    }


public byte[] GeneratePayslip(string employeeName, decimal salary)
{
    var pdf = Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(12));

            // Header
            //page.Header()
            //    .AlignCenter()
            //    .Element(e => e.Image("RafflesLogo.png")); // Adjust path or embed resource
            //    //.Text("Raffles Udaipur")
            //    //.SemiBold().FontSize(16).FontColor(Colors.Black)
            //    //.Text("Tikheda, Girwa, Udaipur-313001")
            //    //.FontSize(10)
            //    //.Text($"Salary / Leave Slip for the month of : 21 June 2025 - 20 July 2025")
            //    //.FontSize(12).FontColor(Colors.Black);

            // Employee and Bank Details
            page.Content()
                .Column(col =>
                {
                    col.Item().Border(1).Padding(5).Column(innerCol =>
                    {
                        innerCol.Item().Text($"Name : {employeeName}");
                        innerCol.Item().Text("Emp. Code : R00589");
                        innerCol.Item().Text("Department : Guest Service Ambassador");
                        innerCol.Item().Text("Grade : 2");
                        innerCol.Item().Text("Join Date : 04 Sep 2023");
                    });

                    col.Item().Border(1).Padding(5).Column(innerCol =>
                    {
                        innerCol.Item().Text("Bank Name : BANK OF BARODA");
                        innerCol.Item().Text("Bank A/c No. : 1030810024200");
                        innerCol.Item().Text("IFSC Code : BARBNATHDW");
                        innerCol.Item().Text("PF No. : RJ204741087");
                        innerCol.Item().Text("ESIC : NA");
                        innerCol.Item().Text("UAN : 10155450404");
                    });

                    // Wage Heads Table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(100);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        // Table Header
                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Wage Heads").Bold();
                            header.Cell().Element(CellStyle).Text("Gross Rates").Bold();
                            header.Cell().Element(CellStyle).Text("Earnings").Bold();
                            header.Cell().Element(CellStyle).Text("Deductions").Bold();
                            header.Cell().Element(CellStyle).Text("Amount").Bold().AlignRight();
                        });

                        static IContainer CellStyle(IContainer container) => container.Border(1).Padding(5);

                        // Wage Details
                        table.Cell().Element(CellStyle).Text("Basic + DA");
                        table.Cell().Element(CellStyle).Text("15,000.00");
                        table.Cell().Element(CellStyle).Text("15,000.00");
                        table.Cell().Element(CellStyle).Text("E.P.F.");
                        table.Cell().Element(CellStyle).Text("1,800.00").AlignRight();

                        table.Cell().Element(CellStyle).Text("H.R.A.");
                        table.Cell().Element(CellStyle).Text("6,000.00");
                        table.Cell().Element(CellStyle).Text("6,000.00");
                        table.Cell().Element(CellStyle).Text("ESIC");
                        table.Cell().Element(CellStyle).Text("0.00").AlignRight();

                        table.Cell().Element(CellStyle).Text("Flexi Pay");
                        table.Cell().Element(CellStyle).Text("5,750.00");
                        table.Cell().Element(CellStyle).Text("5,750.00");
                        table.Cell().Element(CellStyle).Text("Hostel TDS");
                        table.Cell().Element(CellStyle).Text("0.00").AlignRight();

                        table.Cell().Element(CellStyle).Text("Leave Encashment");
                        table.Cell().Element(CellStyle).Text("0.00");
                        table.Cell().Element(CellStyle).Text("0.00");
                        table.Cell().Element(CellStyle).Text("TDS");
                        table.Cell().Element(CellStyle).Text("0.00").AlignRight();

                        table.Cell().Element(CellStyle).Text("Sp. All., Tip, Arrear & Other");
                        table.Cell().Element(CellStyle).Text("2,950.00");
                        table.Cell().Element(CellStyle).Text("2,950.00");
                        table.Cell().Element(CellStyle).Text("Statutory Deductions");
                        table.Cell().Element(CellStyle).Text("0.00").AlignRight();

                        //table.Cell().Element(CellStyle).Text("Total Earnings").RowSpan(2);
                        //table.Cell().Element(CellStyle).Text("29,700.00").RowSpan(2);
                        //table.Cell().Element(CellStyle).Text("29,700.00").RowSpan(2);
                        //table.Cell().Element(CellStyle).Text("Total Deductions").RowSpan(2);
                        //table.Cell().Element(CellStyle).Text("1,800.00").RowSpan(2).AlignRight();

                        table.Cell().Element(CellStyle).Text($"Net Amount Payable Rs. {salary:n2} /=");
                    });

                    // Notes
                    col.Item().Text($"(Rs. Twenty-Six Thousand Three Hundred Five only through Bank for the month of July 2025)")
                        .Italic().FontSize(10);
                    col.Item().Text("Email : rathorelaxmansingh41@gmail.com").FontSize(10);
                });

            // Footer
            //page.Footer()
            //    .AlignCenter()
            //    .Text($"Date : 28 July 2025").FontSize(10)
            //    .Text("Place : Udaipur")
            //    .Text("This is a computer generated statement, hence does not require any signature")
            //    .Column(col =>
            //    {
            //        col.Item().Text("Prepared By").FontSize(10);
            //        col.Item().Text("Checked By").FontSize(10);
            //        col.Item().Text("(Signature of Laxman Singh Rathore)").FontSize(10);
            //    })
            //    .Text("Name & Address of Employer :").FontSize(10)
            //    .Text("Varsha Enterprises Pvt Ltd.")
            //    .Text("Vil. Tikheda, Tehshil Girwa, Udaipur-313001");
        });
    });

    return pdf.GeneratePdf();
}

// Helper method to convert number to words (simplified)
private string NumberToWords(decimal number)
    {
        // This is a simplified version. For a complete implementation, use a dedicated library or expand this.
        if (number == 26305.00m) return "Twenty-Six Thousand Three Hundred Five";
        return number.ToString("N2"); // Fallback to numeric format

    }
}
