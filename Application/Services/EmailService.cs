using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SphereScheduleAPI.Application.Interfaces;

namespace SphereScheduleAPI.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(
            ILogger<EmailService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            _logger.LogInformation("Sending email to {To} with subject: {Subject}", to, subject);

            // TODO: Implement actual email sending (SMTP, SendGrid, etc.)
            // For now, just log the action

            await Task.CompletedTask;
        }

        public async Task SendInvitationEmailAsync(string to, string appointmentTitle, DateTimeOffset startTime,
                                                 DateTimeOffset endTime, string? location, string? meetingLink)
        {
            var subject = $"Invitation: {appointmentTitle}";
            var body = $@"
                <h2>You're Invited!</h2>
                <p>You have been invited to: <strong>{appointmentTitle}</strong></p>
                <p><strong>Date/Time:</strong> {startTime:g} - {endTime:g}</p>
                <p><strong>Location:</strong> {(string.IsNullOrEmpty(location) ? "Virtual" : location)}</p>
                {(string.IsNullOrEmpty(meetingLink) ? "" : $"<p><strong>Meeting Link:</strong> <a href='{meetingLink}'>{meetingLink}</a></p>")}
                <br>
                <p>Please respond to this invitation.</p>
            ";

            await SendEmailAsync(to, subject, body, true);
        }

        public async Task SendReminderEmailAsync(string to, string reminderTitle, string message, DateTimeOffset reminderTime)
        {
            var subject = $"Reminder: {reminderTitle}";
            var body = $@"
                <h2>Reminder</h2>
                <p><strong>{reminderTitle}</strong></p>
                <p>{message}</p>
                <p><strong>Reminder Time:</strong> {reminderTime:g}</p>
            ";

            await SendEmailAsync(to, subject, body, true);
        }
    }
}