// Application/Services/NoteService.cs
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
    public class NoteService : INoteService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<NoteService> _logger;

        public NoteService(ApplicationDbContext context, IMapper mapper, ILogger<NoteService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<NoteDto>> GetUserNotesAsync(Guid userID, NoteFilterDto? filter = null)
        {
            var query = _context.Notes
                .Include(n => n.Task)
                .Include(n => n.Event)
                .Include(n => n.Appointment)
                .Include(n => n.Meeting)
                .Where(n => !n.IsDeleted && n.UserID == userID);

            if (filter != null)
            {
                if (filter.TaskID.HasValue)
                    query = query.Where(n => n.TaskID == filter.TaskID.Value);
                if (filter.EventID.HasValue)
                    query = query.Where(n => n.EventID == filter.EventID.Value);
                if (filter.AppointmentID.HasValue)
                    query = query.Where(n => n.AppointmentID == filter.AppointmentID.Value);
                if (filter.MeetingID.HasValue)
                    query = query.Where(n => n.MeetingID == filter.MeetingID.Value);
                if (!string.IsNullOrEmpty(filter.SearchText))
                    query = query.Where(n => n.Title!.Contains(filter.SearchText) || n.Content.Contains(filter.SearchText));

                // Apply sorting
                query = filter.SortDescending
                    ? query.OrderByDescending(n => EF.Property<object>(n, filter.SortBy ?? "UpdatedAt"))
                    : query.OrderBy(n => EF.Property<object>(n, filter.SortBy ?? "UpdatedAt"));

                // Apply paging
                query = query.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize);
            }
            else
            {
                query = query.OrderByDescending(n => n.UpdatedAt);
            }

            var notes = await query.ToListAsync();

            // Set linked entity titles
            var noteDtos = _mapper.Map<IEnumerable<NoteDto>>(notes).ToList();
            var noteList = notes.ToList();
            for (int i = 0; i < noteDtos.Count; i++)
            {
                noteDtos[i].LinkedEntityTitle = GetLinkedEntityTitle(noteList[i]);
            }

            return noteDtos;
        }

        public async Task<NoteDto?> GetNoteByIdAsync(Guid noteID)
        {
            var note = await _context.Notes
                .Include(n => n.Task)
                .Include(n => n.Event)
                .Include(n => n.Appointment)
                .Include(n => n.Meeting)
                .FirstOrDefaultAsync(n => n.NoteID == noteID && !n.IsDeleted);

            if (note == null) return null;

            var dto = _mapper.Map<NoteDto>(note);
            dto.LinkedEntityTitle = GetLinkedEntityTitle(note);
            return dto;
        }

        public async Task<NoteDto> CreateNoteAsync(Guid userID, CreateNoteDto createDto)
        {
            var note = _mapper.Map<Note>(createDto);
            note.UserID = userID;
            note.CreatedAt = DateTimeOffset.UtcNow;
            note.UpdatedAt = DateTimeOffset.UtcNow;

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Note created: {NoteID} by user {UserID}", note.NoteID, userID);

            // Reload with related entities for full DTO
            var created = await _context.Notes
                .Include(n => n.Task)
                .Include(n => n.Event)
                .Include(n => n.Appointment)
                .Include(n => n.Meeting)
                .FirstAsync(n => n.NoteID == note.NoteID);

            var dto = _mapper.Map<NoteDto>(created);
            dto.LinkedEntityTitle = GetLinkedEntityTitle(created);
            return dto;
        }

        public async Task<NoteDto> UpdateNoteAsync(Guid noteID, UpdateNoteDto updateDto)
        {
            var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteID == noteID && !n.IsDeleted);
            if (note == null)
                throw new KeyNotFoundException($"Note with ID {noteID} not found");

            if (updateDto.Title != null)
                note.Title = updateDto.Title;
            if (updateDto.Content != null)
                note.Content = updateDto.Content;

            note.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Note updated: {NoteID}", noteID);

            // Reload with related entities
            var updated = await _context.Notes
                .Include(n => n.Task)
                .Include(n => n.Event)
                .Include(n => n.Appointment)
                .Include(n => n.Meeting)
                .FirstAsync(n => n.NoteID == note.NoteID);

            var dto = _mapper.Map<NoteDto>(updated);
            dto.LinkedEntityTitle = GetLinkedEntityTitle(updated);
            return dto;
        }

        public async Task<bool> DeleteNoteAsync(Guid noteID)
        {
            var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteID == noteID && !n.IsDeleted);
            if (note == null) return false;

            note.IsDeleted = true;
            note.DeletedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Note deleted: {NoteID}", noteID);
            return true;
        }

        public async Task<IEnumerable<NoteDto>> GetNotesByTaskAsync(Guid taskID)
        {
            var notes = await _context.Notes
                .Include(n => n.Task)
                .Where(n => !n.IsDeleted && n.TaskID == taskID)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<NoteDto>>(notes).ToList();
            var noteList = notes.ToList();
            for (int i = 0; i < dtos.Count; i++)
            {
                dtos[i].LinkedEntityTitle = GetLinkedEntityTitle(noteList[i]);
            }
            return dtos;
        }

        public async Task<IEnumerable<NoteDto>> GetNotesByEventAsync(Guid eventID)
        {
            var notes = await _context.Notes
                .Include(n => n.Event)
                .Where(n => !n.IsDeleted && n.EventID == eventID)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<NoteDto>>(notes).ToList();
            var noteList = notes.ToList();
            for (int i = 0; i < dtos.Count; i++)
            {
                dtos[i].LinkedEntityTitle = GetLinkedEntityTitle(noteList[i]);
            }
            return dtos;
        }

        public async Task<IEnumerable<NoteDto>> GetNotesByAppointmentAsync(Guid appointmentID)
        {
            var notes = await _context.Notes
                .Include(n => n.Appointment)
                .Where(n => !n.IsDeleted && n.AppointmentID == appointmentID)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<NoteDto>>(notes).ToList();
            var noteList = notes.ToList();
            for (int i = 0; i < dtos.Count; i++)
            {
                dtos[i].LinkedEntityTitle = GetLinkedEntityTitle(noteList[i]);
            }
            return dtos;
        }

        public async Task<IEnumerable<NoteDto>> GetNotesByMeetingAsync(Guid meetingID)
        {
            var notes = await _context.Notes
                .Include(n => n.Meeting)
                .Where(n => !n.IsDeleted && n.MeetingID == meetingID)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<NoteDto>>(notes).ToList();
            var noteList = notes.ToList();
            for (int i = 0; i < dtos.Count; i++)
            {
                dtos[i].LinkedEntityTitle = GetLinkedEntityTitle(noteList[i]);
            }
            return dtos;
        }

        public async Task<IEnumerable<NoteDto>> SearchNotesAsync(Guid userID, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<NoteDto>();

            var notes = await _context.Notes
                .Include(n => n.Task)
                .Include(n => n.Event)
                .Include(n => n.Appointment)
                .Include(n => n.Meeting)
                .Where(n => !n.IsDeleted && n.UserID == userID
                         && (n.Title!.Contains(searchTerm) || n.Content.Contains(searchTerm)))
                .OrderByDescending(n => n.UpdatedAt)
                .Take(50)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<NoteDto>>(notes).ToList();
            var noteList = notes.ToList();
            for (int i = 0; i < dtos.Count; i++)
            {
                dtos[i].LinkedEntityTitle = GetLinkedEntityTitle(noteList[i]);
            }
            return dtos;
        }

        /// <summary>
        /// Gets the title of the linked entity for display purposes.
        /// </summary>
        private static string? GetLinkedEntityTitle(Note note)
        {
            if (note.Task != null) return note.Task.Title;
            if (note.Event != null) return note.Event.Name;
            if (note.Appointment != null) return note.Appointment.Title;
            if (note.Meeting != null) return note.Meeting.Title;
            return null;
        }
    }
}