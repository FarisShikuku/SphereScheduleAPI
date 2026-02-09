using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Application.Mappings;
using SphereScheduleAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using SphereScheduleAPI.Infrastructure.Data; // Add this
using System.Threading.Tasks;

namespace SphereScheduleAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _context; // Add this
        private readonly ILogger<AppointmentsController> _logger; // Add this

        public AppointmentsController(IAppointmentService appointmentService, IMapper mapper)
        {
            _appointmentService = appointmentService;
            _mapper = mapper;
            
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID in token");
            }
            return userId;
        }

        // GET: api/appointments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointments(
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var appointments = await _appointmentService.GetUserAppointmentsAsync(userId, startDate, endDate);
                return Ok(_mapper.Map<IEnumerable<AppointmentDto>>(appointments));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving appointments", error = ex.Message });
            }
        }

        // GET: api/appointments/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<AppointmentDto>> GetAppointment(Guid id)
        {
            try
            {
                var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
                if (appointment == null)
                    return NotFound(new { message = $"Appointment with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (appointment.UserId != userId)
                    return Forbid();

                return Ok(_mapper.Map<AppointmentDto>(appointment));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving appointment", error = ex.Message });
            }
        }

        // POST: api/appointments
        [HttpPost]
        public async Task<ActionResult<AppointmentDto>> CreateAppointment([FromBody] CreateAppointmentDto createDto)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Map DTO to entity
                var appointment = _mapper.Map<Appointment>(createDto);
                appointment.UserId = userId;
                appointment.Status = "scheduled";
                appointment.CalendarColor = createDto.CalendarColor ?? "#2196F3";

                // Validate end date is after start date
                if (appointment.EndDateTime <= appointment.StartDateTime)
                {
                    return BadRequest(new { message = "End date/time must be after start date/time" });
                }

                var created = await _appointmentService.CreateAppointmentAsync(appointment);
                return CreatedAtAction(nameof(GetAppointment),
                    new { id = created.AppointmentId },
                    _mapper.Map<AppointmentDto>(created));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Time conflict"))
            {
                return Conflict(new { message = "Time conflict with existing appointment" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating appointment", error = ex.Message });
            }
        }

        // PUT: api/appointments/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<AppointmentDto>> UpdateAppointment(Guid id, [FromBody] UpdateAppointmentDto updateDto)
        {
            try
            {
                var existing = await _appointmentService.GetAppointmentByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Appointment with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (existing.UserId != userId)
                    return Forbid();

                // Map updates
                _mapper.Map(updateDto, existing);
                existing.UpdatedAt = DateTimeOffset.UtcNow;

                // Validate end date is after start date
                if (existing.EndDateTime <= existing.StartDateTime)
                {
                    return BadRequest(new { message = "End date/time must be after start date/time" });
                }

                var updated = await _appointmentService.UpdateAppointmentAsync(existing);
                return Ok(_mapper.Map<AppointmentDto>(updated));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Time conflict"))
            {
                return Conflict(new { message = "Time conflict with existing appointment" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating appointment", error = ex.Message });
            }
        }

        // DELETE: api/appointments/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAppointment(Guid id)
        {
            try
            {
                var existing = await _appointmentService.GetAppointmentByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Appointment with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (existing.UserId != userId)
                    return Forbid();

                var success = await _appointmentService.DeleteAppointmentAsync(id);
                if (!success)
                    return StatusCode(500, new { message = "Failed to delete appointment" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting appointment", error = ex.Message });
            }
        }

        // GET: api/appointments/upcoming
        [HttpGet("upcoming")]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetUpcomingAppointments([FromQuery] int daysAhead = 7)
        {
            try
            {
                var userId = GetCurrentUserId();
                var appointments = await _appointmentService.GetUpcomingAppointmentsAsync(userId, daysAhead);
                return Ok(_mapper.Map<IEnumerable<AppointmentDto>>(appointments));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving upcoming appointments", error = ex.Message });
            }
        }

        // GET: api/appointments/date-range
        [HttpGet("date-range")]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointmentsByDateRange(
            [FromQuery] DateTimeOffset startDate,
            [FromQuery] DateTimeOffset endDate)
        {
            try
            {
                var userId = GetCurrentUserId();
                var appointments = await _appointmentService.GetAppointmentsByDateRangeAsync(userId, startDate, endDate);
                return Ok(_mapper.Map<IEnumerable<AppointmentDto>>(appointments));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving appointments", error = ex.Message });
            }
        }

        // GET: api/appointments/status/{status}
        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointmentsByStatus(string status)
        {
            try
            {
                var userId = GetCurrentUserId();
                var appointments = await _appointmentService.GetAppointmentsByStatusAsync(userId, status);
                return Ok(_mapper.Map<IEnumerable<AppointmentDto>>(appointments));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving appointments by status", error = ex.Message });
            }
        }

        // GET: api/appointments/type/{type}
        [HttpGet("type/{type}")]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointmentsByType(string type)
        {
            try
            {
                var userId = GetCurrentUserId();
                var appointments = await _appointmentService.GetAppointmentsByTypeAsync(userId, type);
                return Ok(_mapper.Map<IEnumerable<AppointmentDto>>(appointments));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving appointments by type", error = ex.Message });
            }
        }

        // GET: api/appointments/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetAppointmentStatistics(
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var stats = await _appointmentService.GetAppointmentStatisticsAsync(userId, startDate, endDate);
                var count = await _appointmentService.GetAppointmentCountByUserAsync(userId);

                return Ok(new
                {
                    totalCount = count,
                    statistics = stats,
                    dateRange = new
                    {
                        startDate = startDate?.ToString("yyyy-MM-dd"),
                        endDate = endDate?.ToString("yyyy-MM-dd")
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving appointment statistics", error = ex.Message });
            }
        }

        // POST: api/appointments/{id}/status
        [HttpPost("{id}/status")]
        public async Task<ActionResult> ChangeAppointmentStatus(Guid id, [FromBody] ChangeStatusDto statusDto)
        {
            try
            {
                var existing = await _appointmentService.GetAppointmentByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Appointment with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (existing.UserId != userId)
                    return Forbid();

                var success = await _appointmentService.ChangeAppointmentStatusAsync(id, statusDto.NewStatus);
                if (!success)
                    return StatusCode(500, new { message = "Failed to change appointment status" });

                return Ok(new { message = "Appointment status updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error changing appointment status", error = ex.Message });
            }
        }

        // POST: api/appointments/{id}/reschedule
        [HttpPost("{id}/reschedule")]
        public async Task<ActionResult<AppointmentDto>> RescheduleAppointment(
            Guid id,
            [FromBody] RescheduleAppointmentDto rescheduleDto)
        {
            try
            {
                var existing = await _appointmentService.GetAppointmentByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Appointment with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (existing.UserId != userId)
                    return Forbid();

                // Validate new times
                if (rescheduleDto.NewEndDateTime <= rescheduleDto.NewStartDateTime)
                {
                    return BadRequest(new { message = "End date/time must be after start date/time" });
                }

                var updated = await _appointmentService.RescheduleAppointmentAsync(
                    id, rescheduleDto.NewStartDateTime, rescheduleDto.NewEndDateTime);

                return Ok(_mapper.Map<AppointmentDto>(updated));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Time conflict"))
            {
                return Conflict(new { message = "Time conflict with existing appointment" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error rescheduling appointment", error = ex.Message });
            }
        }

        // GET: api/appointments/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> SearchAppointments([FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                    return BadRequest(new { message = "Search term is required" });

                var userId = GetCurrentUserId();
                var appointments = await _appointmentService.SearchAppointmentsAsync(userId, term);
                return Ok(_mapper.Map<IEnumerable<AppointmentDto>>(appointments));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error searching appointments", error = ex.Message });
            }
        }

        // POST: api/appointments/check-conflict
        [HttpPost("check-conflict")]
        public async Task<ActionResult> CheckTimeConflict([FromBody] CheckTimeConflictDto conflictDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var hasConflict = await _appointmentService.CheckTimeConflictAsync(
                    userId, conflictDto.StartDateTime, conflictDto.EndDateTime, conflictDto.ExcludeAppointmentId);

                return Ok(new
                {
                    hasConflict,
                    message = hasConflict ? "Time conflict detected" : "No time conflict"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error checking time conflict", error = ex.Message });
            }
        }

        
    }

    // Supporting DTO classes for this controller
    public class ChangeStatusDto
    {
        public string NewStatus { get; set; }
    }

    public class RescheduleAppointmentDto
    {
        public DateTimeOffset NewStartDateTime { get; set; }
        public DateTimeOffset NewEndDateTime { get; set; }
    }

    public class CheckTimeConflictDto
    {
        public DateTimeOffset StartDateTime { get; set; }
        public DateTimeOffset EndDateTime { get; set; }
        public Guid? ExcludeAppointmentId { get; set; }
    }
}