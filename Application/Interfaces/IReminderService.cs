using SphereScheduleAPI.Application.DTOs;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface IReminderService
    {
        Task<ReminderDto> CreateReminderAsync(CreateReminderDto createDto);
        Task<ReminderDto> GetReminderByIdAsync(Guid ReminderID);
        Task<IEnumerable<ReminderDto>> GetRemindersByUserIDAsync(Guid UserID);
        Task<IEnumerable<ReminderDto>> GetRemindersByFilterAsync(ReminderFilterDto filterDto);
        Task<ReminderDto> UpdateReminderAsync(Guid ReminderID, UpdateReminderDto updateDto);
        Task<bool> DeleteReminderAsync(Guid ReminderID);
        Task<bool> MarkReminderAsSentAsync(Guid ReminderID);
        Task<bool> CancelReminderAsync(Guid ReminderID);
        Task<IEnumerable<ReminderDto>> GetPendingRemindersAsync(DateTimeOffset? beforeDate = null);
        Task<bool> RescheduleReminderAsync(Guid ReminderID, DateTimeOffset newDateTime);
        Task<int> GetReminderCountByUserAsync(Guid UserID, string? status = null);
    }
}