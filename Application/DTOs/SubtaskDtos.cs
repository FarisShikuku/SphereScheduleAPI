using System;
using System.ComponentModel.DataAnnotations;

namespace SphereScheduleAPI.Application.DTOs
{
    public class SubtaskDto
    {
        public Guid SubtaskId { get; set; }
        public Guid TaskId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public TimeSpan? DueTime { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public int SubtaskOrder { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public bool IsOverdue => Status != "completed" && Status != "cancelled" && DueDate < DateTime.Today;
        public string DueStatus => CalculateDueStatus(DueDate, Status);
        public int? DaysUntilDue => CalculateDaysUntilDue(DueDate);

        private static string CalculateDueStatus(DateTime? dueDate, string status)
        {
            if (status == "completed" || status == "cancelled")
                return status;

            if (!dueDate.HasValue)
                return "no_due_date";

            var today = DateTime.Today;
            var daysDiff = (dueDate.Value.Date - today).Days;

            if (daysDiff < 0)
                return "overdue";
            if (daysDiff == 0)
                return "today";
            if (daysDiff == 1)
                return "tomorrow";
            if (daysDiff <= 7)
                return "this_week";

            return "future";
        }

        private static int? CalculateDaysUntilDue(DateTime? dueDate)
        {
            if (!dueDate.HasValue)
                return null;

            var today = DateTime.Today;
            return (dueDate.Value.Date - today).Days;
        }
    }

    public class CreateSubtaskDto
    {
        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public string Status { get; set; } = "pending";

        public string Priority { get; set; } = "medium";

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? DueTime { get; set; }

        [Range(0, 1000)]
        public int SubtaskOrder { get; set; } = 0;
    }

    public class UpdateSubtaskDto
    {
        [StringLength(255, MinimumLength = 1)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public string Status { get; set; }

        public string Priority { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? DueTime { get; set; }

        [Range(0, 1000)]
        public int? SubtaskOrder { get; set; }
    }

    public class SubtaskFilterDto
    {
        public string Status { get; set; }
        public string Priority { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public bool? IsOverdue { get; set; }
        public bool? HasDueTime { get; set; }
    }

    public class SubtaskStatisticsDto
    {
        public int TotalSubtasks { get; set; }
        public int PendingSubtasks { get; set; }
        public int InProgressSubtasks { get; set; }
        public int CompletedSubtasks { get; set; }
        public int CancelledSubtasks { get; set; }
        public int OverdueSubtasks { get; set; }
        public int TodaySubtasks { get; set; }
        public decimal CompletionRate { get; set; }
        public TimeSpan? AverageCompletionTime { get; set; }
        public Dictionary<string, int> PriorityBreakdown { get; set; } = new();
    }

    public class ReorderSubtasksDto
    {
        [Required]
        public Dictionary<Guid, int> SubtaskOrders { get; set; }
    }

    public class BulkSubtaskActionDto
    {
        [Required]
        public Guid[] SubtaskIds { get; set; }
    }

    public class ChangeSubtaskStatusDto
    {
        [Required]
        public string NewStatus { get; set; }
    }

    public class ChangeSubtaskPriorityDto
    {
        [Required]
        public string NewPriority { get; set; }
    }

    public class UpdateSubtaskDueDateDto
    {
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? DueTime { get; set; }
    }

    public class MoveSubtaskDto
    {
        [Required]
        public Guid NewTaskId { get; set; }
    }

    public class CreateMultipleSubtasksDto
    {
        [Required]
        public List<CreateSubtaskDto> Subtasks { get; set; } = new();
    }
}