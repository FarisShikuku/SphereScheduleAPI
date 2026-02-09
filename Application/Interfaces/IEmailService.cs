namespace SphereScheduleAPI.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = false);
        Task SendInvitationEmailAsync(string to, string appointmentTitle, DateTimeOffset startTime,
                                     DateTimeOffset endTime, string? location, string? meetingLink);
        Task SendReminderEmailAsync(string to, string reminderTitle, string message, DateTimeOffset reminderTime);
    }
}