// Application/Services/MeetingService.cs
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
    public class MeetingService : IMeetingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<MeetingService> _logger;

        public MeetingService(ApplicationDbContext context, IMapper mapper, ILogger<MeetingService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<MeetingDto>> GetUserMeetingsAsync(Guid userID, MeetingFilterDto? filter = null)
        {
            var query = _context.Meetings
                .Include(m => m.Organizer)
                .Include(m => m.Task)
                .Include(m => m.Participants)
                .Where(m => !m.IsDeleted && m.OrganizerUserID == userID);

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Status))
                    query = query.Where(m => m.Status == filter.Status);
                if (filter.StartDateFrom.HasValue)
                    query = query.Where(m => m.StartDateTime >= filter.StartDateFrom.Value);
                if (filter.StartDateTo.HasValue)
                    query = query.Where(m => m.StartDateTime <= filter.StartDateTo.Value);
                if (filter.IsRecurring.HasValue)
                    query = query.Where(m => m.IsRecurring == filter.IsRecurring.Value);
                if (!string.IsNullOrEmpty(filter.SearchText))
                    query = query.Where(m => m.Title.Contains(filter.SearchText) || m.Description!.Contains(filter.SearchText));
            }

            query = query.OrderByDescending(m => m.StartDateTime);
            var meetings = await query.ToListAsync();
            return _mapper.Map<IEnumerable<MeetingDto>>(meetings);
        }

        public async Task<MeetingDto?> GetMeetingByIdAsync(Guid meetingID, bool includeParticipants = false)
        {
            var query = _context.Meetings
                .Include(m => m.Organizer)
                .Include(m => m.Task)
                .Where(m => m.MeetingID == meetingID && !m.IsDeleted);

            if (includeParticipants)
                query = query.Include(m => m.Participants).ThenInclude(p => p.User);

            var meeting = await query.FirstOrDefaultAsync();
            return meeting == null ? null : _mapper.Map<MeetingDto>(meeting);
        }

        public async Task<IEnumerable<MeetingDto>> GetMeetingsByTaskAsync(Guid taskID)
        {
            var meetings = await _context.Meetings
                .Include(m => m.Organizer)
                .Include(m => m.Task)
                .Where(m => m.TaskID == taskID && !m.IsDeleted)
                .OrderByDescending(m => m.StartDateTime)
                .ToListAsync();

            return _mapper.Map<IEnumerable<MeetingDto>>(meetings);
        }

        public async Task<MeetingDto> CreateMeetingAsync(Guid organizerUserID, CreateMeetingDto createDto)
        {
            // Verify task exists and belongs to user
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskID == createDto.TaskID && !t.IsDeleted);
            if (task == null)
                throw new KeyNotFoundException($"Task with ID {createDto.TaskID} not found");

            // Check if task already has a meeting
            var existingMeeting = await _context.Meetings.FirstOrDefaultAsync(m => m.TaskID == createDto.TaskID && !m.IsDeleted);
            if (existingMeeting != null)
                throw new InvalidOperationException("Task already has a meeting. One task can have only one meeting.");

            var meeting = _mapper.Map<Meeting>(createDto);
            meeting.OrganizerUserID = organizerUserID;
            meeting.Status = "scheduled";

            // Add participants if provided
            if (createDto.Participants != null && createDto.Participants.Any())
            {
                foreach (var participantDto in createDto.Participants)
                {
                    var participant = _mapper.Map<MeetingParticipant>(participantDto);
                    participant.MeetingID = meeting.MeetingID;
                    meeting.Participants.Add(participant);
                }
            }

            _context.Meetings.Add(meeting);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Meeting created: {MeetingID} for task {TaskID}", meeting.MeetingID, meeting.TaskID);
            return _mapper.Map<MeetingDto>(meeting);
        }

        public async Task<MeetingDto> UpdateMeetingAsync(Guid meetingID, UpdateMeetingDto updateDto)
        {
            var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.MeetingID == meetingID && !m.IsDeleted);
            if (meeting == null)
                throw new KeyNotFoundException($"Meeting with ID {meetingID} not found");

            // Apply updates
            if (updateDto.Title != null) meeting.Title = updateDto.Title;
            if (updateDto.Description != null) meeting.Description = updateDto.Description;
            if (updateDto.StartDateTime.HasValue) meeting.StartDateTime = updateDto.StartDateTime.Value;
            if (updateDto.EndDateTime.HasValue) meeting.EndDateTime = updateDto.EndDateTime.Value;
            if (updateDto.MeetingLink != null) meeting.MeetingLink = updateDto.MeetingLink;
            if (updateDto.MeetingPlatform != null) meeting.MeetingPlatform = updateDto.MeetingPlatform;
            if (updateDto.Status != null) meeting.Status = updateDto.Status;
            if (updateDto.IsRecurring.HasValue) meeting.IsRecurring = updateDto.IsRecurring.Value;
            if (updateDto.RecurrencePattern != null) meeting.RecurrencePattern = updateDto.RecurrencePattern;

            meeting.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Meeting updated: {MeetingID}", meetingID);
            return _mapper.Map<MeetingDto>(meeting);
        }

        public async Task<bool> DeleteMeetingAsync(Guid meetingID)
        {
            var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.MeetingID == meetingID && !m.IsDeleted);
            if (meeting == null) return false;

            meeting.IsDeleted = true;
            meeting.DeletedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Meeting deleted: {MeetingID}", meetingID);
            return true;
        }

        public async Task<IEnumerable<MeetingDto>> GetUpcomingMeetingsAsync(Guid userID, int daysAhead = 7)
        {
            var now = DateTimeOffset.UtcNow;
            var endDate = now.AddDays(daysAhead);

            var meetings = await _context.Meetings
                .Include(m => m.Organizer)
                .Include(m => m.Task)
                .Include(m => m.Participants)
                .Where(m => !m.IsDeleted && m.OrganizerUserID == userID
                         && m.StartDateTime >= now && m.StartDateTime <= endDate
                         && m.Status != "cancelled")
                .OrderBy(m => m.StartDateTime)
                .ToListAsync();

            return _mapper.Map<IEnumerable<MeetingDto>>(meetings);
        }

        public async Task<IEnumerable<MeetingDto>> GetLiveMeetingsAsync(Guid userID)
        {
            var now = DateTimeOffset.UtcNow;

            var meetings = await _context.Meetings
                .Include(m => m.Organizer)
                .Include(m => m.Task)
                .Include(m => m.Participants)
                .Where(m => !m.IsDeleted && m.OrganizerUserID == userID
                         && m.StartDateTime <= now && m.EndDateTime >= now
                         && m.Status == "live")
                .OrderBy(m => m.StartDateTime)
                .ToListAsync();

            return _mapper.Map<IEnumerable<MeetingDto>>(meetings);
        }

        public async Task<IEnumerable<MeetingDto>> GetMeetingsByStatusAsync(Guid userID, string status)
        {
            var meetings = await _context.Meetings
                .Include(m => m.Organizer)
                .Include(m => m.Task)
                .Include(m => m.Participants)
                .Where(m => !m.IsDeleted && m.OrganizerUserID == userID && m.Status == status)
                .OrderByDescending(m => m.StartDateTime)
                .ToListAsync();

            return _mapper.Map<IEnumerable<MeetingDto>>(meetings);
        }

        public async Task<MeetingDto> ChangeMeetingStatusAsync(Guid meetingID, string newStatus)
        {
            var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.MeetingID == meetingID && !m.IsDeleted);
            if (meeting == null)
                throw new KeyNotFoundException($"Meeting with ID {meetingID} not found");

            var validStatuses = new[] { "scheduled", "live", "ended", "cancelled" };
            if (!validStatuses.Contains(newStatus))
                throw new ArgumentException($"Invalid status: {newStatus}. Valid values: {string.Join(", ", validStatuses)}");

            meeting.Status = newStatus;
            meeting.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return _mapper.Map<MeetingDto>(meeting);
        }

        // ─── Participant Management ────────────────────────────────────────────────

        public async Task<MeetingParticipantDto> AddParticipantAsync(Guid meetingID, CreateMeetingParticipantDto createDto)
        {
            var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.MeetingID == meetingID && !m.IsDeleted);
            if (meeting == null)
                throw new KeyNotFoundException($"Meeting with ID {meetingID} not found");

            // Check if participant already exists
            var existingParticipant = await _context.MeetingParticipants
                .FirstOrDefaultAsync(p => p.MeetingID == meetingID && p.Email == createDto.Email);
            if (existingParticipant != null)
                throw new InvalidOperationException($"Participant with email {createDto.Email} already exists in this meeting");

            var participant = _mapper.Map<MeetingParticipant>(createDto);
            participant.MeetingID = meetingID;

            _context.MeetingParticipants.Add(participant);
            await _context.SaveChangesAsync();

            return _mapper.Map<MeetingParticipantDto>(participant);
        }

        public async Task<bool> RemoveParticipantAsync(Guid participantID)
        {
            var participant = await _context.MeetingParticipants.FirstOrDefaultAsync(p => p.ParticipantID == participantID);
            if (participant == null) return false;

            _context.MeetingParticipants.Remove(participant);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateParticipantStatusAsync(Guid participantID, string status)
        {
            var participant = await _context.MeetingParticipants.FirstOrDefaultAsync(p => p.ParticipantID == participantID);
            if (participant == null) return false;

            participant.InvitationStatus = status;
            participant.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<MeetingParticipantDto>> GetParticipantsAsync(Guid meetingID)
        {
            var participants = await _context.MeetingParticipants
                .Include(p => p.User)
                .Where(p => p.MeetingID == meetingID)
                .ToListAsync();

            return _mapper.Map<IEnumerable<MeetingParticipantDto>>(participants);
        }

        // ─── Statistics ────────────────────────────────────────────────────────────

        public async Task<MeetingStatisticsDto> GetMeetingStatisticsAsync(Guid userID, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            var query = _context.Meetings.Where(m => !m.IsDeleted && m.OrganizerUserID == userID);

            if (startDate.HasValue)
                query = query.Where(m => m.StartDateTime >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(m => m.StartDateTime <= endDate.Value);

            var meetings = await query.Include(m => m.Participants).ToListAsync();

            return new MeetingStatisticsDto
            {
                TotalMeetings = meetings.Count,
                ScheduledMeetings = meetings.Count(m => m.Status == "scheduled"),
                LiveMeetings = meetings.Count(m => m.Status == "live"),
                EndedMeetings = meetings.Count(m => m.Status == "ended"),
                CancelledMeetings = meetings.Count(m => m.Status == "cancelled"),
                RecurringMeetings = meetings.Count(m => m.IsRecurring),
                TotalParticipants = meetings.Sum(m => m.Participants.Count),
                AverageParticipantsPerMeeting = meetings.Any() ? meetings.Average(m => m.Participants.Count) : 0,
                MeetingsByPlatform = meetings.Where(m => !string.IsNullOrEmpty(m.MeetingPlatform))
                    .GroupBy(m => m.MeetingPlatform!)
                    .ToDictionary(g => g.Key, g => g.Count()),
                MeetingsByDay = meetings.GroupBy(m => m.StartDateTime.DayOfWeek.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }
    }
}