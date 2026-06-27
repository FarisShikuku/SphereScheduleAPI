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
        Task<Appointment> GetAppointmentByIdAsync(Guid AppointmentID);
        Task<IEnumerable<Appointment>> GetUserAppointmentsAsync(Guid UserID, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
        Task<Appointment> CreateAppointmentAsync(Appointment appointment);
        Task<Appointment> UpdateAppointmentAsync(Appointment appointment);
        Task<bool> DeleteAppointmentAsync(Guid AppointmentID);

        // Query operations
        Task<IEnumerable<Appointment>> GetAppointmentsByDateRangeAsync(Guid UserID, DateTimeOffset startDate, DateTimeOffset endDate);
        Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(Guid UserID, int daysAhead = 7);
        Task<IEnumerable<Appointment>> GetAppointmentsByStatusAsync(Guid UserID, string status);
        Task<IEnumerable<Appointment>> GetAppointmentsByTypeAsync(Guid UserID, string appointmentType);

        // Statistics
        Task<int> GetAppointmentCountByUserAsync(Guid UserID);
        Task<Dictionary<string, int>> GetAppointmentStatisticsAsync(Guid UserID, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);

        // Special operations
        Task<bool> ChangeAppointmentStatusAsync(Guid AppointmentID, string newStatus);
        Task<Appointment> RescheduleAppointmentAsync(Guid AppointmentID, DateTimeOffset newStart, DateTimeOffset newEnd);
        Task<bool> AddParticipantToAppointmentAsync(Guid AppointmentID, Guid ParticipantID);
        Task<bool> RemoveParticipantFromAppointmentAsync(Guid AppointmentID, Guid ParticipantID);
        Task<IEnumerable<Appointment>> SearchAppointmentsAsync(Guid UserID, string searchTerm);
        Task<bool> CheckTimeConflictAsync(Guid UserID, DateTimeOffset start, DateTimeOffset end, Guid? excludeAppointmentID = null);
    }
}