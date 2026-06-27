using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;

namespace SphereScheduleAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RemindersController : ControllerBase
    {
        private readonly IReminderService _reminderService;
        private readonly ILogger<RemindersController> _logger;

        public RemindersController(
            IReminderService reminderService,
            ILogger<RemindersController> logger)
        {
            _reminderService = reminderService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ReminderDto>), 200)]
        public async Task<IActionResult> GetReminders([FromQuery] ReminderFilterDto filterDto)
        {
            var UserID = GetUserIDFromToken();

            if (filterDto.UserID.HasValue && filterDto.UserID.Value != UserID)
            {
                return Forbid();
            }

            filterDto.UserID = UserID;
            var reminders = await _reminderService.GetRemindersByFilterAsync(filterDto);
            return Ok(reminders);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ReminderDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetReminderById(Guid id)
        {
            var reminder = await _reminderService.GetReminderByIdAsync(id);

            var UserID = GetUserIDFromToken();
            if (reminder.UserID != UserID)
            {
                return Forbid();
            }

            return Ok(reminder);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ReminderDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateReminder([FromBody] CreateReminderDto createDto)
        {
            var UserID = GetUserIDFromToken();
            if (createDto.UserID != UserID)
            {
                return Forbid();
            }

            var reminder = await _reminderService.CreateReminderAsync(createDto);
            return CreatedAtAction(nameof(GetReminderById), new { id = reminder.ReminderID }, reminder);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ReminderDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateReminder(Guid id, [FromBody] UpdateReminderDto updateDto)
        {
            var reminder = await _reminderService.GetReminderByIdAsync(id);
            var UserID = GetUserIDFromToken();
            if (reminder.UserID != UserID)
            {
                return Forbid();
            }

            var updatedReminder = await _reminderService.UpdateReminderAsync(id, updateDto);
            return Ok(updatedReminder);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteReminder(Guid id)
        {
            var reminder = await _reminderService.GetReminderByIdAsync(id);
            var UserID = GetUserIDFromToken();
            if (reminder.UserID != UserID)
            {
                return Forbid();
            }

            await _reminderService.DeleteReminderAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/mark-sent")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> MarkAsSent(Guid id)
        {
            var reminder = await _reminderService.GetReminderByIdAsync(id);
            var UserID = GetUserIDFromToken();
            if (reminder.UserID != UserID)
            {
                return Forbid();
            }

            await _reminderService.MarkReminderAsSentAsync(id);
            return Ok(new { message = "Reminder marked as sent" });
        }

        [HttpPost("{id}/cancel")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CancelReminder(Guid id)
        {
            var reminder = await _reminderService.GetReminderByIdAsync(id);
            var UserID = GetUserIDFromToken();
            if (reminder.UserID != UserID)
            {
                return Forbid();
            }

            await _reminderService.CancelReminderAsync(id);
            return Ok(new { message = "Reminder cancelled" });
        }

        [HttpPost("{id}/reschedule")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RescheduleReminder(Guid id, [FromBody] DateTimeOffset newDateTime)
        {
            var reminder = await _reminderService.GetReminderByIdAsync(id);
            var UserID = GetUserIDFromToken();
            if (reminder.UserID != UserID)
            {
                return Forbid();
            }

            await _reminderService.RescheduleReminderAsync(id, newDateTime);
            return Ok(new { message = "Reminder rescheduled" });
        }

        [HttpGet("pending")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> GetPendingReminders([FromQuery] DateTimeOffset? beforeDate = null)
        {
            var reminders = await _reminderService.GetPendingRemindersAsync(beforeDate);
            return Ok(reminders);
        }

        [HttpGet("stats")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetReminderStats()
        {
            var UserID = GetUserIDFromToken();

            var totalReminders = await _reminderService.GetReminderCountByUserAsync(UserID);
            var pendingReminders = await _reminderService.GetReminderCountByUserAsync(UserID, "pending");
            var sentReminders = await _reminderService.GetReminderCountByUserAsync(UserID, "sent");

            return Ok(new
            {
                Total = totalReminders,
                Pending = pendingReminders,
                Sent = sentReminders,
                Cancelled = await _reminderService.GetReminderCountByUserAsync(UserID, "cancelled"),
                Failed = await _reminderService.GetReminderCountByUserAsync(UserID, "failed")
            });
        }

        private Guid GetUserIDFromToken()
        {
            // For now, return a demo user ID
            // In production, implement proper JWT token extraction
            return Guid.Parse("12345678-1234-1234-1234-123456789abc");
        }
    }
}