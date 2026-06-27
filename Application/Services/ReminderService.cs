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
                var userExists = await _context.Users.AnyAsync(u => u.UserID == createDto.UserID && u.IsActive);
                if (!userExists)
                {
                    throw new ArgumentException($"User with ID {createDto.UserID} not found or inactive");
                }

                // Validate task exists if TaskID is provided
                if (createDto.TaskID.HasValue)
                {
                    var taskExists = await _context.Tasks.AnyAsync(t => t.TaskID == createDto.TaskID.Value && !t.IsDeleted);
                    if (!taskExists)
                    {
                        throw new ArgumentException($"Task with ID {createDto.TaskID} not found");
                    }
                }

                // Validate appointment exists if AppointmentID is provided
                if (createDto.AppointmentID.HasValue)
                {
                    var appointmentExists = await _context.Appointments.AnyAsync(a => a.AppointmentID == createDto.AppointmentID.Value && !a.IsDeleted);
                    if (!appointmentExists)
                    {
                        throw new ArgumentException($"Appointment with ID {createDto.AppointmentID} not found");
                    }
                }

                var reminder = _mapper.Map<Reminder>(createDto);
                reminder.ReminderID = Guid.NewGuid();
                reminder.CreatedAt = DateTimeOffset.UtcNow;
                reminder.UpdatedAt = DateTimeOffset.UtcNow;

                _context.Reminders.Add(reminder);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created reminder {ReminderID} for user {UserID}", reminder.ReminderID, reminder.UserID);

                return _mapper.Map<ReminderDto>(reminder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reminder for user {UserID}", createDto.UserID);
                throw;
            }
        }

        public async Task<ReminderDto> GetReminderByIdAsync(Guid ReminderID)
        {
            var reminder = await _context.Reminders
                .Include(r => r.User)
                .Include(r => r.Task)
                .Include(r => r.Appointment)
                .FirstOrDefaultAsync(r => r.ReminderID == ReminderID);

            if (reminder == null)
            {
                throw new KeyNotFoundException($"Reminder with ID {ReminderID} not found");
            }

            return _mapper.Map<ReminderDto>(reminder);
        }

        public async Task<IEnumerable<ReminderDto>> GetRemindersByUserIDAsync(Guid UserID)
        {
            var reminders = await _context.Reminders
                .Where(r => r.UserID == UserID)
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
            if (filterDto.UserID.HasValue)
            {
                query = query.Where(r => r.UserID == filterDto.UserID.Value);
            }

            if (filterDto.TaskID.HasValue)
            {
                query = query.Where(r => r.TaskID == filterDto.TaskID.Value);
            }

            if (filterDto.AppointmentID.HasValue)
            {
                query = query.Where(r => r.AppointmentID == filterDto.AppointmentID.Value);
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

        public async Task<ReminderDto> UpdateReminderAsync(Guid ReminderID, UpdateReminderDto updateDto)
        {
            var reminder = await _context.Reminders.FindAsync(ReminderID);
            if (reminder == null)
            {
                throw new KeyNotFoundException($"Reminder with ID {ReminderID} not found");
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
            _logger.LogInformation("Updated reminder {ReminderID}", ReminderID);

            return await GetReminderByIdAsync(ReminderID);
        }

        public async Task<bool> DeleteReminderAsync(Guid ReminderID)
        {
            var reminder = await _context.Reminders.FindAsync(ReminderID);
            if (reminder == null)
            {
                throw new KeyNotFoundException($"Reminder with ID {ReminderID} not found");
            }

            _context.Reminders.Remove(reminder);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted reminder {ReminderID}", ReminderID);
            return true;
        }

        public async Task<bool> MarkReminderAsSentAsync(Guid ReminderID)
        {
            var reminder = await _context.Reminders.FindAsync(ReminderID);
            if (reminder == null)
            {
                throw new KeyNotFoundException($"Reminder with ID {ReminderID} not found");
            }

            reminder.Status = "sent";
            reminder.SentAt = DateTimeOffset.UtcNow;
            reminder.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Marked reminder {ReminderID} as sent", ReminderID);
            return true;
        }

        public async Task<bool> CancelReminderAsync(Guid ReminderID)
        {
            var reminder = await _context.Reminders.FindAsync(ReminderID);
            if (reminder == null)
            {
                throw new KeyNotFoundException($"Reminder with ID {ReminderID} not found");
            }

            // Only allow cancellation of pending reminders
            if (reminder.Status != "pending")
            {
                throw new InvalidOperationException($"Cannot cancel reminder with status {reminder.Status}. Only pending reminders can be cancelled.");
            }

            reminder.Status = "cancelled";
            reminder.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Cancelled reminder {ReminderID}", ReminderID);
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

        public async Task<bool> RescheduleReminderAsync(Guid ReminderID, DateTimeOffset newDateTime)
        {
            var reminder = await _context.Reminders.FindAsync(ReminderID);
            if (reminder == null)
            {
                throw new KeyNotFoundException($"Reminder with ID {ReminderID} not found");
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
            _logger.LogInformation("Rescheduled reminder {ReminderID} to {NewDateTime}", ReminderID, newDateTime);
            return true;
        }

        public async Task<int> GetReminderCountByUserAsync(Guid UserID, string? status = null)
        {
            var query = _context.Reminders.Where(r => r.UserID == UserID);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            return await query.CountAsync();
        }
    }
}