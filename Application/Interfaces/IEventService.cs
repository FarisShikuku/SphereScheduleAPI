// Application/Interfaces/IEventService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SphereScheduleAPI.Application.DTOs;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface IEventService
    {
        // Event CRUD
        Task<IEnumerable<EventDto>> GetUserEventsAsync(Guid userID, EventFilterDto? filter = null);
        Task<EventDto?> GetEventByIdAsync(Guid eventID, bool includeParticipants = false);
        Task<EventDto> CreateEventAsync(Guid userID, CreateEventDto createDto);
        Task<EventDto> UpdateEventAsync(Guid eventID, UpdateEventDto updateDto);
        Task<bool> DeleteEventAsync(Guid eventID);
        Task<EventDto> ChangeEventStatusAsync(Guid eventID, string newStatus);

        // Event Participants
        Task<EventParticipantDto> AddParticipantAsync(Guid eventID, CreateEventParticipantDto createDto);
        Task<bool> RemoveParticipantAsync(Guid participantID);
        Task<bool> UpdateParticipantStatusAsync(Guid participantID, string status);
        Task<IEnumerable<EventParticipantDto>> GetParticipantsAsync(Guid eventID);

        // Event Categories
        Task<IEnumerable<EventCategoryDto>> GetCategoriesAsync(Guid? userID = null);
        Task<EventCategoryDto> CreateCategoryAsync(Guid? userID, CreateEventCategoryDto createDto);
        Task<bool> DeleteCategoryAsync(Guid categoryID);
        Task<IEnumerable<EventCategoryDto>> GetSystemCategoriesAsync();
        Task<IEnumerable<EventCategoryDto>> GetUserCustomCategoriesAsync(Guid userID);

        // Statistics
        Task<EventStatisticsDto> GetEventStatisticsAsync(Guid userID, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);

        // Search
        Task<IEnumerable<EventDto>> SearchEventsAsync(Guid userID, string searchTerm);
    }
}