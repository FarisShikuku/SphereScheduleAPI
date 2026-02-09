using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface IAppointmentService
    {
        // Basic CRUD
        Task<Appointment> GetAppointmentByIdAsync(Guid appointmentId);
        Task<IEnumerable<Appointment>> GetUserAppointmentsAsync(Guid userId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
        Task<Appointment> CreateAppointmentAsync(Appointment appointment);
        Task<Appointment> UpdateAppointmentAsync(Appointment appointment);
        Task<bool> DeleteAppointmentAsync(Guid appointmentId);

        // Query operations
        Task<IEnumerable<Appointment>> GetAppointmentsByDateRangeAsync(Guid userId, DateTimeOffset startDate, DateTimeOffset endDate);
        Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(Guid userId, int daysAhead = 7);
        Task<IEnumerable<Appointment>> GetAppointmentsByStatusAsync(Guid userId, string status);
        Task<IEnumerable<Appointment>> GetAppointmentsByTypeAsync(Guid userId, string appointmentType);

        // Statistics
        Task<int> GetAppointmentCountByUserAsync(Guid userId);
        Task<Dictionary<string, int>> GetAppointmentStatisticsAsync(Guid userId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);

        // Special operations
        Task<bool> ChangeAppointmentStatusAsync(Guid appointmentId, string newStatus);
        Task<Appointment> RescheduleAppointmentAsync(Guid appointmentId, DateTimeOffset newStart, DateTimeOffset newEnd);
        Task<bool> AddParticipantToAppointmentAsync(Guid appointmentId, Guid participantId);
        Task<bool> RemoveParticipantFromAppointmentAsync(Guid appointmentId, Guid participantId);
        Task<IEnumerable<Appointment>> SearchAppointmentsAsync(Guid userId, string searchTerm);
        Task<bool> CheckTimeConflictAsync(Guid userId, DateTimeOffset start, DateTimeOffset end, Guid? excludeAppointmentId = null);
    }
}