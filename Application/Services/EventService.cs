// Application/Services/EventService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Domain.Entities;
using SphereScheduleAPI.Infrastructure.Data;

namespace SphereScheduleAPI.Application.Services
{
    public class EventService : IEventService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<EventService> _logger;

        public EventService(ApplicationDbContext context, IMapper mapper, ILogger<EventService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════
        // EVENT CRUD
        // ═══════════════════════════════════════════════════

        public async Task<IEnumerable<EventDto>> GetUserEventsAsync(Guid userID, EventFilterDto? filter = null)
        {
            var query = _context.Events
                .Include(e => e.User)
                .Include(e => e.Category)
                .Include(e => e.Participants)
                .Include(e => e.Reminders)
                .Where(e => !e.IsDeleted && e.UserID == userID);

            if (filter != null)
            {
                if (filter.CategoryID.HasValue)
                    query = query.Where(e => e.CategoryID == filter.CategoryID.Value);
                if (!string.IsNullOrEmpty(filter.Status))
                    query = query.Where(e => e.Status == filter.Status);
                if (filter.StartDateFrom.HasValue)
                    query = query.Where(e => e.StartDateTime >= filter.StartDateFrom.Value);
                if (filter.StartDateTo.HasValue)
                    query = query.Where(e => e.StartDateTime <= filter.StartDateTo.Value);
                if (!string.IsNullOrEmpty(filter.SearchText))
                    query = query.Where(e => e.Name.Contains(filter.SearchText));
            }

            query = query.OrderByDescending(e => e.StartDateTime);
            var events = await query.ToListAsync();
            return _mapper.Map<IEnumerable<EventDto>>(events);
        }

        public async Task<EventDto?> GetEventByIdAsync(Guid eventID, bool includeParticipants = false)
        {
            var query = _context.Events
                .Include(e => e.User)
                .Include(e => e.Category)
                .Where(e => e.EventID == eventID && !e.IsDeleted);

            if (includeParticipants)
                query = query.Include(e => e.Participants).ThenInclude(p => p.User);

            var eventEntity = await query.FirstOrDefaultAsync();
            return eventEntity == null ? null : _mapper.Map<EventDto>(eventEntity);
        }

        public async Task<EventDto> CreateEventAsync(Guid userID, CreateEventDto createDto)
        {
            var eventEntity = _mapper.Map<Event>(createDto);
            eventEntity.UserID = userID;
            eventEntity.Status = "planned";

            // Optionally create a linked task
            if (createDto.CreateLinkedTask)
            {
                var task = new TaskEntity
                {
                    UserID = userID,
                    Title = $"Event: {createDto.Name}",
                    Description = createDto.PlanningNotes,
                    TaskType = "event",
                    StartDate = createDto.StartDateTime?.Date,
                    StartTime = createDto.StartDateTime?.TimeOfDay,
                    EndDate = createDto.EndDateTime?.Date,
                    EndTime = createDto.EndDateTime?.TimeOfDay,
                    Status = createDto.StartDateTime.HasValue && createDto.StartDateTime.Value <= DateTimeOffset.UtcNow ? "in_progress" : "pending"
                };

                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();
                eventEntity.TaskID = task.TaskID;
            }

            // Add participants if any
            if (createDto.Participants != null && createDto.Participants.Any())
            {
                foreach (var participantDto in createDto.Participants)
                {
                    var participant = _mapper.Map<EventParticipant>(participantDto);
                    participant.EventID = eventEntity.EventID;
                    eventEntity.Participants.Add(participant);
                }
            }

            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Event created: {EventID}", eventEntity.EventID);
            return _mapper.Map<EventDto>(eventEntity);
        }

        public async Task<EventDto> UpdateEventAsync(Guid eventID, UpdateEventDto updateDto)
        {
            var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.EventID == eventID && !e.IsDeleted);
            if (eventEntity == null)
                throw new KeyNotFoundException($"Event with ID {eventID} not found");

            if (updateDto.Name != null) eventEntity.Name = updateDto.Name;
            if (updateDto.CategoryID.HasValue) eventEntity.CategoryID = updateDto.CategoryID;
            if (updateDto.Format != null) eventEntity.Format = updateDto.Format;
            if (updateDto.PlanningNotes != null) eventEntity.PlanningNotes = updateDto.PlanningNotes;
            if (updateDto.StartDateTime.HasValue) eventEntity.StartDateTime = updateDto.StartDateTime;
            if (updateDto.EndDateTime.HasValue) eventEntity.EndDateTime = updateDto.EndDateTime;
            if (updateDto.Status != null) eventEntity.Status = updateDto.Status;
            if (updateDto.IsRecurring.HasValue) eventEntity.IsRecurring = updateDto.IsRecurring.Value;
            if (updateDto.RecurrencePattern != null) eventEntity.RecurrencePattern = updateDto.RecurrencePattern;

            eventEntity.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return _mapper.Map<EventDto>(eventEntity);
        }

        public async Task<bool> DeleteEventAsync(Guid eventID)
        {
            var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.EventID == eventID && !e.IsDeleted);
            if (eventEntity == null) return false;

            eventEntity.IsDeleted = true;
            eventEntity.DeletedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<EventDto> ChangeEventStatusAsync(Guid eventID, string newStatus)
        {
            var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.EventID == eventID && !e.IsDeleted);
            if (eventEntity == null)
                throw new KeyNotFoundException($"Event with ID {eventID} not found");

            var validStatuses = new[] { "planned", "ongoing", "completed", "cancelled" };
            if (!validStatuses.Contains(newStatus))
                throw new ArgumentException($"Invalid status: {newStatus}");

