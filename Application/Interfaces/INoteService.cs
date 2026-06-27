// Application/Interfaces/INoteService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SphereScheduleAPI.Application.DTOs;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface INoteService
    {
        Task<IEnumerable<NoteDto>> GetUserNotesAsync(Guid userID, NoteFilterDto? filter = null);
        Task<NoteDto?> GetNoteByIdAsync(Guid noteID);
        Task<NoteDto> CreateNoteAsync(Guid userID, CreateNoteDto createDto);
        Task<NoteDto> UpdateNoteAsync(Guid noteID, UpdateNoteDto updateDto);
        Task<bool> DeleteNoteAsync(Guid noteID);

        // Entity-specific notes
        Task<IEnumerable<NoteDto>> GetNotesByTaskAsync(Guid taskID);
        Task<IEnumerable<NoteDto>> GetNotesByEventAsync(Guid eventID);
        Task<IEnumerable<NoteDto>> GetNotesByAppointmentAsync(Guid appointmentID);
        Task<IEnumerable<NoteDto>> GetNotesByMeetingAsync(Guid meetingID);

        // Search
        Task<IEnumerable<NoteDto>> SearchNotesAsync(Guid userID, string searchTerm);
    }
}