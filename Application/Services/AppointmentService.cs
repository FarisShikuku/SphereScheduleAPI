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

        public async Task<Appointment> GetAppointmentByIdAsync(Guid AppointmentID)
        {
            return await _context.Appointments
                .Include(a => a.Participants)
                .Include(a => a.Reminders)
                .FirstOrDefaultAsync(a => a.AppointmentID == AppointmentID && !a.IsDeleted);
        }

        public async Task<IEnumerable<Appointment>> GetUserAppointmentsAsync(Guid UserID, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            var query = _context.Appointments
                .Include(a => a.Participants)
                .Where(a => a.UserID == UserID && !a.IsDeleted);

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
            if (await CheckTimeConflictAsync(appointment.UserID, appointment.StartDateTime, appointment.EndDateTime))
            {
                throw new InvalidOperationException("Time conflict with existing appointment");
            }

            appointment.AppointmentID = Guid.NewGuid();
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
            var existing = await GetAppointmentByIdAsync(appointment.AppointmentID);
            if (existing == null)
                throw new KeyNotFoundException($"Appointment with ID {appointment.AppointmentID} not found");

            // Validate time conflict (excluding current appointment)
            if (await CheckTimeConflictAsync(appointment.UserID, appointment.StartDateTime, appointment.EndDateTime, appointment.AppointmentID))
            {
                throw new InvalidOperationException("Time conflict with existing appointment");
            }

            appointment.UpdatedAt = DateTimeOffset.UtcNow;
            _context.Entry(existing).CurrentValues.SetValues(appointment);
            await _context.SaveChangesAsync();
            return appointment;
        }

        public async Task<bool> DeleteAppointmentAsync(Guid AppointmentID)
        {
            var appointment = await GetAppointmentByIdAsync(AppointmentID);
            if (appointment == null) return false;

            appointment.IsDeleted = true;
            appointment.DeletedAt = DateTimeOffset.UtcNow;
            appointment.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Appointment>> GetAppointmentsByDateRangeAsync(Guid UserID, DateTimeOffset startDate, DateTimeOffset endDate)
        {
            return await _context.Appointments
                .Include(a => a.Participants)
                .Where(a => a.UserID == UserID &&
                           !a.IsDeleted &&
                           a.StartDateTime >= startDate &&
                           a.EndDateTime <= endDate)
                .OrderBy(a => a.StartDateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(Guid UserID, int daysAhead = 7)
        {
            var now = DateTimeOffset.UtcNow;
            var futureDate = now.AddDays(daysAhead);

            return await _context.Appointments
                .Include(a => a.Participants)
                .Where(a => a.UserID == UserID &&
                           !a.IsDeleted &&
                           a.Status == "scheduled" &&
                           a.StartDateTime >= now &&
                           a.StartDateTime <= futureDate)
                .OrderBy(a => a.StartDateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetAppointmentsByStatusAsync(Guid UserID, string status)
        {
            return await _context.Appointments
                .Include(a => a.Participants)
                .Where(a => a.UserID == UserID &&
                           !a.IsDeleted &&
                           a.Status == status)
                .OrderBy(a => a.StartDateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetAppointmentsByTypeAsync(Guid UserID, string appointmentType)
        {
            return await _context.Appointments
                .Include(a => a.Participants)
                .Where(a => a.UserID == UserID &&
                           !a.IsDeleted &&
                           a.AppointmentType == appointmentType)
                .OrderBy(a => a.StartDateTime)
                .ToListAsync();
        }

        public async Task<int> GetAppointmentCountByUserAsync(Guid UserID)
        {
            return await _context.Appointments
                .CountAsync(a => a.UserID == UserID && !a.IsDeleted);
        }

        public async Task<Dictionary<string, int>> GetAppointmentStatisticsAsync(Guid UserID, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            var query = _context.Appointments
                .Where(a => a.UserID == UserID && !a.IsDeleted);

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

        public async Task<bool> ChangeAppointmentStatusAsync(Guid AppointmentID, string newStatus)
        {
            var appointment = await GetAppointmentByIdAsync(AppointmentID);
            if (appointment == null) return false;

            appointment.Status = newStatus;
            appointment.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Appointment> RescheduleAppointmentAsync(Guid AppointmentID, DateTimeOffset newStart, DateTimeOffset newEnd)
        {
            var appointment = await GetAppointmentByIdAsync(AppointmentID);
            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {AppointmentID} not found");

            // Validate time conflict
            if (await CheckTimeConflictAsync(appointment.UserID, newStart, newEnd, AppointmentID))
            {
                throw new InvalidOperationException("Time conflict with existing appointment");
            }

            appointment.StartDateTime = newStart;
            appointment.EndDateTime = newEnd;
            appointment.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return appointment;
        }

        public async Task<bool> AddParticipantToAppointmentAsync(Guid AppointmentID, Guid ParticipantID)
        {
            var appointment = await GetAppointmentByIdAsync(AppointmentID);
            if (appointment == null) return false;

            var participant = await _context.Participants.FindAsync(ParticipantID);
            if (participant == null) return false;

            if (!appointment.Participants.Any(p => p.ParticipantID == ParticipantID))
            {
                appointment.Participants.Add(participant);
                appointment.UpdatedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> RemoveParticipantFromAppointmentAsync(Guid AppointmentID, Guid ParticipantID)
        {
            var appointment = await GetAppointmentByIdAsync(AppointmentID);
            if (appointment == null) return false;

            var participant = appointment.Participants.FirstOrDefault(p => p.ParticipantID == ParticipantID);
            if (participant != null)
            {
                appointment.Participants.Remove(participant);
                appointment.UpdatedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<IEnumerable<Appointment>> SearchAppointmentsAsync(Guid UserID, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetUserAppointmentsAsync(UserID);

            return await _context.Appointments
                .Include(a => a.Participants)
                .Where(a => a.UserID == UserID &&
                           !a.IsDeleted &&
                           (a.Title.Contains(searchTerm) ||
                            a.Description.Contains(searchTerm) ||
                            a.Location.Contains(searchTerm) ||
                            a.Notes.Contains(searchTerm)))
                .OrderBy(a => a.StartDateTime)
                .ToListAsync();
        }

        public async Task<bool> CheckTimeConflictAsync(Guid UserID, DateTimeOffset start, DateTimeOffset end, Guid? excludeAppointmentID = null)
        {
            var query = _context.Appointments
                .Where(a => a.UserID == UserID &&
                           !a.IsDeleted &&
                           a.Status != "cancelled" &&
                           ((start >= a.StartDateTime && start < a.EndDateTime) ||
                            (end > a.StartDateTime && end <= a.EndDateTime) ||
                            (start <= a.StartDateTime && end >= a.EndDateTime)));

            if (excludeAppointmentID.HasValue)
                query = query.Where(a => a.AppointmentID != excludeAppointmentID.Value);

            return await query.AnyAsync();
        }
    }
}