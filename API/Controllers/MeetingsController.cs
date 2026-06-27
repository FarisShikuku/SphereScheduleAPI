// API/Controllers/MeetingsController.cs
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
    public class MeetingsController : ControllerBase
    {
        private readonly IMeetingService _meetingService;
        private readonly ILogger<MeetingsController> _logger;

        public MeetingsController(IMeetingService meetingService, ILogger<MeetingsController> logger)
        {
            _meetingService = meetingService;
            _logger = logger;
        }

        private Guid GetCurrentUserID()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user ID in token");
            return userId;
        }

        // GET: api/meetings
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<MeetingDto>), 200)]
        public async Task<IActionResult> GetMeetings([FromQuery] MeetingFilterDto? filter = null)
        {
            var userId = GetCurrentUserID();
            var meetings = await _meetingService.GetUserMeetingsAsync(userId, filter);
            return Ok(meetings);
        }

        // GET: api/meetings/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(MeetingDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMeeting(Guid id, [FromQuery] bool includeParticipants = false)
        {
            var meeting = await _meetingService.GetMeetingByIdAsync(id, includeParticipants);
            if (meeting == null)
                return NotFound(new { message = $"Meeting with ID {id} not found" });

            return Ok(meeting);
        }

        // GET: api/meetings/task/{taskId}
        [HttpGet("task/{taskId}")]
        [ProducesResponseType(typeof(IEnumerable<MeetingDto>), 200)]
        public async Task<IActionResult> GetMeetingsByTask(Guid taskId)
        {
            var meetings = await _meetingService.GetMeetingsByTaskAsync(taskId);
            return Ok(meetings);
        }

        // POST: api/meetings
        [HttpPost]
        [ProducesResponseType(typeof(MeetingDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateMeeting([FromBody] CreateMeetingDto createDto)
        {
            try
            {
                var userId = GetCurrentUserID();
                var meeting = await _meetingService.CreateMeetingAsync(userId, createDto);
                return CreatedAtAction(nameof(GetMeeting), new { id = meeting.MeetingID }, meeting);
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

        // PUT: api/meetings/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(MeetingDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateMeeting(Guid id, [FromBody] UpdateMeetingDto updateDto)
        {
            try
            {
                var meeting = await _meetingService.UpdateMeetingAsync(id, updateDto);
                return Ok(meeting);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // DELETE: api/meetings/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteMeeting(Guid id)
        {
            var success = await _meetingService.DeleteMeetingAsync(id);
            if (!success)
                return NotFound(new { message = $"Meeting with ID {id} not found" });

            return NoContent();
        }

        // GET: api/meetings/upcoming
        [HttpGet("upcoming")]
        [ProducesResponseType(typeof(IEnumerable<MeetingDto>), 200)]
        public async Task<IActionResult> GetUpcomingMeetings([FromQuery] int daysAhead = 7)
        {
            var userId = GetCurrentUserID();
            var meetings = await _meetingService.GetUpcomingMeetingsAsync(userId, daysAhead);
            return Ok(meetings);
        }

        // GET: api/meetings/live
        [HttpGet("live")]
        [ProducesResponseType(typeof(IEnumerable<MeetingDto>), 200)]
        public async Task<IActionResult> GetLiveMeetings()
        {
            var userId = GetCurrentUserID();
            var meetings = await _meetingService.GetLiveMeetingsAsync(userId);
            return Ok(meetings);
        }

        // GET: api/meetings/status/{status}
        [HttpGet("status/{status}")]
        [ProducesResponseType(typeof(IEnumerable<MeetingDto>), 200)]
        public async Task<IActionResult> GetMeetingsByStatus(string status)
        {
            var userId = GetCurrentUserID();
            var meetings = await _meetingService.GetMeetingsByStatusAsync(userId, status);
            return Ok(meetings);
        }

        // POST: api/meetings/{id}/status
        [HttpPost("{id}/status")]
        [ProducesResponseType(typeof(MeetingDto), 200)]
        public async Task<IActionResult> ChangeMeetingStatus(Guid id, [FromBody] MeetingStatusDto statusDto)
        {
            try
            {
                var meeting = await _meetingService.ChangeMeetingStatusAsync(id, statusDto.NewStatus);
                return Ok(meeting);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/meetings/{id}/participants
        [HttpPost("{id}/participants")]
        [ProducesResponseType(typeof(MeetingParticipantDto), 201)]
        public async Task<IActionResult> AddParticipant(Guid id, [FromBody] CreateMeetingParticipantDto createDto)
        {
            try
            {
                var participant = await _meetingService.AddParticipantAsync(id, createDto);
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

        // DELETE: api/meetings/participants/{participantId}
        [HttpDelete("participants/{participantId}")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> RemoveParticipant(Guid participantId)
        {
            var success = await _meetingService.RemoveParticipantAsync(participantId);
            if (!success)
                return NotFound(new { message = "Participant not found" });

            return NoContent();
        }

        // GET: api/meetings/{id}/participants
        [HttpGet("{id}/participants")]
        [ProducesResponseType(typeof(IEnumerable<MeetingParticipantDto>), 200)]
        public async Task<IActionResult> GetParticipants(Guid id)
        {
            var participants = await _meetingService.GetParticipantsAsync(id);
            return Ok(participants);
        }

        // GET: api/meetings/statistics
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(MeetingStatisticsDto), 200)]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var userId = GetCurrentUserID();
            var stats = await _meetingService.GetMeetingStatisticsAsync(userId, startDate, endDate);
            return Ok(stats);
        }
    }

    // Controller-specific DTO - unique name to avoid conflicts
    public class MeetingStatusDto
    {
        public string NewStatus { get; set; } = string.Empty;
    }
}