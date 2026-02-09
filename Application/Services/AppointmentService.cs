using Microsoft.EntityFrameworkCore;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Domain.Entities;
using SphereScheduleAPI.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SphereScheduleAPI.Application.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;

        public AppointmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Appointment> GetAppointmentByIdAsync(Guid appointmentId)
        {
            return await _context.Appointments
                .Include(a => a.Participants)
                .Include(a => a.Reminders)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && !a.IsDeleted);
        }

        public async Task<IEnumerable<Appointment>> GetUserAppointmentsAsync(Guid userId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            var query = _context.Appointments
                .Include(a => a.Participants)
                .Where(a => a.UserId == userId && !a.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(a => a.StartDateTime >= startDate);

            if (endDate.HasValue)
                query = query.Where(a => a.EndDateTime <= endDate);

            return await query
                .OrderBy(a => a.StartDateTime)
                .ToListAsync();
        }

        public async Task<Appointment> CreateAppointmentAsync(Appointment appointment)
        {
            // Validate time conflict
            if (await CheckTimeConflictAsync(appointment.UserId, appointment.StartDateTime, appointment.EndDateTime))
            {
                throw new InvalidOperationException("Time conflict with existing appointment");
            }

            appointment.AppointmentId = Guid.NewGuid();
            appointment.CreatedAt = DateTimeOffset.UtcNow;
            appointment.UpdatedAt = DateTimeOffset.UtcNow;
            appointment.IsDeleted = false;
            appointment.DeletedAt = null;

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            return appointment;
        }

        public async Task<Appointment> UpdateAppointmentAsync(Appointment appointment)
        {
            // Check if appointment exists
            var existing = await GetAppointmentByIdAsync(appointment.AppointmentId);
            if (existing == null)
                throw new KeyNotFoundException($"Appointment with ID {appointment.AppointmentId} not found");

            // Validate time conflict (excluding current appointment)
            if (await CheckTimeConflictAsync(appointment.UserId, appointment.StartDateTime, appointment.EndDateTime, appointment.AppointmentId))
            {
                throw new InvalidOperationException("Time conflict with existing appointment");
            }

            appointment.UpdatedAt = DateTimeOffset.UtcNow;
            _context.Entry(existing).CurrentValues.SetValues(appointment);
            await _context.SaveChangesAsync();
            return appointment;
        }

        public async Task<bool> DeleteAppointmentAsync(Guid appointmentId)
        {
            var appointment = await GetAppointmentByIdAsync(appointmentId);
            if (appointment == null) return false;

            appointment.IsDeleted = true;
            appointment.DeletedAt = DateTimeOffset.UtcNow;
            appointment.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Appointment>> GetAppointmentsByDateRangeAsync(Guid userId, DateTimeOffset startDate, DateTimeOffset endDate)
        {
            return await _context.Appointments
                .Include(a => a.Participants)
                .Where(a => a.UserId == userId &&
                           !a.IsDeleted &&
                           a.StartDateTime >= startDate &&
                           a.EndDateTime <= endDate)
                .OrderBy(a => a.StartDateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(Guid userId, int daysAhead = 7)
        {
            var now = DateTimeOffset.UtcNow;
            var futureDate = now.AddDays(daysAhead);

            return await _context.Appointments
                .Include(a => a.Participants)
                .Where(a => a.UserId == userId &&
                           !a.IsDeleted &&
                           a.Status == "scheduled" &&
                           a.StartDateTime >= now &&
                           a.StartDateTime <= futureDate)
                .OrderBy(a => a.StartDateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetAppointmentsByStatusAsync(Guid userId, string status)
        {
            return await _context.Appointments
                .Include(a => a.Participants)
                .Where(a => a.UserId == userId &&
                           !a.IsDeleted &&
                           a.Status == status)
                .OrderBy(a => a.StartDateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetAppointmentsByTypeAsync(Guid userId, string appointmentType)
        {
            return await _context.Appointments
                .Include(a => a.Participants)
                .Where(a => a.UserId == userId &&
                           !a.IsDeleted &&
                           a.AppointmentType == appointmentType)
                .OrderBy(a => a.StartDateTime)
                .ToListAsync();
        }

        public async Task<int> GetAppointmentCountByUserAsync(Guid userId)
        {
            return await _context.Appointments
                .CountAsync(a => a.UserId == userId && !a.IsDeleted);
        }

        public async Task<Dictionary<string, int>> GetAppointmentStatisticsAsync(Guid userId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            var query = _context.Appointments
                .Where(a => a.UserId == userId && !a.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(a => a.StartDateTime >= startDate);

            if (endDate.HasValue)
                query = query.Where(a => a.StartDateTime <= endDate);

            var appointments = await query.ToListAsync();

            return new Dictionary<string, int>
            {
                { "total", appointments.Count },
                { "scheduled", appointments.Count(a => a.Status == "scheduled") },
                { "completed", appointments.Count(a => a.Status == "completed") },
                { "cancelled", appointments.Count(a => a.Status == "cancelled") },
                { "upcoming", appointments.Count(a => a.Status == "scheduled" && a.StartDateTime > DateTimeOffset.UtcNow) },
                { "virtual", appointments.Count(a => a.IsVirtual) },
                { "recurring", appointments.Count(a => a.IsRecurring) }
            };
        }

        public async Task<bool> ChangeAppointmentStatusAsync(Guid appointmentId, string newStatus)
        {
            var appointment = await GetAppointmentByIdAsync(appointmentId);
            if (appointment == null) return false;

            appointment.Status = newStatus;
            appointment.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Appointment> RescheduleAppointmentAsync(Guid appointmentId, DateTimeOffset newStart, DateTimeOffset newEnd)
        {
            var appointment = await GetAppointmentByIdAsync(appointmentId);
            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found");

            // Validate time conflict
            if (await CheckTimeConflictAsync(appointment.UserId, newStart, newEnd, appointmentId))
            {
                throw new InvalidOperationException("Time conflict with existing appointment");
            }

            appointment.StartDateTime = newStart;
            appointment.EndDateTime = newEnd;
            appointment.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return appointment;
        }

        public async Task<bool> AddParticipantToAppointmentAsync(Guid appointmentId, Guid participantId)
        {
            var appointment = await GetAppointmentByIdAsync(appointmentId);
            if (appointment == null) return false;

            var participant = await _context.Participants.FindAsync(participantId);
            if (participant == null) return false;

            if (!appointment.Participants.Any(p => p.ParticipantId == participantId))
            {
                appointment.Participants.Add(participant);
                appointment.UpdatedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> RemoveParticipantFromAppointmentAsync(Guid appointmentId, Guid participantId)
        {
            var appointment = await GetAppointmentByIdAsync(appointmentId);
            if (appointment == null) return false;

            var participant = appointment.Participants.FirstOrDefault(p => p.ParticipantId == participantId);
            if (participant != null)
            {
                appointment.Participants.Remove(participant);
                appointment.UpdatedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<IEnumerable<Appointment>> SearchAppointmentsAsync(Guid userId, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetUserAppointmentsAsync(userId);

            return await _context.Appointments
                .Include(a => a.Participants)
                .Where(a => a.UserId == userId &&
                           !a.IsDeleted &&
                           (a.Title.Contains(searchTerm) ||
                            a.Description.Contains(searchTerm) ||
                            a.Location.Contains(searchTerm) ||
                            a.Notes.Contains(searchTerm)))
                .OrderBy(a => a.StartDateTime)
                .ToListAsync();
        }

        public async Task<bool> CheckTimeConflictAsync(Guid userId, DateTimeOffset start, DateTimeOffset end, Guid? excludeAppointmentId = null)
        {
            var query = _context.Appointments
                .Where(a => a.UserId == userId &&
                           !a.IsDeleted &&
                           a.Status != "cancelled" &&
                           ((start >= a.StartDateTime && start < a.EndDateTime) ||
                            (end > a.StartDateTime && end <= a.EndDateTime) ||
                            (start <= a.StartDateTime && end >= a.EndDateTime)));

            if (excludeAppointmentId.HasValue)
                query = query.Where(a => a.AppointmentId != excludeAppointmentId.Value);

            return await query.AnyAsync();
        }
    }
}