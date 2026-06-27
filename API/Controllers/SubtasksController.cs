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
    [Route("api/tasks/{TaskID}/[controller]")]
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

        private Guid GetCurrentUserID()
        {
            var UserIDClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(UserIDClaim) || !Guid.TryParse(UserIDClaim, out var UserID))
            {
                throw new UnauthorizedAccessException("Invalid user ID in token");
            }
            return UserID;
        }

        // GET: api/tasks/{TaskID}/subtasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> GetSubtasks(Guid TaskID)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                var subtasks = await _subtaskService.GetTaskSubtasksAsync(TaskID);
                return Ok(_mapper.Map<IEnumerable<SubtaskDto>>(subtasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving subtasks", error = ex.Message });
            }
        }

        // GET: api/tasks/{TaskID}/subtasks/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<SubtaskDto>> GetSubtask(Guid TaskID, Guid id)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                // Verify subtask belongs to task
                if (!await _subtaskService.SubtaskBelongsToTaskAsync(id, TaskID))
                    return NotFound(new { message = $"Subtask with ID {id} not found in task {TaskID}" });

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

        // POST: api/tasks/{TaskID}/subtasks
        [HttpPost]
        public async Task<ActionResult<SubtaskDto>> CreateSubtask(Guid TaskID, [FromBody] CreateSubtaskDto createDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                // Map DTO to entity
                var subtask = _mapper.Map<Subtask>(createDto);
                subtask.TaskID = TaskID;

                var created = await _subtaskService.CreateSubtaskAsync(subtask);
                return CreatedAtAction(nameof(GetSubtask),
                    new { TaskID, id = created.SubTaskID },
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

        // PUT: api/tasks/{TaskID}/subtasks/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<SubtaskDto>> UpdateSubtask(Guid TaskID, Guid id, [FromBody] UpdateSubtaskDto updateDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                // Verify subtask exists and belongs to task
                var existing = await _subtaskService.GetSubtaskByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Subtask with ID {id} not found" });

                if (existing.TaskID != TaskID)
                    return BadRequest(new { message = $"Subtask does not belong to task {TaskID}" });

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

        // DELETE: api/tasks/{TaskID}/subtasks/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSubtask(Guid TaskID, Guid id)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                // Verify subtask exists and belongs to task
                var existing = await _subtaskService.GetSubtaskByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Subtask with ID {id} not found" });

                if (existing.TaskID != TaskID)
                    return BadRequest(new { message = $"Subtask does not belong to task {TaskID}" });

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

        // GET: api/tasks/{TaskID}/subtasks/status/{status}
        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> GetSubtasksByStatus(Guid TaskID, string status)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                var subtasks = await _subtaskService.GetSubtasksByStatusAsync(TaskID, status);
                return Ok(_mapper.Map<IEnumerable<SubtaskDto>>(subtasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving subtasks by status", error = ex.Message });
            }
        }

        // GET: api/tasks/{TaskID}/subtasks/priority/{priority}
        [HttpGet("priority/{priority}")]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> GetSubtasksByPriority(Guid TaskID, string priority)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                var subtasks = await _subtaskService.GetSubtasksByPriorityAsync(TaskID, priority);
                return Ok(_mapper.Map<IEnumerable<SubtaskDto>>(subtasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving subtasks by priority", error = ex.Message });
            }
        }

        // GET: api/tasks/{TaskID}/subtasks/overdue
        [HttpGet("overdue")]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> GetOverdueSubtasks(Guid TaskID)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                var subtasks = await _subtaskService.GetOverdueSubtasksAsync(TaskID);
                return Ok(_mapper.Map<IEnumerable<SubtaskDto>>(subtasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving overdue subtasks", error = ex.Message });
            }
        }

        // GET: api/tasks/{TaskID}/subtasks/today
        [HttpGet("today")]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> GetTodaySubtasks(Guid TaskID)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                var subtasks = await _subtaskService.GetTodaySubtasksAsync(TaskID);
                return Ok(_mapper.Map<IEnumerable<SubtaskDto>>(subtasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving today's subtasks", error = ex.Message });
            }
        }

        // GET: api/tasks/{TaskID}/subtasks/completed
        [HttpGet("completed")]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> GetCompletedSubtasks(
            Guid TaskID,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                var subtasks = await _subtaskService.GetCompletedSubtasksAsync(TaskID, startDate, endDate);
                return Ok(_mapper.Map<IEnumerable<SubtaskDto>>(subtasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving completed subtasks", error = ex.Message });
            }
        }

        // POST: api/tasks/{TaskID}/subtasks/{id}/status
        [HttpPost("{id}/status")]
        public async Task<ActionResult> ChangeSubtaskStatus(Guid TaskID, Guid id, [FromBody] ChangeSubtaskStatusDto statusDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                // Verify subtask exists and belongs to task
                if (!await _subtaskService.SubtaskBelongsToTaskAsync(id, TaskID))
                    return NotFound(new { message = $"Subtask with ID {id} not found in task {TaskID}" });

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

        // POST: api/tasks/{TaskID}/subtasks/{id}/complete
        [HttpPost("{id}/complete")]
        public async Task<ActionResult> CompleteSubtask(Guid TaskID, Guid id)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                // Verify subtask exists and belongs to task
                if (!await _subtaskService.SubtaskBelongsToTaskAsync(id, TaskID))
                    return NotFound(new { message = $"Subtask with ID {id} not found in task {TaskID}" });

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

        // POST: api/tasks/{TaskID}/subtasks/{id}/priority
        [HttpPost("{id}/priority")]
        public async Task<ActionResult> UpdateSubtaskPriority(Guid TaskID, Guid id, [FromBody] ChangeSubtaskPriorityDto priorityDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                // Verify subtask exists and belongs to task
                if (!await _subtaskService.SubtaskBelongsToTaskAsync(id, TaskID))
                    return NotFound(new { message = $"Subtask with ID {id} not found in task {TaskID}" });

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

        // POST: api/tasks/{TaskID}/subtasks/{id}/order
        [HttpPost("{id}/order")]
        public async Task<ActionResult> UpdateSubtaskOrder(Guid TaskID, Guid id, [FromBody] UpdateSubtaskOrderDto orderDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                // Verify subtask exists and belongs to task
                if (!await _subtaskService.SubtaskBelongsToTaskAsync(id, TaskID))
                    return NotFound(new { message = $"Subtask with ID {id} not found in task {TaskID}" });

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

        // POST: api/tasks/{TaskID}/subtasks/reorder
        [HttpPost("reorder")]
        public async Task<ActionResult> ReorderSubtasks(Guid TaskID, [FromBody] ReorderSubtasksDto reorderDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                var success = await _subtaskService.ReorderSubtasksAsync(TaskID, reorderDto.SubtaskOrders);
                if (!success)
                    return StatusCode(500, new { message = "Failed to reorder subtasks" });

                return Ok(new { message = "Subtasks reordered successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error reordering subtasks", error = ex.Message });
            }
        }

        // POST: api/tasks/{TaskID}/subtasks/{id}/due-date
        [HttpPost("{id}/due-date")]
        public async Task<ActionResult> UpdateSubtaskDueDate(Guid TaskID, Guid id, [FromBody] UpdateSubtaskDueDateDto dueDateDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                // Verify subtask exists and belongs to task
                if (!await _subtaskService.SubtaskBelongsToTaskAsync(id, TaskID))
                    return NotFound(new { message = $"Subtask with ID {id} not found in task {TaskID}" });

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

        // GET: api/tasks/{TaskID}/subtasks/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<SubtaskStatisticsDto>> GetSubtaskStatistics(Guid TaskID)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                var stats = await _subtaskService.GetSubtaskStatisticsAsync(TaskID);
                var completionRate = await _subtaskService.GetSubtaskCompletionRateAsync(TaskID);
                var avgCompletionTime = await _subtaskService.GetAverageCompletionTimeAsync(TaskID);

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

        // POST: api/tasks/{TaskID}/subtasks/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult> CreateMultipleSubtasks(Guid TaskID, [FromBody] CreateMultipleSubtasksDto bulkDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                if (bulkDto.Subtasks == null || !bulkDto.Subtasks.Any())
                    return BadRequest(new { message = "No subtasks provided" });

                // Map DTOs to entities
                var subtasks = _mapper.Map<List<Subtask>>(bulkDto.Subtasks);

                var success = await _subtaskService.CreateMultipleSubtasksAsync(TaskID, subtasks);
                if (!success)
                    return StatusCode(500, new { message = "Failed to create multiple subtasks" });

                return Ok(new { message = "Subtasks created successfully", count = bulkDto.Subtasks.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating multiple subtasks", error = ex.Message });
            }
        }

        // POST: api/tasks/{TaskID}/subtasks/bulk/delete
        [HttpPost("bulk/delete")]
        public async Task<ActionResult> DeleteMultipleSubtasks(Guid TaskID, [FromBody] BulkSubtaskActionDto bulkDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                if (bulkDto.SubTaskIDs == null || bulkDto.SubTaskIDs.Length == 0)
                    return BadRequest(new { message = "No subtask IDs provided" });

                // Verify all subtasks belong to this task
                var subtasks = await _subtaskService.GetTaskSubtasksAsync(TaskID);
                var taskSubTaskIDs = subtasks.Select(s => s.SubTaskID).ToHashSet();

                if (bulkDto.SubTaskIDs.Any(id => !taskSubTaskIDs.Contains(id)))
                    return BadRequest(new { message = "One or more subtasks do not belong to this task" });

                var success = await _subtaskService.DeleteMultipleSubtasksAsync(bulkDto.SubTaskIDs);
                if (!success)
                    return StatusCode(500, new { message = "Failed to delete multiple subtasks" });

                return Ok(new { message = "Subtasks deleted successfully", count = bulkDto.SubTaskIDs.Length });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting multiple subtasks", error = ex.Message });
            }
        }

        // POST: api/tasks/{TaskID}/subtasks/bulk/complete
        [HttpPost("bulk/complete")]
        public async Task<ActionResult> CompleteMultipleSubtasks(Guid TaskID, [FromBody] BulkSubtaskActionDto bulkDto)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                if (bulkDto.SubTaskIDs == null || bulkDto.SubTaskIDs.Length == 0)
                    return BadRequest(new { message = "No subtask IDs provided" });

                // Verify all subtasks belong to this task
                var subtasks = await _subtaskService.GetTaskSubtasksAsync(TaskID);
                var taskSubTaskIDs = subtasks.Select(s => s.SubTaskID).ToHashSet();

                if (bulkDto.SubTaskIDs.Any(id => !taskSubTaskIDs.Contains(id)))
                    return BadRequest(new { message = "One or more subtasks do not belong to this task" });

                var success = await _subtaskService.CompleteMultipleSubtasksAsync(bulkDto.SubTaskIDs);
                if (!success)
                    return StatusCode(500, new { message = "Failed to complete multiple subtasks" });

                return Ok(new { message = "Subtasks completed successfully", count = bulkDto.SubTaskIDs.Length });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error completing multiple subtasks", error = ex.Message });
            }
        }

        // POST: api/tasks/{TaskID}/subtasks/{id}/move
        [HttpPost("{id}/move")]
        public async Task<ActionResult> MoveSubtask(Guid TaskID, Guid id, [FromBody] MoveSubtaskDto moveDto)
        {
            try
            {
                // Verify source task exists and belongs to user
                var sourceTask = await _taskService.GetTaskByIdAsync(TaskID);
                if (sourceTask == null)
                    return NotFound(new { message = $"Source task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (sourceTask.UserID != UserID)
                    return Forbid();

                // Verify destination task exists and belongs to user
                var destTask = await _taskService.GetTaskByIdAsync(moveDto.NewTaskID);
                if (destTask == null)
                    return NotFound(new { message = $"Destination task with ID {moveDto.NewTaskID} not found" });

                if (destTask.UserID != UserID)
                    return Forbid();

                // Verify subtask exists and belongs to source task
                if (!await _subtaskService.SubtaskBelongsToTaskAsync(id, TaskID))
                    return NotFound(new { message = $"Subtask with ID {id} not found in task {TaskID}" });

                var success = await _subtaskService.MoveSubtaskToTaskAsync(id, moveDto.NewTaskID);
                if (!success)
                    return StatusCode(500, new { message = "Failed to move subtask" });

                return Ok(new { message = "Subtask moved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error moving subtask", error = ex.Message });
            }
        }

        // POST: api/tasks/{TaskID}/subtasks/{id}/copy
        [HttpPost("{id}/copy")]
        public async Task<ActionResult<SubtaskDto>> CopySubtask(Guid TaskID, Guid id, [FromBody] MoveSubtaskDto copyDto)
        {
            try
            {
                // Verify source task exists and belongs to user
                var sourceTask = await _taskService.GetTaskByIdAsync(TaskID);
                if (sourceTask == null)
                    return NotFound(new { message = $"Source task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (sourceTask.UserID != UserID)
                    return Forbid();

                // Verify destination task exists and belongs to user
                var destTask = await _taskService.GetTaskByIdAsync(copyDto.NewTaskID);
                if (destTask == null)
                    return NotFound(new { message = $"Destination task with ID {copyDto.NewTaskID} not found" });

                if (destTask.UserID != UserID)
                    return Forbid();

                // Verify subtask exists and belongs to source task
                if (!await _subtaskService.SubtaskBelongsToTaskAsync(id, TaskID))
                    return NotFound(new { message = $"Subtask with ID {id} not found in task {TaskID}" });

                var success = await _subtaskService.CopySubtaskToTaskAsync(id, copyDto.NewTaskID);
                if (!success)
                    return StatusCode(500, new { message = "Failed to copy subtask" });

                // Get the copied subtask (it will be the last one in the destination task)
                var destSubtasks = await _subtaskService.GetTaskSubtasksAsync(copyDto.NewTaskID);
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

        // GET: api/tasks/{TaskID}/subtasks/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> SearchSubtasks(Guid TaskID, [FromQuery] string term)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                if (string.IsNullOrWhiteSpace(term))
                    return BadRequest(new { message = "Search term is required" });

                var subtasks = await _subtaskService.SearchSubtasksAsync(TaskID, term);
                return Ok(_mapper.Map<IEnumerable<SubtaskDto>>(subtasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error searching subtasks", error = ex.Message });
            }
        }

        // GET: api/tasks/{TaskID}/subtasks/filter
        [HttpGet("filter")]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> FilterSubtasks(Guid TaskID, [FromQuery] SubtaskFilterDto filter)
        {
            try
            {
                // Verify task exists and belongs to user
                var task = await _taskService.GetTaskByIdAsync(TaskID);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {TaskID} not found" });

                var UserID = GetCurrentUserID();
                if (task.UserID != UserID)
                    return Forbid();

                var subtasks = await _subtaskService.FilterSubtasksAsync(TaskID, filter);
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