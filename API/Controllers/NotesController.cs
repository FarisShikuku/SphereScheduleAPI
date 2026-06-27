// API/Controllers/NotesController.cs
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;

namespace SphereScheduleAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotesController : ControllerBase
    {
        private readonly INoteService _noteService;

        public NotesController(INoteService noteService)
        {
            _noteService = noteService;
        }

        private Guid GetCurrentUserID()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user ID in token");
            return userId;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<NoteDto>), 200)]
        public async Task<IActionResult> GetNotes([FromQuery] NoteFilterDto? filter = null)
        {
            var userId = GetCurrentUserID();
            var notes = await _noteService.GetUserNotesAsync(userId, filter);
            return Ok(notes);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(NoteDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetNote(Guid id)
        {
            var note = await _noteService.GetNoteByIdAsync(id);
            if (note == null)
                return NotFound(new { message = $"Note with ID {id} not found" });

            return Ok(note);
        }

        [HttpPost]
        [ProducesResponseType(typeof(NoteDto), 201)]
        public async Task<IActionResult> CreateNote([FromBody] CreateNoteDto createDto)
        {
            var userId = GetCurrentUserID();
            var note = await _noteService.CreateNoteAsync(userId, createDto);
            return CreatedAtAction(nameof(GetNote), new { id = note.NoteID }, note);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(NoteDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateNote(Guid id, [FromBody] UpdateNoteDto updateDto)
        {
            try
            {
                var note = await _noteService.UpdateNoteAsync(id, updateDto);
                return Ok(note);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteNote(Guid id)
        {
            var success = await _noteService.DeleteNoteAsync(id);
            if (!success)
                return NotFound(new { message = $"Note with ID {id} not found" });

            return NoContent();
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<NoteDto>), 200)]
        public async Task<IActionResult> SearchNotes([FromQuery] string term)
        {
            var userId = GetCurrentUserID();
            var notes = await _noteService.SearchNotesAsync(userId, term);
            return Ok(notes);
        }
    }
}