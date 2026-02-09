using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;

namespace SphereScheduleAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ParticipantsController : ControllerBase
    {
        private readonly IParticipantService _participantService;
        private readonly IAppointmentService _appointmentService;
        private readonly ILogger<ParticipantsController> _logger;

        public ParticipantsController(
            IParticipantService participantService,
            IAppointmentService appointmentService,
            ILogger<ParticipantsController> logger)
        {
            _participantService = participantService;
            _appointmentService = appointmentService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ParticipantDto>), 200)]
        public async Task<IActionResult> GetParticipants([FromQuery] ParticipantFilterDto filterDto)
        {
            var userId = GetUserIdFromToken();

            // If no appointment ID is specified, get all appointments where user is a participant
            if (!filterDto.AppointmentId.HasValue)
            {
                return await GetUserInvitations(filterDto);
            }

            // Check if user owns the appointment or is a participant
            var appointment = await _appointmentService.GetAppointmentByIdAsync(filterDto.AppointmentId.Value);
            var isOwner = appointment.UserId == userId;
            var isParticipant = await _participantService.CheckIfUserIsParticipantAsync(filterDto.AppointmentId.Value, userId);

            if (!isOwner && !isParticipant)
            {
                return Forbid();
            }

            var participants = await _participantService.GetParticipantsByFilterAsync(filterDto);
            return Ok(participants);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ParticipantDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetParticipantById(Guid id, [FromQuery] bool includeDetails = false)
        {
            var participant = await _participantService.GetParticipantByIdAsync(id, includeDetails);

            var userId = GetUserIdFromToken();
            var appointment = await _appointmentService.GetAppointmentByIdAsync(participant.AppointmentId);

            var isOwner = appointment.UserId == userId;
            var isParticipant = await _participantService.CheckIfUserIsParticipantAsync(participant.AppointmentId, userId);
            var isSelf = participant.UserId == userId ||
                        participant.Email.Equals(GetUserEmail(), StringComparison.OrdinalIgnoreCase);

            if (!isOwner && !isParticipant && !isSelf)
            {
                return Forbid();
            }

            return Ok(participant);
        }

        [HttpPost("appointment/{appointmentId}")]
        [ProducesResponseType(typeof(ParticipantDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddParticipant(Guid appointmentId, [FromBody] CreateParticipantDto createDto)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
            var userId = GetUserIdFromToken();

            if (appointment.UserId != userId)
            {
                return Forbid();
            }

            var participant = await _participantService.CreateParticipantAsync(appointmentId, createDto);
            return CreatedAtAction(nameof(GetParticipantById), new { id = participant.ParticipantId }, participant);
        }

        [HttpPost("bulk")]
        [ProducesResponseType(typeof(IEnumerable<ParticipantDto>), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddParticipantsBulk([FromBody] BulkAddParticipantsDto bulkDto)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(bulkDto.AppointmentId);
            var userId = GetUserIdFromToken();

            if (appointment.UserId != userId)
            {
                return Forbid();
            }

            var participants = await _participantService.CreateParticipantsBulkAsync(bulkDto);
            return CreatedAtAction(nameof(GetParticipants), new { appointmentId = bulkDto.AppointmentId }, participants);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ParticipantDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateParticipant(Guid id, [FromBody] UpdateParticipantDto updateDto)
        {
            var participant = await _participantService.GetParticipantByIdAsync(id);
            var appointment = await _appointmentService.GetAppointmentByIdAsync(participant.AppointmentId);
            var userId = GetUserIdFromToken();

            if (appointment.UserId != userId)
            {
                return Forbid();
            }

            var updatedParticipant = await _participantService.UpdateParticipantAsync(id, updateDto);
            return Ok(updatedParticipant);
        }

        [HttpPost("{id}/status")]
        [ProducesResponseType(typeof(ParticipantDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateParticipantStatus(Guid id, [FromBody] UpdateParticipantStatusDto statusDto)
        {
            var participant = await _participantService.GetParticipantByIdAsync(id);
            var userId = GetUserIdFromToken();

            // Allow if user is the participant or appointment owner
            var appointment = await _appointmentService.GetAppointmentByIdAsync(participant.AppointmentId);
            var isOwner = appointment.UserId == userId;
            var isSelf = participant.UserId == userId ||
                        participant.Email.Equals(GetUserEmail(), StringComparison.OrdinalIgnoreCase);

            if (!isOwner && !isSelf)
            {
                return Forbid();
            }

            var updatedParticipant = await _participantService.UpdateParticipantStatusAsync(id, statusDto);
            return Ok(updatedParticipant);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteParticipant(Guid id)
        {
            var participant = await _participantService.GetParticipantByIdAsync(id);
            var appointment = await _appointmentService.GetAppointmentByIdAsync(participant.AppointmentId);
            var userId = GetUserIdFromToken();

            if (appointment.UserId != userId)
            {
                return Forbid();
            }

            await _participantService.DeleteParticipantAsync(id);
            return NoContent();
        }

        [HttpDelete("appointment/{appointmentId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteAllParticipants(Guid appointmentId)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
            var userId = GetUserIdFromToken();

            if (appointment.UserId != userId)
            {
                return Forbid();
            }

            await _participantService.DeleteParticipantsByAppointmentAsync(appointmentId);
            return NoContent();
        }

        [HttpGet("appointment/{appointmentId}/stats")]
        [ProducesResponseType(typeof(ParticipantStatisticsDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetParticipantStatistics(Guid appointmentId)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
            var userId = GetUserIdFromToken();

            var isOwner = appointment.UserId == userId;
            var isParticipant = await _participantService.CheckIfUserIsParticipantAsync(appointmentId, userId);

            if (!isOwner && !isParticipant)
            {
                return Forbid();
            }

            var statistics = await _participantService.GetParticipantStatisticsAsync(appointmentId);
            return Ok(statistics);
        }

        [HttpGet("my-invitations")]
        [ProducesResponseType(typeof(IEnumerable<ParticipantDto>), 200)]
        public async Task<IActionResult> GetMyInvitations([FromQuery] ParticipantFilterDto filterDto)
        {
            var userId = GetUserIdFromToken();
            var invitations = await _participantService.GetInvitationsByUserAsync(userId, filterDto);
            return Ok(invitations);
        }

        [HttpGet("check/{appointmentId}")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> CheckIfParticipant(Guid appointmentId)
        {
            var userId = GetUserIdFromToken();
            var userEmail = GetUserEmail();

            var isParticipantByUserId = await _participantService.CheckIfUserIsParticipantAsync(appointmentId, userId);
            var isParticipantByEmail = await _participantService.CheckIfEmailIsParticipantAsync(appointmentId, userEmail);

            return Ok(new
            {
                UserId = userId,
                Email = userEmail,
                IsParticipant = isParticipantByUserId || isParticipantByEmail,
                ByUserId = isParticipantByUserId,
                ByEmail = isParticipantByEmail
            });
        }

        [HttpPost("{id}/resend-invitation")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResendInvitation(Guid id)
        {
            var participant = await _participantService.GetParticipantByIdAsync(id);
            var appointment = await _appointmentService.GetAppointmentByIdAsync(participant.AppointmentId);
            var userId = GetUserIdFromToken();

            if (appointment.UserId != userId)
            {
                return Forbid();
            }

            await _participantService.ResendInvitationAsync(id);
            return Ok(new { message = "Invitation resent successfully" });
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<ParticipantDto>), 200)]
        public async Task<IActionResult> SearchParticipants(
            [FromQuery] string searchTerm,
            [FromQuery] Guid? appointmentId = null)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            {
                return BadRequest(new { message = "Search term must be at least 2 characters" });
            }

            if (appointmentId.HasValue)
            {
                var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId.Value);
                var userId = GetUserIdFromToken();

                var isOwner = appointment.UserId == userId;
                var isParticipant = await _participantService.CheckIfUserIsParticipantAsync(appointmentId.Value, userId);

                if (!isOwner && !isParticipant)
                {
                    return Forbid();
                }
            }

            var participants = await _participantService.SearchParticipantsAsync(searchTerm, appointmentId);
            return Ok(participants);
        }

        private async Task<IActionResult> GetUserInvitations(ParticipantFilterDto filterDto)
        {
            var userId = GetUserIdFromToken();
            var invitations = await _participantService.GetInvitationsByUserAsync(userId, filterDto);
            return Ok(invitations);
        }

        private Guid GetUserIdFromToken()
        {
            // Extract user ID from JWT token
            // For demo, returning a hardcoded ID
            return Guid.Parse("12345678-1234-1234-1234-123456789abc");
        }

        private string GetUserEmail()
        {
            // Extract user email from JWT token
            // For demo, returning a hardcoded email
            return "user@example.com";
        }
    }
}