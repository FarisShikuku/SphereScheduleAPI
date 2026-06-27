// API/Controllers/EventsController.cs
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;

namespace SphereScheduleAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ILogger<EventsController> _logger;

        public EventsController(IEventService eventService, ILogger<EventsController> logger)
        {
            _eventService = eventService;
            _logger = logger;
        }

        private Guid GetCurrentUserID()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user ID in token");
            return userId;
        }

        // ═══════════════════════════════════════════════════
        // EVENT CRUD
        // ═══════════════════════════════════════════════════

        // GET: api/events
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EventDto>), 200)]
        public async Task<IActionResult> GetEvents([FromQuery] EventFilterDto? filter = null)
        {
            var userId = GetCurrentUserID();
            var events = await _eventService.GetUserEventsAsync(userId, filter);
            return Ok(events);
        }

        // GET: api/events/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(EventDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetEvent(Guid id, [FromQuery] bool includeParticipants = false)
        {
            var eventEntity = await _eventService.GetEventByIdAsync(id, includeParticipants);
            if (eventEntity == null)
                return NotFound(new { message = $"Event with ID {id} not found" });

            return Ok(eventEntity);
        }

        // POST: api/events
        [HttpPost]
        [ProducesResponseType(typeof(EventDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto createDto)
        {
            var userId = GetCurrentUserID();
            var eventEntity = await _eventService.CreateEventAsync(userId, createDto);
            return CreatedAtAction(nameof(GetEvent), new { id = eventEntity.EventID }, eventEntity);
        }

        // PUT: api/events/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(EventDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventDto updateDto)
        {
            try
            {
                var eventEntity = await _eventService.UpdateEventAsync(id, updateDto);
                return Ok(eventEntity);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // DELETE: api/events/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            var success = await _eventService.DeleteEventAsync(id);
            if (!success)
                return NotFound(new { message = $"Event with ID {id} not found" });

            return NoContent();
        }

        // POST: api/events/{id}/status
        [HttpPost("{id}/status")]
        [ProducesResponseType(typeof(EventDto), 200)]
        public async Task<IActionResult> ChangeEventStatus(Guid id, [FromBody] EventStatusDto statusDto)
        {
            try
            {
                var eventEntity = await _eventService.ChangeEventStatusAsync(id, statusDto.NewStatus);
                return Ok(eventEntity);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════
        // EVENT PARTICIPANTS
        // ═══════════════════════════════════════════════════

        // GET: api/events/{id}/participants
        [HttpGet("{id}/participants")]
        [ProducesResponseType(typeof(IEnumerable<EventParticipantDto>), 200)]
        public async Task<IActionResult> GetParticipants(Guid id)
        {
            var participants = await _eventService.GetParticipantsAsync(id);
            return Ok(participants);
        }

        // POST: api/events/{id}/participants
        [HttpPost("{id}/participants")]
        [ProducesResponseType(typeof(EventParticipantDto), 201)]
        public async Task<IActionResult> AddParticipant(Guid id, [FromBody] CreateEventParticipantDto createDto)
        {
            try
            {
                var participant = await _eventService.AddParticipantAsync(id, createDto);
                return CreatedAtAction(nameof(GetParticipants), new { id }, participant);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // DELETE: api/events/participants/{participantId}
        [HttpDelete("participants/{participantId}")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> RemoveParticipant(Guid participantId)
        {
            var success = await _eventService.RemoveParticipantAsync(participantId);
            if (!success)
                return NotFound(new { message = "Participant not found" });

            return NoContent();
        }

        // ═══════════════════════════════════════════════════
        // EVENT CATEGORIES
        // ═══════════════════════════════════════════════════

        // GET: api/events/categories
        [HttpGet("categories")]
        [ProducesResponseType(typeof(IEnumerable<EventCategoryDto>), 200)]
        public async Task<IActionResult> GetCategories()
        {
            var userId = GetCurrentUserID();
            var categories = await _eventService.GetCategoriesAsync(userId);
            return Ok(categories);
        }

        // GET: api/events/categories/system
        [HttpGet("categories/system")]
        [ProducesResponseType(typeof(IEnumerable<EventCategoryDto>), 200)]
        public async Task<IActionResult> GetSystemCategories()
        {
            var categories = await _eventService.GetSystemCategoriesAsync();
            return Ok(categories);
        }

        // GET: api/events/categories/custom
        [HttpGet("categories/custom")]
        [ProducesResponseType(typeof(IEnumerable<EventCategoryDto>), 200)]
        public async Task<IActionResult> GetCustomCategories()
        {
            var userId = GetCurrentUserID();
            var categories = await _eventService.GetUserCustomCategoriesAsync(userId);
            return Ok(categories);
        }

        // POST: api/events/categories
        [HttpPost("categories")]
        [ProducesResponseType(typeof(EventCategoryDto), 201)]
        public async Task<IActionResult> CreateCategory([FromBody] CreateEventCategoryDto createDto)
        {
            var userId = GetCurrentUserID();
            var category = await _eventService.CreateCategoryAsync(userId, createDto);
            return CreatedAtAction(nameof(GetCategories), category);
        }

        // DELETE: api/events/categories/{categoryId}
        [HttpDelete("categories/{categoryId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> DeleteCategory(Guid categoryId)
        {
            var success = await _eventService.DeleteCategoryAsync(categoryId);
            if (!success)
                return BadRequest(new { message = "Cannot delete system category or category not found" });

            return NoContent();
        }

        // ═══════════════════════════════════════════════════
        // SEARCH & STATISTICS
        // ═══════════════════════════════════════════════════

        // GET: api/events/search
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<EventDto>), 200)]
        public async Task<IActionResult> SearchEvents([FromQuery] string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest(new { message = "Search term is required" });

            var userId = GetCurrentUserID();
            var events = await _eventService.SearchEventsAsync(userId, term);
            return Ok(events);
        }

        // GET: api/events/statistics
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(EventStatisticsDto), 200)]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var userId = GetCurrentUserID();
            var stats = await _eventService.GetEventStatisticsAsync(userId, startDate, endDate);
            return Ok(stats);
        }
    }

    // Controller-specific DTO - unique name to avoid conflicts
    public class EventStatusDto
    {
        public string NewStatus { get; set; } = string.Empty;
    }
}