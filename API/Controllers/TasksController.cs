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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SphereScheduleAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly IMapper _mapper;

        public TasksController(ITaskService taskService, IMapper mapper)
        {
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

        // GET: api/tasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasks(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tasks = await _taskService.GetUserTasksAsync(userId, startDate, endDate);
                return Ok(_mapper.Map<IEnumerable<TaskDto>>(tasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving tasks", error = ex.Message });
            }
        }

        // GET: api/tasks/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDto>> GetTask(Guid id)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(id);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                return Ok(_mapper.Map<TaskDto>(task));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving task", error = ex.Message });
            }
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskDto createDto)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Map DTO to entity
                var task = _mapper.Map<TaskEntity>(createDto);
                task.UserId = userId;
                task.Status = "pending";
                task.CompletionPercentage = 0;

                // Validate due date if provided
                if (task.DueDate.HasValue && task.DueDate < DateTime.Today)
                {
                    return BadRequest(new { message = "Due date cannot be in the past" });
                }

                var created = await _taskService.CreateTaskAsync(task);
                return CreatedAtAction(nameof(GetTask),
                    new { id = created.TaskId },
                    _mapper.Map<TaskDto>(created));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating task", error = ex.Message });
            }
        }

        // PUT: api/tasks/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<TaskDto>> UpdateTask(Guid id, [FromBody] UpdateTaskDto updateDto)
        {
            try
            {
                var existing = await _taskService.GetTaskByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Task with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (existing.UserId != userId)
                    return Forbid();

                // Map updates
                _mapper.Map(updateDto, existing);
                existing.UpdatedAt = DateTimeOffset.UtcNow;

                // Validate due date if provided
                if (existing.DueDate.HasValue && existing.DueDate < DateTime.Today && existing.Status != "completed")
                {
                    return BadRequest(new { message = "Due date cannot be in the past for incomplete tasks" });
                }

                var updated = await _taskService.UpdateTaskAsync(existing);
                return Ok(_mapper.Map<TaskDto>(updated));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating task", error = ex.Message });
            }
        }

        // DELETE: api/tasks/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTask(Guid id)
        {
            try
            {
                var existing = await _taskService.GetTaskByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Task with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (existing.UserId != userId)
                    return Forbid();

                var success = await _taskService.DeleteTaskAsync(id);
                if (!success)
                    return StatusCode(500, new { message = "Failed to delete task" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting task", error = ex.Message });
            }
        }

        // GET: api/tasks/overdue
        [HttpGet("overdue")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetOverdueTasks()
        {
            try
            {
                var userId = GetCurrentUserId();
                var tasks = await _taskService.GetOverdueTasksAsync(userId);
                return Ok(_mapper.Map<IEnumerable<TaskDto>>(tasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving overdue tasks", error = ex.Message });
            }
        }

        // GET: api/tasks/today
        [HttpGet("today")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTodayTasks()
        {
            try
            {
                var userId = GetCurrentUserId();
                var tasks = await _taskService.GetTodayTasksAsync(userId);
                return Ok(_mapper.Map<IEnumerable<TaskDto>>(tasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving today's tasks", error = ex.Message });
            }
        }

        // GET: api/tasks/upcoming
        [HttpGet("upcoming")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetUpcomingTasks([FromQuery] int daysAhead = 7)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tasks = await _taskService.GetUpcomingTasksAsync(userId, daysAhead);
                return Ok(_mapper.Map<IEnumerable<TaskDto>>(tasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving upcoming tasks", error = ex.Message });
            }
        }

        // GET: api/tasks/status/{status}
        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasksByStatus(string status)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tasks = await _taskService.GetTasksByStatusAsync(userId, status);
                return Ok(_mapper.Map<IEnumerable<TaskDto>>(tasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving tasks by status", error = ex.Message });
            }
        }

        // GET: api/tasks/priority/{priority}
        [HttpGet("priority/{priority}")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasksByPriority(string priority)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tasks = await _taskService.GetTasksByPriorityAsync(userId, priority);
                return Ok(_mapper.Map<IEnumerable<TaskDto>>(tasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving tasks by priority", error = ex.Message });
            }
        }

        // GET: api/tasks/category/{category}
        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasksByCategory(string category)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tasks = await _taskService.GetTasksByCategoryAsync(userId, category);
                return Ok(_mapper.Map<IEnumerable<TaskDto>>(tasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving tasks by category", error = ex.Message });
            }
        }

        // GET: api/tasks/type/{type}
        [HttpGet("type/{type}")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasksByType(string type)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tasks = await _taskService.GetTasksByTypeAsync(userId, type);
                return Ok(_mapper.Map<IEnumerable<TaskDto>>(tasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving tasks by type", error = ex.Message });
            }
        }

        // GET: api/tasks/date-range
        [HttpGet("date-range")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasksByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tasks = await _taskService.GetTasksByDateRangeAsync(userId, startDate, endDate);
                return Ok(_mapper.Map<IEnumerable<TaskDto>>(tasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving tasks by date range", error = ex.Message });
            }
        }

        // GET: api/tasks/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetTaskStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var stats = await _taskService.GetTaskStatisticsAsync(userId, startDate, endDate);
                var completionStats = await _taskService.GetTaskCompletionStatsAsync(userId, startDate, endDate);
                var count = await _taskService.GetTaskCountByUserAsync(userId);

                return Ok(new
                {
                    totalCount = count,
                    statistics = stats,
                    completionStats = completionStats,
                    dateRange = new
                    {
                        startDate = startDate?.ToString("yyyy-MM-dd"),
                        endDate = endDate?.ToString("yyyy-MM-dd")
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving task statistics", error = ex.Message });
            }
        }

        // POST: api/tasks/{id}/status
        [HttpPost("{id}/status")]
        public async Task<ActionResult> ChangeTaskStatus(Guid id, [FromBody] ChangeTaskStatusDto statusDto)
        {
            try
            {
                var existing = await _taskService.GetTaskByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Task with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (existing.UserId != userId)
                    return Forbid();

                var success = await _taskService.ChangeTaskStatusAsync(id, statusDto.NewStatus, statusDto.CompletionPercentage);
                if (!success)
                    return StatusCode(500, new { message = "Failed to change task status" });

                return Ok(new { message = "Task status updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error changing task status", error = ex.Message });
            }
        }

        // POST: api/tasks/{id}/progress
        [HttpPost("{id}/progress")]
        public async Task<ActionResult> UpdateTaskProgress(Guid id, [FromBody] UpdateTaskProgressDto progressDto)
        {
            try
            {
                var existing = await _taskService.GetTaskByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Task with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (existing.UserId != userId)
                    return Forbid();

                var success = await _taskService.UpdateTaskProgressAsync(id, progressDto.ProgressPercentage);
                if (!success)
                    return StatusCode(500, new { message = "Failed to update task progress" });

                return Ok(new { message = "Task progress updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating task progress", error = ex.Message });
            }
        }

        // POST: api/tasks/{id}/time-spent
        [HttpPost("{id}/time-spent")]
        public async Task<ActionResult> AddTimeSpent(Guid id, [FromBody] AddTimeSpentDto timeSpentDto)
        {
            try
            {
                var existing = await _taskService.GetTaskByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Task with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (existing.UserId != userId)
                    return Forbid();

                if (timeSpentDto.Minutes <= 0)
                    return BadRequest(new { message = "Minutes must be greater than 0" });

                var success = await _taskService.AddTimeSpentAsync(id, timeSpentDto.Minutes);
                if (!success)
                    return StatusCode(500, new { message = "Failed to add time spent" });

                return Ok(new { message = "Time spent added successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error adding time spent", error = ex.Message });
            }
        }

        // POST: api/tasks/{id}/duplicate
        [HttpPost("{id}/duplicate")]
        public async Task<ActionResult<TaskDto>> DuplicateTask(Guid id)
        {
            try
            {
                var existing = await _taskService.GetTaskByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Task with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (existing.UserId != userId)
                    return Forbid();

                var duplicated = await _taskService.DuplicateTaskAsync(id);
                return Ok(_mapper.Map<TaskDto>(duplicated));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error duplicating task", error = ex.Message });
            }
        }

        // GET: api/tasks/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> SearchTasks([FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                    return BadRequest(new { message = "Search term is required" });

                var userId = GetCurrentUserId();
                var tasks = await _taskService.SearchTasksAsync(userId, term);
                return Ok(_mapper.Map<IEnumerable<TaskDto>>(tasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error searching tasks", error = ex.Message });
            }
        }

        // GET: api/tasks/tags
        [HttpGet("tags")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasksByTags([FromQuery] string[] tags)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tasks = await _taskService.GetTasksByTagsAsync(userId, tags);
                return Ok(_mapper.Map<IEnumerable<TaskDto>>(tasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving tasks by tags", error = ex.Message });
            }
        }

        // POST: api/tasks/bulk/complete
        [HttpPost("bulk/complete")]
        public async Task<ActionResult> CompleteMultipleTasks([FromBody] Guid[] taskIds)
        {
            try
            {
                if (taskIds == null || taskIds.Length == 0)
                    return BadRequest(new { message = "No task IDs provided" });

                var userId = GetCurrentUserId();

                // Verify all tasks belong to user
                var tasks = await _taskService.GetUserTasksAsync(userId);
                var userTaskIds = tasks.Select(t => t.TaskId).ToHashSet();

                if (taskIds.Any(id => !userTaskIds.Contains(id)))
                    return Forbid();

                var success = await _taskService.CompleteMultipleTasksAsync(taskIds);
                if (!success)
                    return StatusCode(500, new { message = "Failed to complete tasks" });

                return Ok(new { message = "Tasks completed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error completing tasks", error = ex.Message });
            }
        }

        // POST: api/tasks/bulk/delete
        [HttpPost("bulk/delete")]
        public async Task<ActionResult> DeleteMultipleTasks([FromBody] Guid[] taskIds)
        {
            try
            {
                if (taskIds == null || taskIds.Length == 0)
                    return BadRequest(new { message = "No task IDs provided" });

                var userId = GetCurrentUserId();

                // Verify all tasks belong to user
                var tasks = await _taskService.GetUserTasksAsync(userId);
                var userTaskIds = tasks.Select(t => t.TaskId).ToHashSet();

                if (taskIds.Any(id => !userTaskIds.Contains(id)))
                    return Forbid();

                var success = await _taskService.DeleteMultipleTasksAsync(taskIds);
                if (!success)
                    return StatusCode(500, new { message = "Failed to delete tasks" });

                return Ok(new { message = "Tasks deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting tasks", error = ex.Message });
            }
        }

        // POST: api/tasks/bulk/priority
        [HttpPost("bulk/priority")]
        public async Task<ActionResult> ChangeMultipleTasksPriority([FromBody] BulkPriorityChangeDto priorityDto)
        {
            try
            {
                if (priorityDto.TaskIds == null || priorityDto.TaskIds.Length == 0)
                    return BadRequest(new { message = "No task IDs provided" });

                if (string.IsNullOrEmpty(priorityDto.NewPriority))
                    return BadRequest(new { message = "New priority is required" });

                var userId = GetCurrentUserId();

                // Verify all tasks belong to user
                var tasks = await _taskService.GetUserTasksAsync(userId);
                var userTaskIds = tasks.Select(t => t.TaskId).ToHashSet();

                if (priorityDto.TaskIds.Any(id => !userTaskIds.Contains(id)))
                    return Forbid();

                var success = await _taskService.ChangeMultipleTasksPriorityAsync(priorityDto.TaskIds, priorityDto.NewPriority);
                if (!success)
                    return StatusCode(500, new { message = "Failed to change task priorities" });

                return Ok(new { message = "Task priorities updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error changing task priorities", error = ex.Message });
            }
        }

        // GET: api/tasks/{id}/subtasks
        [HttpGet("{id}/subtasks")]
        public async Task<ActionResult<IEnumerable<SubtaskDto>>> GetTaskSubtasks(Guid id)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(id);
                if (task == null)
                    return NotFound(new { message = $"Task with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (task.UserId != userId)
                    return Forbid();

                var subtasks = await _taskService.GetTaskSubtasksAsync(id);
                return Ok(_mapper.Map<IEnumerable<SubtaskDto>>(subtasks));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving task subtasks", error = ex.Message });
            }
        }

        
    }

    // Supporting DTO classes for this controller
    public class ChangeTaskStatusDto
    {
        public string NewStatus { get; set; }
        public int? CompletionPercentage { get; set; }
    }

    public class UpdateTaskProgressDto
    {
        [Range(0, 100)]
        public int ProgressPercentage { get; set; }
    }

    public class AddTimeSpentDto
    {
        [Range(1, 1440)]
        public int Minutes { get; set; }
    }

    public class BulkPriorityChangeDto
    {
        public Guid[] TaskIds { get; set; }
        public string NewPriority { get; set; }
    }
}