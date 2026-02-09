namespace SphereScheduleAPI.Application.DTOs
{
    public class TaskFilterDto
    {
        public string Status { get; set; }
        public string Category { get; set; }
        public string Priority { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public bool? IsRecurring { get; set; }
        public string SearchTerm { get; set; }
        public string SortBy { get; set; }
        public bool SortDescending { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}