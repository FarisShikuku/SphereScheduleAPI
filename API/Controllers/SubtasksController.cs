using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Application.Mappings;
using SphereScheduleAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SphereScheduleAPI.API.Controllers
{
    [Route("api/tasks/{taskId}/[controller]")]
    [ApiController]
    [Authorize]
    public class SubtasksController : ControllerBase
    {
        private readonly ISubtaskService _subtaskService;
        private readonly ITaskService _taskService;
        private readonly IMapper _mapper;

        public SubtasksController(ISubtaskService subtaskService, ITaskService taskService, IMapper mapper)
        {
            _subtaskService = subtaskService;
            _taskService = taskService;
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

        // GET: api/tasks/{taskId}/subtasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> GetSubtasks(Guid taskId)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                var subtasks = await _subtaskService.GetTaskSubtasksAsync(taskId);
                return Ok(_mapper.Map<IEnumerable<SubtaskDto>>(subtasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving subtasks", error = ex.Message });
            }
        }

        // GET: api/tasks/{taskId}/subtasks/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<SubtaskDto>> GetSubtask(Guid taskId, Guid id)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                // Verify subtask belongs to task
                if (!await _subtaskService.SubtaskBelongsToTaskAsync(id, taskId))
                    return NotFound(new { message = $"Subtask with ID {id} not found in task {taskId}" });

                var subtask = await _subtaskService.GetSubtaskByIdAsync(id);
                if (subtask == null)
                    return NotFound(new { message = $"Subtask with ID {id} not found" });

                return Ok(_mapper.Map<SubtaskDto>(subtask));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving subtask", error = ex.Message });
            }
        }

        // POST: api/tasks/{taskId}/subtasks
        [HttpPost]
        public async Task<ActionResult<SubtaskDto>> CreateSubtask(Guid taskId, [FromBody] CreateSubtaskDto createDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                // Map DTO to entity
                var subtask = _mapper.Map<Subtask>(createDto);
                subtask.TaskId = taskId;

                var created = await _subtaskService.CreateSubtaskAsync(subtask);
                return CreatedAtAction(nameof(GetSubtask),
                    new { taskId, id = created.SubtaskId },
                    _mapper.Map<SubtaskDto>(created));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating subtask", error = ex.Message });
            }
        }

        // PUT: api/tasks/{taskId}/subtasks/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<SubtaskDto>> UpdateSubtask(Guid taskId, Guid id, [FromBody] UpdateSubtaskDto updateDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                // Verify subtask exists and belongs to task
                var existing = await _subtaskService.GetSubtaskByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Subtask with ID {id} not found" });

                if (existing.TaskId != taskId)
                    return BadRequest(new { message = $"Subtask does not belong to task {taskId}" });

                // Map updates
                _mapper.Map(updateDto, existing);
                existing.UpdatedAt = DateTimeOffset.UtcNow;

                var updated = await _subtaskService.UpdateSubtaskAsync(existing);
                return Ok(_mapper.Map<SubtaskDto>(updated));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating subtask", error = ex.Message });
            }
        }

        // DELETE: api/tasks/{taskId}/subtasks/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSubtask(Guid taskId, Guid id)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                // Verify subtask exists and belongs to task
                var existing = await _subtaskService.GetSubtaskByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Subtask with ID {id} not found" });

                if (existing.TaskId != taskId)
                    return BadRequest(new { message = $"Subtask does not belong to task {taskId}" });

                var success = await _subtaskService.DeleteSubtaskAsync(id);
                if (!success)
                    return StatusCode(500, new { message = "Failed to delete subtask" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting subtask", error = ex.Message });
            }
        }

        // GET: api/tasks/{taskId}/subtasks/status/{status}
        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> GetSubtasksByStatus(Guid taskId, string status)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                var subtasks = await _subtaskService.GetSubtasksByStatusAsync(taskId, status);
                return Ok(_mapper.Map<IEnumerable<SubtaskDto>>(subtasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving subtasks by status", error = ex.Message });
            }
        }

        // GET: api/tasks/{taskId}/subtasks/priority/{priority}
        [HttpGet("priority/{priority}")]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> GetSubtasksByPriority(Guid taskId, string priority)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                var subtasks = await _subtaskService.GetSubtasksByPriorityAsync(taskId, priority);
                return Ok(_mapper.Map<IEnumerable<SubtaskDto>>(subtasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving subtasks by priority", error = ex.Message });
            }
        }

        // GET: api/tasks/{taskId}/subtasks/overdue
        [HttpGet("overdue")]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> GetOverdueSubtasks(Guid taskId)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                var subtasks = await _subtaskService.GetOverdueSubtasksAsync(taskId);
                return Ok(_mapper.Map<IEnumerable<SubtaskDto>>(subtasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving overdue subtasks", error = ex.Message });
            }
        }

        // GET: api/tasks/{taskId}/subtasks/today
        [HttpGet("today")]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> GetTodaySubtasks(Guid taskId)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                var subtasks = await _subtaskService.GetTodaySubtasksAsync(taskId);
                return Ok(_mapper.Map<IEnumerable<SubtaskDto>>(subtasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving today's subtasks", error = ex.Message });
            }
        }

        // GET: api/tasks/{taskId}/subtasks/completed
        [HttpGet("completed")]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> GetCompletedSubtasks(
            Guid taskId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                var subtasks = await _subtaskService.GetCompletedSubtasksAsync(taskId, startDate, endDate);
                return Ok(_mapper.Map<IEnumerable<SubtaskDto>>(subtasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving completed subtasks", error = ex.Message });
            }
        }

        // POST: api/tasks/{taskId}/subtasks/{id}/status
        [HttpPost("{id}/status")]
        public async Task<ActionResult> ChangeSubtaskStatus(Guid taskId, Guid id, [FromBody] ChangeSubtaskStatusDto statusDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                // Verify subtask exists and belongs to task
                if (!await _subtaskService.SubtaskBelongsToTaskAsync(id, taskId))
                    return NotFound(new { message = $"Subtask with ID {id} not found in task {taskId}" });

                var success = await _subtaskService.ChangeSubtaskStatusAsync(id, statusDto.NewStatus);
                if (!success)
                    return StatusCode(500, new { message = "Failed to change subtask status" });

                return Ok(new { message = "Subtask status updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error changing subtask status", error = ex.Message });
            }
        }

        // POST: api/tasks/{taskId}/subtasks/{id}/complete
        [HttpPost("{id}/complete")]
        public async Task<ActionResult> CompleteSubtask(Guid taskId, Guid id)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                // Verify subtask exists and belongs to task
                if (!await _subtaskService.SubtaskBelongsToTaskAsync(id, taskId))
                    return NotFound(new { message = $"Subtask with ID {id} not found in task {taskId}" });

                var success = await _subtaskService.CompleteSubtaskAsync(id);
                if (!success)
                    return StatusCode(500, new { message = "Failed to complete subtask" });

                return Ok(new { message = "Subtask marked as completed" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error completing subtask", error = ex.Message });
            }
        }

        // POST: api/tasks/{taskId}/subtasks/{id}/priority
        [HttpPost("{id}/priority")]
        public async Task<ActionResult> UpdateSubtaskPriority(Guid taskId, Guid id, [FromBody] ChangeSubtaskPriorityDto priorityDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                // Verify subtask exists and belongs to task
                if (!await _subtaskService.SubtaskBelongsToTaskAsync(id, taskId))
                    return NotFound(new { message = $"Subtask with ID {id} not found in task {taskId}" });

                var success = await _subtaskService.UpdateSubtaskPriorityAsync(id, priorityDto.NewPriority);
                if (!success)
                    return StatusCode(500, new { message = "Failed to update subtask priority" });

                return Ok(new { message = "Subtask priority updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating subtask priority", error = ex.Message });
            }
        }

        // POST: api/tasks/{taskId}/subtasks/{id}/order
        [HttpPost("{id}/order")]
        public async Task<ActionResult> UpdateSubtaskOrder(Guid taskId, Guid id, [FromBody] UpdateSubtaskOrderDto orderDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                // Verify subtask exists and belongs to task
                if (!await _subtaskService.SubtaskBelongsToTaskAsync(id, taskId))
                    return NotFound(new { message = $"Subtask with ID {id} not found in task {taskId}" });

                var success = await _subtaskService.UpdateSubtaskOrderAsync(id, orderDto.NewOrder);
                if (!success)
                    return StatusCode(500, new { message = "Failed to update subtask order" });

                return Ok(new { message = "Subtask order updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating subtask order", error = ex.Message });
            }
        }

        // POST: api/tasks/{taskId}/subtasks/reorder
        [HttpPost("reorder")]
        public async Task<ActionResult> ReorderSubtasks(Guid taskId, [FromBody] ReorderSubtasksDto reorderDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                var success = await _subtaskService.ReorderSubtasksAsync(taskId, reorderDto.SubtaskOrders);
                if (!success)
                    return StatusCode(500, new { message = "Failed to reorder subtasks" });

                return Ok(new { message = "Subtasks reordered successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error reordering subtasks", error = ex.Message });
            }
        }

        // POST: api/tasks/{taskId}/subtasks/{id}/due-date
        [HttpPost("{id}/due-date")]
        public async Task<ActionResult> UpdateSubtaskDueDate(Guid taskId, Guid id, [FromBody] UpdateSubtaskDueDateDto dueDateDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                // Verify subtask exists and belongs to task
                if (!await _subtaskService.SubtaskBelongsToTaskAsync(id, taskId))
                    return NotFound(new { message = $"Subtask with ID {id} not found in task {taskId}" });

                var success = await _subtaskService.UpdateSubtaskDueDateAsync(id, dueDateDto.DueDate, dueDateDto.DueTime);
                if (!success)
                    return StatusCode(500, new { message = "Failed to update subtask due date" });

                return Ok(new { message = "Subtask due date updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating subtask due date", error = ex.Message });
            }
        }

        // GET: api/tasks/{taskId}/subtasks/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<SubtaskStatisticsDto>> GetSubtaskStatistics(Guid taskId)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                var stats = await _subtaskService.GetSubtaskStatisticsAsync(taskId);
                var completionRate = await _subtaskService.GetSubtaskCompletionRateAsync(taskId);
                var avgCompletionTime = await _subtaskService.GetAverageCompletionTimeAsync(taskId);

                var statistics = new SubtaskStatisticsDto
                {
                    TotalSubtasks = stats["total"],
                    PendingSubtasks = stats["pending"],
                    InProgressSubtasks = stats["in_progress"],
                    CompletedSubtasks = stats["completed"],
                    CancelledSubtasks = stats["cancelled"],
                    OverdueSubtasks = stats["overdue"],
                    TodaySubtasks = stats["today"],
                    CompletionRate = completionRate,
                    AverageCompletionTime = avgCompletionTime,
                    PriorityBreakdown = new Dictionary<string, int>
                    {
                        { "high", stats["high_priority"] },
                        { "medium", stats["medium_priority"] },
                        { "low", stats["low_priority"] }
                    }
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving subtask statistics", error = ex.Message });
            }
        }

        // POST: api/tasks/{taskId}/subtasks/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult> CreateMultipleSubtasks(Guid taskId, [FromBody] CreateMultipleSubtasksDto bulkDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                if (bulkDto.Subtasks == null || !bulkDto.Subtasks.Any())
                    return BadRequest(new { message = "No subtasks provided" });

                // Map DTOs to entities
                var subtasks = _mapper.Map<List<Subtask>>(bulkDto.Subtasks);

                var success = await _subtaskService.CreateMultipleSubtasksAsync(taskId, subtasks);
                if (!success)
                    return StatusCode(500, new { message = "Failed to create multiple subtasks" });

                return Ok(new { message = "Subtasks created successfully", count = bulkDto.Subtasks.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating multiple subtasks", error = ex.Message });
            }
        }

        // POST: api/tasks/{taskId}/subtasks/bulk/delete
        [HttpPost("bulk/delete")]
        public async Task<ActionResult> DeleteMultipleSubtasks(Guid taskId, [FromBody] BulkSubtaskActionDto bulkDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                if (bulkDto.SubtaskIds == null || bulkDto.SubtaskIds.Length == 0)
                    return BadRequest(new { message = "No subtask IDs provided" });

                // Verify all subtasks belong to this task
                var subtasks = await _subtaskService.GetTaskSubtasksAsync(taskId);
                var taskSubtaskIds = subtasks.Select(s => s.SubtaskId).ToHashSet();

                if (bulkDto.SubtaskIds.Any(id => !taskSubtaskIds.Contains(id)))
                    return BadRequest(new { message = "One or more subtasks do not belong to this task" });

                var success = await _subtaskService.DeleteMultipleSubtasksAsync(bulkDto.SubtaskIds);
                if (!success)
                    return StatusCode(500, new { message = "Failed to delete multiple subtasks" });

                return Ok(new { message = "Subtasks deleted successfully", count = bulkDto.SubtaskIds.Length });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting multiple subtasks", error = ex.Message });
            }
        }

        // POST: api/tasks/{taskId}/subtasks/bulk/complete
        [HttpPost("bulk/complete")]
        public async Task<ActionResult> CompleteMultipleSubtasks(Guid taskId, [FromBody] BulkSubtaskActionDto bulkDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                if (bulkDto.SubtaskIds == null || bulkDto.SubtaskIds.Length == 0)
                    return BadRequest(new { message = "No subtask IDs provided" });

                // Verify all subtasks belong to this task
                var subtasks = await _subtaskService.GetTaskSubtasksAsync(taskId);
                var taskSubtaskIds = subtasks.Select(s => s.SubtaskId).ToHashSet();

                if (bulkDto.SubtaskIds.Any(id => !taskSubtaskIds.Contains(id)))
                    return BadRequest(new { message = "One or more subtasks do not belong to this task" });

                var success = await _subtaskService.CompleteMultipleSubtasksAsync(bulkDto.SubtaskIds);
                if (!success)
                    return StatusCode(500, new { message = "Failed to complete multiple subtasks" });

                return Ok(new { message = "Subtasks completed successfully", count = bulkDto.SubtaskIds.Length });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error completing multiple subtasks", error = ex.Message });
            }
        }

        // POST: api/tasks/{taskId}/subtasks/{id}/move
        [HttpPost("{id}/move")]
        public async Task<ActionResult> MoveSubtask(Guid taskId, Guid id, [FromBody] MoveSubtaskDto moveDto)
        {
            try
            {
                // Verify source task exists and belongs to user
                var sourceTask = await _taskService.GetTaskByIdAsync(taskId);
                if (sourceTask == null)
                    return NotFound(new { message = $"Source task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (sourceTask.UserId != userId)
                    return Forbid();

                // Verify destination task exists and belongs to user
                var destTask = await _taskService.GetTaskByIdAsync(moveDto.NewTaskId);
                if (destTask == null)
                    return NotFound(new { message = $"Destination task with ID {moveDto.NewTaskId} not found" });

                if (destTask.UserId != userId)
                    return Forbid();

                // Verify subtask exists and belongs to source task
                if (!await _subtaskService.SubtaskBelongsToTaskAsync(id, taskId))
                    return NotFound(new { message = $"Subtask with ID {id} not found in task {taskId}" });

                var success = await _subtaskService.MoveSubtaskToTaskAsync(id, moveDto.NewTaskId);
                if (!success)
                    return StatusCode(500, new { message = "Failed to move subtask" });

                return Ok(new { message = "Subtask moved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error moving subtask", error = ex.Message });
            }
        }

        // POST: api/tasks/{taskId}/subtasks/{id}/copy
        [HttpPost("{id}/copy")]
        public async Task<ActionResult<SubtaskDto>> CopySubtask(Guid taskId, Guid id, [FromBody] MoveSubtaskDto copyDto)
        {
            try
            {
                // Verify source task exists and belongs to user
                var sourceTask = await _taskService.GetTaskByIdAsync(taskId);
                if (sourceTask == null)
                    return NotFound(new { message = $"Source task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (sourceTask.UserId != userId)
                    return Forbid();

                // Verify destination task exists and belongs to user
                var destTask = await _taskService.GetTaskByIdAsync(copyDto.NewTaskId);
                if (destTask == null)
                    return NotFound(new { message = $"Destination task with ID {copyDto.NewTaskId} not found" });

                if (destTask.UserId != userId)
                    return Forbid();

                // Verify subtask exists and belongs to source task
                if (!await _subtaskService.SubtaskBelongsToTaskAsync(id, taskId))
                    return NotFound(new { message = $"Subtask with ID {id} not found in task {taskId}" });

                var success = await _subtaskService.CopySubtaskToTaskAsync(id, copyDto.NewTaskId);
                if (!success)
                    return StatusCode(500, new { message = "Failed to copy subtask" });

                // Get the copied subtask (it will be the last one in the destination task)
                var destSubtasks = await _subtaskService.GetTaskSubtasksAsync(copyDto.NewTaskId);
                var copiedSubtask = destSubtasks.LastOrDefault();

                if (copiedSubtask == null)
                    return StatusCode(500, new { message = "Failed to retrieve copied subtask" });

                return Ok(_mapper.Map<SubtaskDto>(copiedSubtask));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error copying subtask", error = ex.Message });
            }
        }

        // GET: api/tasks/{taskId}/subtasks/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> SearchSubtasks(Guid taskId, [FromQuery] string term)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                if (string.IsNullOrWhiteSpace(term))
                    return BadRequest(new { message = "Search term is required" });

                var subtasks = await _subtaskService.SearchSubtasksAsync(taskId, term);
                return Ok(_mapper.Map<IEnumerable<SubtaskDto>>(subtasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error searching subtasks", error = ex.Message });
            }
        }

        // GET: api/tasks/{taskId}/subtasks/filter
        [HttpGet("filter")]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> FilterSubtasks(Guid taskId, [FromQuery] SubtaskFilterDto filter)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {taskId} not found" });

                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                var subtasks = await _subtaskService.FilterSubtasksAsync(taskId, filter);
                return Ok(_mapper.Map<IEnumerable<SubtaskDto>>(subtasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error filtering subtasks", error = ex.Message });
            }
        }
    }

    // Supporting DTO classes for this controller
    public class UpdateSubtaskOrderDto
    {
        [Range(0, 1000)]
        public int NewOrder { get; set; }
    }
}