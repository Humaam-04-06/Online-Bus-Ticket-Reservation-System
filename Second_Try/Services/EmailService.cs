using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Second_Try.Services
{
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(string toEmail, string fullName);
        Task SendBookingConfirmedEmailAsync(string toEmail, string fullName, string route, DateTime travelDate, int seats, string busClass);
        Task SendBookingRejectedEmailAsync(string toEmail, string fullName, string route, DateTime travelDate, string? remarks);
        Task SendBookingCancelledEmailAsync(string toEmail, string fullName, string route, DateTime travelDate);
        Task SendPasswordChangedEmailAsync(string toEmail, string fullName);
        Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetUrl);
        Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        private string SmtpHost     => _config["EmailSettings:SmtpHost"]    ?? "smtp.gmail.com";
        private int    SmtpPort     => int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
        private string SenderEmail  => _config["EmailSettings:SenderEmail"]  ?? "";
        private string SenderName   => _config["EmailSettings:SenderName"]   ?? "SRCTravel";
        private string AppPassword  => _config["EmailSettings:AppPassword"]  ?? "";

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        // ── Core send method ─────────────────────────────────────
        public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(SenderName, SenderEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;

                message.Body = new BodyBuilder { HtmlBody = WrapInTemplate(subject, htmlBody) }.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(SenderEmail, AppPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("✅ Email sent to {Email} — Subject: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send email to {Email}", toEmail);
                // Don't throw — email failure should never crash the app
            }
        }

        // ── Welcome on register ───────────────────────────────────
        public async Task SendWelcomeEmailAsync(string toEmail, string fullName)
        {
            string body = $@"
                <h2>Welcome to SRCTravel, {fullName}! 🎉</h2>
                <p>Your account has been created successfully. You can now log in and start booking bus tickets.</p>
                <a href='https://localhost:7096/Auth/Login' class='btn'>Login to Your Account</a>
                <p style='margin-top:1.5rem;color:#888;font-size:0.85rem;'>If you did not create this account, please ignore this email.</p>";
            await SendEmailAsync(toEmail, fullName, "Welcome to SRCTravel! 🎉", body);
        }

        // ── Booking accepted ─────────────────────────────────────
        public async Task SendBookingConfirmedEmailAsync(string toEmail, string fullName,
            string route, DateTime travelDate, int seats, string busClass)
        {
            string body = $@"
                <h2>Booking Confirmed ✅</h2>
                <p>Great news, <strong>{fullName}</strong>! Your booking request has been <strong>accepted</strong>.</p>
                <table style='width:100%;border-collapse:collapse;margin:1rem 0;'>
                    <tr><td style='padding:8px;color:#888;'>Route</td><td style='padding:8px;font-weight:600;'>{route}</td></tr>
                    <tr style='background:#f9f9f9;'><td style='padding:8px;color:#888;'>Travel Date</td><td style='padding:8px;font-weight:600;'>{travelDate:dddd, MMMM dd, yyyy}</td></tr>
                    <tr><td style='padding:8px;color:#888;'>Bus Class</td><td style='padding:8px;font-weight:600;'>{busClass}</td></tr>
                    <tr style='background:#f9f9f9;'><td style='padding:8px;color:#888;'>Seats</td><td style='padding:8px;font-weight:600;'>{seats}</td></tr>
                </table>
                <p>Please arrive at the departure point at least 30 minutes before departure.</p>
                <a href='https://localhost:7096/Customer/MyRequests' class='btn'>View My Bookings</a>";
            await SendEmailAsync(toEmail, fullName, "Your Booking is Confirmed! ✅", body);
        }

        // ── Booking rejected ─────────────────────────────────────
        public async Task SendBookingRejectedEmailAsync(string toEmail, string fullName,
            string route, DateTime travelDate, string? remarks)
        {
            string body = $@"
                <h2>Booking Update ❌</h2>
                <p>Hi <strong>{fullName}</strong>, unfortunately your booking request for <strong>{route}</strong> on <strong>{travelDate:MMM dd, yyyy}</strong> could not be processed.</p>
                {(string.IsNullOrEmpty(remarks) ? "" : $"<p><strong>Reason:</strong> {remarks}</p>")}
                <p>You may submit a new request for a different date or route.</p>
                <a href='https://localhost:7096/Customer/NewRequest' class='btn'>Book Again</a>";
            await SendEmailAsync(toEmail, fullName, "Booking Request Update", body);
        }

        // ── Booking cancelled ─────────────────────────────────────
        public async Task SendBookingCancelledEmailAsync(string toEmail, string fullName,
            string route, DateTime travelDate)
        {
            string body = $@"
                <h2>Booking Cancelled</h2>
                <p>Hi <strong>{fullName}</strong>, your booking for <strong>{route}</strong> on <strong>{travelDate:MMM dd, yyyy}</strong> has been cancelled as requested.</p>
                <a href='https://localhost:7096/Customer/NewRequest' class='btn'>Book a New Trip</a>";
            await SendEmailAsync(toEmail, fullName, "Booking Cancelled", body);
        }

        // ── Password changed ─────────────────────────────────────
        public async Task SendPasswordChangedEmailAsync(string toEmail, string fullName)
        {
            string body = $@"
                <h2>Password Changed 🔐</h2>
                <p>Hi <strong>{fullName}</strong>, your SRCTravel account password was changed successfully.</p>
                <p>If you did not make this change, please contact us immediately.</p>";
            await SendEmailAsync(toEmail, fullName, "Password Changed — SRCTravel", body);
        }

        // ── Password reset request ────────────────────────────────
        public async Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetUrl)
        {
            string body = $@"
                <h2>Reset Your Password 🔑</h2>
                <p>Hi <strong>{fullName}</strong>, we received a request to reset your SRCTravel account password.</p>
                <p>Click the button below to set a new password. This link expires in <strong>30 minutes</strong>.</p>
                <a href='{resetUrl}' class='btn'>Reset My Password</a>
                <p style='margin-top:1.5rem;color:#888;font-size:0.85rem;'>
                    If you did not request a password reset, you can safely ignore this email.
                    Your password will not be changed.
                </p>";
            await SendEmailAsync(toEmail, fullName, "Reset Your SRCTravel Password", body);
        }

        // ── HTML email wrapper template ───────────────────────────
        private static string WrapInTemplate(string title, string content) => $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'>
