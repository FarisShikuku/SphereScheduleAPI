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
            var UserID = GetUserIDFromToken();

            // If no appointment ID is specified, get all appointments where user is a participant
            if (!filterDto.AppointmentID.HasValue)
            {
                return await GetUserInvitations(filterDto);
            }

            // Check if user owns the appointment or is a participant
            var appointment = await _appointmentService.GetAppointmentByIdAsync(filterDto.AppointmentID.Value);
            var isOwner = appointment.UserID == UserID;
            var isParticipant = await _participantService.CheckIfUserIsParticipantAsync(filterDto.AppointmentID.Value, UserID);

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

            var UserID = GetUserIDFromToken();
            var appointment = await _appointmentService.GetAppointmentByIdAsync(participant.AppointmentID);

            var isOwner = appointment.UserID == UserID;
            var isParticipant = await _participantService.CheckIfUserIsParticipantAsync(participant.AppointmentID, UserID);
            var isSelf = participant.UserID == UserID ||
                        participant.Email.Equals(GetUserEmail(), StringComparison.OrdinalIgnoreCase);

            if (!isOwner && !isParticipant && !isSelf)
            {
                return Forbid();
            }

            return Ok(participant);
        }

        [HttpPost("appointment/{AppointmentID}")]
        [ProducesResponseType(typeof(ParticipantDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddParticipant(Guid AppointmentID, [FromBody] CreateParticipantDto createDto)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(AppointmentID);
            var UserID = GetUserIDFromToken();

            if (appointment.UserID != UserID)
            {
                return Forbid();
            }

            var participant = await _participantService.CreateParticipantAsync(AppointmentID, createDto);
            return CreatedAtAction(nameof(GetParticipantById), new { id = participant.ParticipantID }, participant);
        }

        [HttpPost("bulk")]
        [ProducesResponseType(typeof(IEnumerable<ParticipantDto>), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddParticipantsBulk([FromBody] BulkAddParticipantsDto bulkDto)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(bulkDto.AppointmentID);
            var UserID = GetUserIDFromToken();

            if (appointment.UserID != UserID)
            {
                return Forbid();
            }

            var participants = await _participantService.CreateParticipantsBulkAsync(bulkDto);
            return CreatedAtAction(nameof(GetParticipants), new { AppointmentID = bulkDto.AppointmentID }, participants);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ParticipantDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateParticipant(Guid id, [FromBody] UpdateParticipantDto updateDto)
        {
            var participant = await _participantService.GetParticipantByIdAsync(id);
            var appointment = await _appointmentService.GetAppointmentByIdAsync(participant.AppointmentID);
            var UserID = GetUserIDFromToken();

            if (appointment.UserID != UserID)
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
            var UserID = GetUserIDFromToken();

            // Allow if user is the participant or appointment owner
            var appointment = await _appointmentService.GetAppointmentByIdAsync(participant.AppointmentID);
            var isOwner = appointment.UserID == UserID;
            var isSelf = participant.UserID == UserID ||
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
            var appointment = await _appointmentService.GetAppointmentByIdAsync(participant.AppointmentID);
            var UserID = GetUserIDFromToken();

            if (appointment.UserID != UserID)
            {
                return Forbid();
            }

            await _participantService.DeleteParticipantAsync(id);
            return NoContent();
        }

        [HttpDelete("appointment/{AppointmentID}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteAllParticipants(Guid AppointmentID)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(AppointmentID);
            var UserID = GetUserIDFromToken();

            if (appointment.UserID != UserID)
            {
                return Forbid();
            }

            await _participantService.DeleteParticipantsByAppointmentAsync(AppointmentID);
            return NoContent();
        }

        [HttpGet("appointment/{AppointmentID}/stats")]
        [ProducesResponseType(typeof(ParticipantStatisticsDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetParticipantStatistics(Guid AppointmentID)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(AppointmentID);
            var UserID = GetUserIDFromToken();

            var isOwner = appointment.UserID == UserID;
            var isParticipant = await _participantService.CheckIfUserIsParticipantAsync(AppointmentID, UserID);

            if (!isOwner && !isParticipant)
            {
                return Forbid();
            }

            var statistics = await _participantService.GetParticipantStatisticsAsync(AppointmentID);
            return Ok(statistics);
        }

        [HttpGet("my-invitations")]
        [ProducesResponseType(typeof(IEnumerable<ParticipantDto>), 200)]
        public async Task<IActionResult> GetMyInvitations([FromQuery] ParticipantFilterDto filterDto)
        {
            var UserID = GetUserIDFromToken();
            var invitations = await _participantService.GetInvitationsByUserAsync(UserID, filterDto);
            return Ok(invitations);
        }

        [HttpGet("check/{AppointmentID}")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> CheckIfParticipant(Guid AppointmentID)
        {
            var UserID = GetUserIDFromToken();
            var userEmail = GetUserEmail();

            var isParticipantByUserID = await _participantService.CheckIfUserIsParticipantAsync(AppointmentID, UserID);
            var isParticipantByEmail = await _participantService.CheckIfEmailIsParticipantAsync(AppointmentID, userEmail);

            return Ok(new
            {
                UserID = UserID,
                Email = userEmail,
                IsParticipant = isParticipantByUserID || isParticipantByEmail,
                ByUserID = isParticipantByUserID,
                ByEmail = isParticipantByEmail
            });
        }

        [HttpPost("{id}/resend-invitation")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResendInvitation(Guid id)
        {
            var participant = await _participantService.GetParticipantByIdAsync(id);
            var appointment = await _appointmentService.GetAppointmentByIdAsync(participant.AppointmentID);
            var UserID = GetUserIDFromToken();

            if (appointment.UserID != UserID)
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
            [FromQuery] Guid? AppointmentID = null)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            {
                return BadRequest(new { message = "Search term must be at least 2 characters" });
            }

            if (AppointmentID.HasValue)
            {
                var appointment = await _appointmentService.GetAppointmentByIdAsync(AppointmentID.Value);
                var UserID = GetUserIDFromToken();

                var isOwner = appointment.UserID == UserID;
                var isParticipant = await _participantService.CheckIfUserIsParticipantAsync(AppointmentID.Value, UserID);

                if (!isOwner && !isParticipant)
                {
                    return Forbid();
                }
            }

            var participants = await _participantService.SearchParticipantsAsync(searchTerm, AppointmentID);
            return Ok(participants);
        }

        private async Task<IActionResult> GetUserInvitations(ParticipantFilterDto filterDto)
        {
            var UserID = GetUserIDFromToken();
            var invitations = await _participantService.GetInvitationsByUserAsync(UserID, filterDto);
            return Ok(invitations);
        }

        private Guid GetUserIDFromToken()
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