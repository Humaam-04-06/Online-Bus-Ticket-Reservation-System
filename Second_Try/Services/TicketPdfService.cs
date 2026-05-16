using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using Second_Try.Models;

namespace Second_Try.Services
{
    public class TicketPdfService
    {
        /// <summary>
        /// Generates a PDF boarding pass for the given accepted BookingRequest.
        /// The BookingRequest must have Route, Customer, AssignedBooking, and BusSchedule loaded.
        /// </summary>
        public byte[] GenerateTicket(BookingRequest req)
        {
            // ── Build QR Code payload ──────────────────────────────────────────
            var route   = req.Route;
            var cust    = req.Customer;
            var booking = req.AssignedBooking;
            var sched   = req.BusSchedule;

            string qrPayload =
                $"SRCTravel Ticket\n" +
                $"REF: TKT-{req.Id:D6}\n" +
                $"Passenger: {cust?.FullName}\n" +
                $"Route: {route?.Origin} → {route?.Destination}\n" +
                $"Date: {req.TravelDate:dd MMM yyyy}\n" +
                $"Seats: {req.SelectedSeatNumbers}\n" +
                $"Class: {req.PreferredBusType}\n" +
                $"Status: CONFIRMED";

            byte[] qrBytes = GenerateQrPng(qrPayload);

            // ── QuestPDF Document ──────────────────────────────────────────────
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5.Landscape());
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // ── Top Header bar ──────────────────────────────────
                        col.Item().Background(Color.FromHex("#0a1628")).Padding(18).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("SRC TRAVEL").FontSize(18).Bold().FontColor(Color.FromHex("#00D1FF"));
                                c.Item().Text("Online Bus Ticket Reservation").FontSize(9).FontColor(Color.FromHex("#94a3b8"));
                            });
                            row.ConstantItem(130).AlignRight().Column(c =>
                            {
                                c.Item().Text($"TKT-{req.Id:D6}").FontSize(14).Bold().FontColor(Color.FromHex("#ffffff")).FontFamily("Courier New");
                                c.Item().Text("BOARDING PASS").FontSize(8).FontColor(Color.FromHex("#00D1FF")).Bold();
                            });
                        });

                        // ── Route banner ────────────────────────────────────
                        col.Item().Background(Color.FromHex("#111827")).PaddingHorizontal(18).PaddingVertical(12).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(route?.Origin ?? "N/A").FontSize(22).Bold().FontColor(Color.FromHex("#ffffff"));
                                c.Item().Text("Origin").FontSize(8).FontColor(Color.FromHex("#6b7280"));
                            });

                            row.ConstantItem(60).AlignCenter().AlignMiddle().Text("→").FontSize(20).FontColor(Color.FromHex("#00D1FF")).Bold();

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(route?.Destination ?? "N/A").FontSize(22).Bold().FontColor(Color.FromHex("#ffffff"));
                                c.Item().Text("Destination").FontSize(8).FontColor(Color.FromHex("#6b7280"));
                            });

                            // Departure / Arrival if schedule exists
                            if (sched != null)
                            {
                                row.ConstantItem(100).AlignRight().Column(c =>
                                {
                                    c.Item().Text(DateTime.Today.Add(sched.DepartureTime).ToString("hh:mm tt")).FontSize(13).Bold().FontColor(Color.FromHex("#00D1FF"));
                                    c.Item().Text("→ " + DateTime.Today.Add(sched.ArrivalTime).ToString("hh:mm tt")).FontSize(11).FontColor(Color.FromHex("#94a3b8"));
                                });
                            }
                        });

                        // ── Main body (fields + QR) ──────────────────────────
                        col.Item().Background(Color.FromHex("#1e293b")).Padding(18).Row(row =>
                        {
                            // Left: passenger details
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().PaddingBottom(6).Row(r =>
                                {
                                    r.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text("PASSENGER").FontSize(7).FontColor(Color.FromHex("#6b7280")).Bold();
                                        inner.Item().Text(cust?.FullName ?? "N/A").FontSize(12).Bold().FontColor(Color.FromHex("#f1f5f9"));
                                    });
                                    r.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text("DATE").FontSize(7).FontColor(Color.FromHex("#6b7280")).Bold();
                                        inner.Item().Text(req.TravelDate.ToString("dd MMM yyyy")).FontSize(12).Bold().FontColor(Color.FromHex("#f1f5f9"));
                                    });
                                });

                                c.Item().PaddingBottom(6).Row(r =>
                                {
                                    r.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text("SEAT(S)").FontSize(7).FontColor(Color.FromHex("#6b7280")).Bold();
                                        inner.Item().Text(string.IsNullOrEmpty(req.SelectedSeatNumbers) ? (booking?.SeatNumbers ?? "N/A") : req.SelectedSeatNumbers)
                                            .FontSize(12).Bold().FontColor(Color.FromHex("#00D1FF"));
                                    });
                                    r.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text("CLASS").FontSize(7).FontColor(Color.FromHex("#6b7280")).Bold();
                                        inner.Item().Text(req.PreferredBusType.ToString()).FontSize(12).Bold().FontColor(Color.FromHex("#f1f5f9"));
                                    });
                                });

                                c.Item().PaddingBottom(6).Row(r =>
                                {
                                    r.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text("TOTAL FARE").FontSize(7).FontColor(Color.FromHex("#6b7280")).Bold();
                                        inner.Item().Text(booking != null ? $"Rs {booking.TotalFare:N0}" : "As per ticket booth")
                                            .FontSize(12).Bold().FontColor(Color.FromHex("#4ade80"));
                                    });
                                    r.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text("SEATS REQ.").FontSize(7).FontColor(Color.FromHex("#6b7280")).Bold();
                                        inner.Item().Text(req.NumberOfSeats.ToString()).FontSize(12).Bold().FontColor(Color.FromHex("#f1f5f9"));
                                    });
                                });

                                c.Item().Background(Color.FromHex("#0f172a")).Padding(8).Row(r =>
                                {
                                    r.AutoItem().PaddingRight(6).AlignMiddle().Text("★").FontSize(14).FontColor(Color.FromHex("#00D1FF"));
                                    r.RelativeItem().AlignMiddle().Text("Status: ACCEPTED — Present this ticket at the bus terminal.").FontSize(8).FontColor(Color.FromHex("#94a3b8"));
                                });
                            });

                            // Vertical divider
                            row.ConstantItem(1).Background(Color.FromHex("#334155")).Extend();
                            row.ConstantItem(10);

                            // Right: QR code
                            row.ConstantItem(110).AlignCenter().Column(c =>
                            {
                                c.Item().Image(qrBytes).FitArea();
                                c.Item().AlignCenter().Text("Scan to verify").FontSize(7).FontColor(Color.FromHex("#6b7280"));
                            });
                        });

                        // ── Footer ────────────────────────────────────────────
                        col.Item().Background(Color.FromHex("#0a1628")).PaddingHorizontal(18).PaddingVertical(8).Row(row =>
                        {
                            row.RelativeItem().Text($"Issued: {DateTime.Now:dd MMM yyyy, hh:mm tt}").FontSize(7).FontColor(Color.FromHex("#475569"));
                            row.RelativeItem().AlignCenter().Text("SRC Travel — Online Bus Reservation System").FontSize(7).FontColor(Color.FromHex("#475569"));
                            row.RelativeItem().AlignRight().Text($"Contact: src-travel@support.com").FontSize(7).FontColor(Color.FromHex("#475569"));
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        // ── QRCoder helper ─────────────────────────────────────────────────────
        private static byte[] GenerateQrPng(string text)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            var pngQr  = new PngByteQRCode(qrData);
            return pngQr.GetGraphic(6);  // 6px per module = ~240×240px
        }
    }
}