<style>
  body {{ font-family: 'Segoe UI', Arial, sans-serif; background:#f0f4f8; margin:0; padding:0; }}
  .wrap {{ max-width:600px; margin:2rem auto; background:#fff; border-radius:12px; overflow:hidden; box-shadow:0 4px 20px rgba(0,0,0,0.08); }}
  .header {{ background:linear-gradient(135deg,#1e3a47,#5C7E8F); padding:2rem; text-align:center; }}
  .header h1 {{ color:#fff; margin:0; font-size:1.6rem; letter-spacing:0.5px; }}
  .header p {{ color:rgba(255,255,255,0.75); margin:0.25rem 0 0; font-size:0.9rem; }}
  .body {{ padding:2rem; color:#1F2937; line-height:1.7; }}
  .body h2 {{ color:#1e3a47; font-size:1.3rem; margin-top:0; }}
  .btn {{ display:inline-block; margin-top:1rem; padding:0.75rem 1.75rem; background:#5C7E8F; color:#fff!important;
          text-decoration:none; border-radius:8px; font-weight:600; font-size:0.9rem; }}
  .footer {{ background:#f9fafb; padding:1rem 2rem; text-align:center; color:#9CA3AF; font-size:0.78rem; border-top:1px solid #e5e7eb; }}
</style></head>
<body>
  <div class='wrap'>
    <div class='header'>
      <h1>🚌 SRCTravel</h1>
      <p>Online Bus Reservation System</p>
    </div>
    <div class='body'>{content}</div>
    <div class='footer'>© {DateTime.UtcNow.Year} SRCTravel. This is an automated email, please do not reply.</div>
  </div>
</body></html>";
    }
}
