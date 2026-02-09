using SphereScheduleAPI.Application.DTOs;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface IReminderService
    {
        Task<ReminderDto> CreateReminderAsync(CreateReminderDto createDto);
        Task<ReminderDto> GetReminderByIdAsync(Guid reminderId);
        Task<IEnumerable<ReminderDto>> GetRemindersByUserIdAsync(Guid userId);
        Task<IEnumerable<ReminderDto>> GetRemindersByFilterAsync(ReminderFilterDto filterDto);
        Task<ReminderDto> UpdateReminderAsync(Guid reminderId, UpdateReminderDto updateDto);
        Task<bool> DeleteReminderAsync(Guid reminderId);
        Task<bool> MarkReminderAsSentAsync(Guid reminderId);
        Task<bool> CancelReminderAsync(Guid reminderId);
        Task<IEnumerable<ReminderDto>> GetPendingRemindersAsync(DateTimeOffset? beforeDate = null);
        Task<bool> RescheduleReminderAsync(Guid reminderId, DateTimeOffset newDateTime);
        Task<int> GetReminderCountByUserAsync(Guid userId, string? status = null);
    }
}