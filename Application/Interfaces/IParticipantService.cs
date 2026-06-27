using SphereScheduleAPI.Application.DTOs;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface IParticipantService
    {
        Task<ParticipantDto> CreateParticipantAsync(Guid AppointmentID, CreateParticipantDto createDto);
        Task<IEnumerable<ParticipantDto>> CreateParticipantsBulkAsync(BulkAddParticipantsDto bulkDto);
        Task<ParticipantDto> GetParticipantByIdAsync(Guid ParticipantID, bool includeDetails = false);
        Task<ParticipantDto> GetParticipantByEmailAsync(Guid AppointmentID, string email);
        Task<IEnumerable<ParticipantDto>> GetParticipantsByAppointmentIDAsync(Guid AppointmentID, ParticipantFilterDto filterDto);
        Task<IEnumerable<ParticipantDto>> GetParticipantsByFilterAsync(ParticipantFilterDto filterDto);
        Task<IEnumerable<ParticipantDto>> GetInvitationsByUserAsync(Guid UserID, ParticipantFilterDto filterDto);
        Task<ParticipantDto> UpdateParticipantAsync(Guid ParticipantID, UpdateParticipantDto updateDto);
        Task<ParticipantDto> UpdateParticipantStatusAsync(Guid ParticipantID, UpdateParticipantStatusDto statusDto);
        Task<bool> DeleteParticipantAsync(Guid ParticipantID);
        Task<bool> DeleteParticipantsByAppointmentAsync(Guid AppointmentID);
        Task<int> GetParticipantCountByAppointmentAsync(Guid AppointmentID, string? status = null);
        Task<ParticipantStatisticsDto> GetParticipantStatisticsAsync(Guid AppointmentID);
        Task<bool> ResendInvitationAsync(Guid ParticipantID);
        Task<bool> CheckIfUserIsParticipantAsync(Guid AppointmentID, Guid UserID);
        Task<bool> CheckIfEmailIsParticipantAsync(Guid AppointmentID, string email);
        Task<IEnumerable<ParticipantDto>> SearchParticipantsAsync(string searchTerm, Guid? AppointmentID = null);
    }
}