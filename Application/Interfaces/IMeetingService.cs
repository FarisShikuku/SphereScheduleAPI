// Application/Interfaces/IMeetingService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SphereScheduleAPI.Application.DTOs;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface IMeetingService
    {
        Task<IEnumerable<MeetingDto>> GetUserMeetingsAsync(Guid userID, MeetingFilterDto? filter = null);
        Task<MeetingDto?> GetMeetingByIdAsync(Guid meetingID, bool includeParticipants = false);
        Task<IEnumerable<MeetingDto>> GetMeetingsByTaskAsync(Guid taskID);
        Task<MeetingDto> CreateMeetingAsync(Guid organizerUserID, CreateMeetingDto createDto);
        Task<MeetingDto> UpdateMeetingAsync(Guid meetingID, UpdateMeetingDto updateDto);
        Task<bool> DeleteMeetingAsync(Guid meetingID);
        Task<IEnumerable<MeetingDto>> GetUpcomingMeetingsAsync(Guid userID, int daysAhead = 7);
        Task<IEnumerable<MeetingDto>> GetLiveMeetingsAsync(Guid userID);
        Task<IEnumerable<MeetingDto>> GetMeetingsByStatusAsync(Guid userID, string status);
        Task<MeetingDto> ChangeMeetingStatusAsync(Guid meetingID, string newStatus);

        // Participant management
        Task<MeetingParticipantDto> AddParticipantAsync(Guid meetingID, CreateMeetingParticipantDto createDto);
        Task<bool> RemoveParticipantAsync(Guid participantID);
        Task<bool> UpdateParticipantStatusAsync(Guid participantID, string status);
        Task<IEnumerable<MeetingParticipantDto>> GetParticipantsAsync(Guid meetingID);

        // Statistics
        Task<MeetingStatisticsDto> GetMeetingStatisticsAsync(Guid userID, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
    }
}