            eventEntity.Status = newStatus;
            eventEntity.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return _mapper.Map<EventDto>(eventEntity);
        }

        // ═══════════════════════════════════════════════════
        // EVENT PARTICIPANTS
        // ═══════════════════════════════════════════════════

        public async Task<EventParticipantDto> AddParticipantAsync(Guid eventID, CreateEventParticipantDto createDto)
        {
            var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.EventID == eventID && !e.IsDeleted);
            if (eventEntity == null)
                throw new KeyNotFoundException($"Event with ID {eventID} not found");

            var existing = await _context.EventParticipants
                .FirstOrDefaultAsync(p => p.EventID == eventID && p.Email == createDto.Email);
            if (existing != null)
                throw new InvalidOperationException($"Participant with email {createDto.Email} already exists");

            var participant = _mapper.Map<EventParticipant>(createDto);
            participant.EventID = eventID;

            _context.EventParticipants.Add(participant);
            await _context.SaveChangesAsync();

            return _mapper.Map<EventParticipantDto>(participant);
        }

        public async Task<bool> RemoveParticipantAsync(Guid participantID)
        {
            var participant = await _context.EventParticipants.FirstOrDefaultAsync(p => p.ParticipantID == participantID);
            if (participant == null) return false;

            _context.EventParticipants.Remove(participant);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateParticipantStatusAsync(Guid participantID, string status)
        {
            var participant = await _context.EventParticipants.FirstOrDefaultAsync(p => p.ParticipantID == participantID);
            if (participant == null) return false;

            participant.InvitationStatus = status;
            participant.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<EventParticipantDto>> GetParticipantsAsync(Guid eventID)
        {
            var participants = await _context.EventParticipants
                .Include(p => p.User)
                .Where(p => p.EventID == eventID)
                .ToListAsync();

            return _mapper.Map<IEnumerable<EventParticipantDto>>(participants);
        }

        // ═══════════════════════════════════════════════════
        // EVENT CATEGORIES
        // ═══════════════════════════════════════════════════

        public async Task<IEnumerable<EventCategoryDto>> GetCategoriesAsync(Guid? userID = null)
        {
            var query = _context.EventCategories
                .Include(c => c.Events)
                .Where(c => !c.IsDeleted);

            if (userID.HasValue)
                query = query.Where(c => c.UserID == null || c.UserID == userID.Value); // System + user's custom
            else
                query = query.Where(c => c.UserID == null); // System only

            var categories = await query.OrderBy(c => c.CategoryName).ToListAsync();
            return _mapper.Map<IEnumerable<EventCategoryDto>>(categories);
        }

        public async Task<EventCategoryDto> CreateCategoryAsync(Guid? userID, CreateEventCategoryDto createDto)
        {
            var category = _mapper.Map<EventCategory>(createDto);
            category.UserID = userID;
            category.IsSystem = !userID.HasValue;

            _context.EventCategories.Add(category);
            await _context.SaveChangesAsync();

            return _mapper.Map<EventCategoryDto>(category);
        }

        public async Task<bool> DeleteCategoryAsync(Guid categoryID)
        {
            var category = await _context.EventCategories.FirstOrDefaultAsync(c => c.CategoryID == categoryID && !c.IsDeleted);
            if (category == null || category.IsSystem) return false; // Cannot delete system categories

            category.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<EventCategoryDto>> GetSystemCategoriesAsync()
        {
            var categories = await _context.EventCategories
                .Include(c => c.Events)
                .Where(c => c.IsSystem && !c.IsDeleted)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return _mapper.Map<IEnumerable<EventCategoryDto>>(categories);
        }

        public async Task<IEnumerable<EventCategoryDto>> GetUserCustomCategoriesAsync(Guid userID)
        {
            var categories = await _context.EventCategories
                .Include(c => c.Events)
                .Where(c => c.UserID == userID && !c.IsSystem && !c.IsDeleted)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return _mapper.Map<IEnumerable<EventCategoryDto>>(categories);
        }

        // ═══════════════════════════════════════════════════
        // STATISTICS & SEARCH
        // ═══════════════════════════════════════════════════

        public async Task<EventStatisticsDto> GetEventStatisticsAsync(Guid userID, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            var query = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Participants)
                .Where(e => !e.IsDeleted && e.UserID == userID);

            if (startDate.HasValue) query = query.Where(e => e.StartDateTime >= startDate.Value);
            if (endDate.HasValue) query = query.Where(e => e.StartDateTime <= endDate.Value);

            var events = await query.ToListAsync();

            return new EventStatisticsDto
            {
                TotalEvents = events.Count,
                PlannedEvents = events.Count(e => e.Status == "planned"),
                OngoingEvents = events.Count(e => e.Status == "ongoing"),
                CompletedEvents = events.Count(e => e.Status == "completed"),
                CancelledEvents = events.Count(e => e.Status == "cancelled"),
                TotalParticipants = events.Sum(e => e.Participants.Count),
                EventsByCategory = events.Where(e => e.Category != null)
                    .GroupBy(e => e.Category!.CategoryName)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EventsByStatus = events.GroupBy(e => e.Status)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        public async Task<IEnumerable<EventDto>> SearchEventsAsync(Guid userID, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<EventDto>();

            var events = await _context.Events
                .Include(e => e.User)
                .Include(e => e.Category)
                .Where(e => !e.IsDeleted && e.UserID == userID
                         && (e.Name.Contains(searchTerm) || e.PlanningNotes!.Contains(searchTerm)))
                .OrderByDescending(e => e.StartDateTime)
                .Take(50)
                .ToListAsync();

            return _mapper.Map<IEnumerable<EventDto>>(events);
        }
    }
}