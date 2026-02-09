using SphereScheduleAPI.Application.DTOs;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface IParticipantService
    {
        Task<ParticipantDto> CreateParticipantAsync(Guid appointmentId, CreateParticipantDto createDto);
        Task<IEnumerable<ParticipantDto>> CreateParticipantsBulkAsync(BulkAddParticipantsDto bulkDto);
        Task<ParticipantDto> GetParticipantByIdAsync(Guid participantId, bool includeDetails = false);
        Task<ParticipantDto> GetParticipantByEmailAsync(Guid appointmentId, string email);
        Task<IEnumerable<ParticipantDto>> GetParticipantsByAppointmentIdAsync(Guid appointmentId, ParticipantFilterDto filterDto);
        Task<IEnumerable<ParticipantDto>> GetParticipantsByFilterAsync(ParticipantFilterDto filterDto);
        Task<IEnumerable<ParticipantDto>> GetInvitationsByUserAsync(Guid userId, ParticipantFilterDto filterDto);
        Task<ParticipantDto> UpdateParticipantAsync(Guid participantId, UpdateParticipantDto updateDto);
        Task<ParticipantDto> UpdateParticipantStatusAsync(Guid participantId, UpdateParticipantStatusDto statusDto);
        Task<bool> DeleteParticipantAsync(Guid participantId);
        Task<bool> DeleteParticipantsByAppointmentAsync(Guid appointmentId);
        Task<int> GetParticipantCountByAppointmentAsync(Guid appointmentId, string? status = null);
        Task<ParticipantStatisticsDto> GetParticipantStatisticsAsync(Guid appointmentId);
        Task<bool> ResendInvitationAsync(Guid participantId);
        Task<bool> CheckIfUserIsParticipantAsync(Guid appointmentId, Guid userId);
        Task<bool> CheckIfEmailIsParticipantAsync(Guid appointmentId, string email);
        Task<IEnumerable<ParticipantDto>> SearchParticipantsAsync(string searchTerm, Guid? appointmentId = null);
    }
}