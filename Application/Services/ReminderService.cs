using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Domain.Entities;
using SphereScheduleAPI.Infrastructure.Data;

namespace SphereScheduleAPI.Application.Services
{
    public class ReminderService : IReminderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ReminderService> _logger;

        public ReminderService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<ReminderService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ReminderDto> CreateReminderAsync(CreateReminderDto createDto)
        {
            try
            {
                // Validate user exists
                var userExists = await _context.Users.AnyAsync(u => u.UserId == createDto.UserId && u.IsActive);
                if (!userExists)
                {
                    throw new ArgumentException($"User with ID {createDto.UserId} not found or inactive");
                }

                // Validate task exists if TaskId is provided
                if (createDto.TaskId.HasValue)
                {
                    var taskExists = await _context.Tasks.AnyAsync(t => t.TaskId == createDto.TaskId.Value && !t.IsDeleted);
                    if (!taskExists)
                    {
                        throw new ArgumentException($"Task with ID {createDto.TaskId} not found");
                    }
                }

                // Validate appointment exists if AppointmentId is provided
                if (createDto.AppointmentId.HasValue)
                {
                    var appointmentExists = await _context.Appointments.AnyAsync(a => a.AppointmentId == createDto.AppointmentId.Value && !a.IsDeleted);
                    if (!appointmentExists)
                    {
                        throw new ArgumentException($"Appointment with ID {createDto.AppointmentId} not found");
                    }
                }

                var reminder = _mapper.Map<Reminder>(createDto);
                reminder.ReminderId = Guid.NewGuid();
                reminder.CreatedAt = DateTimeOffset.UtcNow;
                reminder.UpdatedAt = DateTimeOffset.UtcNow;

                _context.Reminders.Add(reminder);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created reminder {ReminderId} for user {UserId}", reminder.ReminderId, reminder.UserId);

                return _mapper.Map<ReminderDto>(reminder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reminder for user {UserId}", createDto.UserId);
                throw;
            }
        }

        public async Task<ReminderDto> GetReminderByIdAsync(Guid reminderId)
        {
            var reminder = await _context.Reminders
                .Include(r => r.User)
                .Include(r => r.Task)
                .Include(r => r.Appointment)
                .FirstOrDefaultAsync(r => r.ReminderId == reminderId);

            if (reminder == null)
            {
                throw new KeyNotFoundException($"Reminder with ID {reminderId} not found");
            }

            return _mapper.Map<ReminderDto>(reminder);
        }

        public async Task<IEnumerable<ReminderDto>> GetRemindersByUserIdAsync(Guid userId)
        {
            var reminders = await _context.Reminders
                .Where(r => r.UserId == userId)
                .Include(r => r.Task)
                .Include(r => r.Appointment)
                .OrderByDescending(r => r.ReminderDateTime)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ReminderDto>>(reminders);
        }

        public async Task<IEnumerable<ReminderDto>> GetRemindersByFilterAsync(ReminderFilterDto filterDto)
        {
            var query = _context.Reminders
                .Include(r => r.User)
                .Include(r => r.Task)
                .Include(r => r.Appointment)
                .AsQueryable();

            // Apply filters
            if (filterDto.UserId.HasValue)
            {
                query = query.Where(r => r.UserId == filterDto.UserId.Value);
            }

            if (filterDto.TaskId.HasValue)
            {
                query = query.Where(r => r.TaskId == filterDto.TaskId.Value);
            }

            if (filterDto.AppointmentId.HasValue)
            {
                query = query.Where(r => r.AppointmentId == filterDto.AppointmentId.Value);
            }

            if (!string.IsNullOrEmpty(filterDto.ReminderType))
            {
                query = query.Where(r => r.ReminderType == filterDto.ReminderType);
            }

            if (!string.IsNullOrEmpty(filterDto.Status))
            {
                query = query.Where(r => r.Status == filterDto.Status);
            }

            if (filterDto.FromDate.HasValue)
            {
                query = query.Where(r => r.ReminderDateTime >= filterDto.FromDate.Value);
            }

            if (filterDto.ToDate.HasValue)
            {
                query = query.Where(r => r.ReminderDateTime <= filterDto.ToDate.Value);
            }

            if (filterDto.IsRecurring.HasValue)
            {
                query = query.Where(r => r.IsRecurring == filterDto.IsRecurring.Value);
            }

            if (!filterDto.IncludeSent.HasValue || !filterDto.IncludeSent.Value)
            {
                query = query.Where(r => r.Status != "sent");
            }

            // Apply pagination
            query = query
                .Skip((filterDto.PageNumber - 1) * filterDto.PageSize)
                .Take(filterDto.PageSize)
                .OrderBy(r => r.ReminderDateTime);

            var reminders = await query.ToListAsync();
            return _mapper.Map<IEnumerable<ReminderDto>>(reminders);
        }

        public async Task<ReminderDto> UpdateReminderAsync(Guid reminderId, UpdateReminderDto updateDto)
        {
            var reminder = await _context.Reminders.FindAsync(reminderId);
            if (reminder == null)
            {
                throw new KeyNotFoundException($"Reminder with ID {reminderId} not found");
            }

            // Only allow updates to pending reminders
            if (reminder.Status != "pending")
            {
                throw new InvalidOperationException($"Cannot update reminder with status {reminder.Status}. Only pending reminders can be updated.");
            }

            // Update properties if provided
            if (!string.IsNullOrEmpty(updateDto.Title))
            {
                reminder.Title = updateDto.Title;
            }

            if (updateDto.Message != null)
            {
                reminder.Message = updateDto.Message;
            }

            if (updateDto.ReminderDateTime.HasValue)
            {
                reminder.ReminderDateTime = updateDto.ReminderDateTime.Value;
            }

            if (updateDto.NotifyViaEmail.HasValue)
            {
                reminder.NotifyViaEmail = updateDto.NotifyViaEmail.Value;
            }

            if (updateDto.NotifyViaPush.HasValue)
            {
                reminder.NotifyViaPush = updateDto.NotifyViaPush.Value;
            }

            if (!string.IsNullOrEmpty(updateDto.Status))
            {
                reminder.Status = updateDto.Status;
            }

            if (updateDto.IsRecurring.HasValue)
            {
                reminder.IsRecurring = updateDto.IsRecurring.Value;
            }

            reminder.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated reminder {ReminderId}", reminderId);

            return await GetReminderByIdAsync(reminderId);
        }

        public async Task<bool> DeleteReminderAsync(Guid reminderId)
        {
            var reminder = await _context.Reminders.FindAsync(reminderId);
            if (reminder == null)
            {
                throw new KeyNotFoundException($"Reminder with ID {reminderId} not found");
            }

            _context.Reminders.Remove(reminder);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted reminder {ReminderId}", reminderId);
            return true;
        }

        public async Task<bool> MarkReminderAsSentAsync(Guid reminderId)
        {
            var reminder = await _context.Reminders.FindAsync(reminderId);
            if (reminder == null)
            {
                throw new KeyNotFoundException($"Reminder with ID {reminderId} not found");
            }

            reminder.Status = "sent";
            reminder.SentAt = DateTimeOffset.UtcNow;
            reminder.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Marked reminder {ReminderId} as sent", reminderId);
            return true;
        }

        public async Task<bool> CancelReminderAsync(Guid reminderId)
        {
            var reminder = await _context.Reminders.FindAsync(reminderId);
            if (reminder == null)
            {
                throw new KeyNotFoundException($"Reminder with ID {reminderId} not found");
            }

            // Only allow cancellation of pending reminders
            if (reminder.Status != "pending")
            {
                throw new InvalidOperationException($"Cannot cancel reminder with status {reminder.Status}. Only pending reminders can be cancelled.");
            }

            reminder.Status = "cancelled";
            reminder.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Cancelled reminder {ReminderId}", reminderId);
            return true;
        }

        public async Task<IEnumerable<ReminderDto>> GetPendingRemindersAsync(DateTimeOffset? beforeDate = null)
        {
            var query = _context.Reminders
                .Include(r => r.User)
                .Include(r => r.Task)
                .Include(r => r.Appointment)
                .Where(r => r.Status == "pending");

            if (beforeDate.HasValue)
            {
                query = query.Where(r => r.ReminderDateTime <= beforeDate.Value);
            }
            else
            {
                // Default: get reminders that are due now or in the past
                query = query.Where(r => r.ReminderDateTime <= DateTimeOffset.UtcNow);
            }

            var reminders = await query
                .OrderBy(r => r.ReminderDateTime)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ReminderDto>>(reminders);
        }

        public async Task<bool> RescheduleReminderAsync(Guid reminderId, DateTimeOffset newDateTime)
        {
            var reminder = await _context.Reminders.FindAsync(reminderId);
            if (reminder == null)
            {
                throw new KeyNotFoundException($"Reminder with ID {reminderId} not found");
            }

            // Only allow rescheduling of pending reminders
            if (reminder.Status != "pending")
            {
                throw new InvalidOperationException($"Cannot reschedule reminder with status {reminder.Status}. Only pending reminders can be rescheduled.");
            }

            // Validate new date is in the future
            if (newDateTime <= DateTimeOffset.UtcNow)
            {
                throw new ArgumentException("New reminder date must be in the future");
            }

            reminder.ReminderDateTime = newDateTime;
            reminder.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Rescheduled reminder {ReminderId} to {NewDateTime}", reminderId, newDateTime);
            return true;
        }

        public async Task<int> GetReminderCountByUserAsync(Guid userId, string? status = null)
        {
            var query = _context.Reminders.Where(r => r.UserId == userId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            return await query.CountAsync();
        }
    }
}