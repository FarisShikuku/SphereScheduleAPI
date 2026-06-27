// Application/DTOs/EventLogDtos.cs
using System;
using System.Collections.Generic;

namespace SphereScheduleAPI.Application.DTOs
{
    public class EventLogDto
    {
        public long LogID { get; set; }
        public Guid? UserID { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? EntitySchema { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public Guid? EntityID { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string LogLevel { get; set; } = string.Empty;
        public string? Message { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        // Additional info
        public string? UserDisplayName { get; set; }
        public string? UserEmail { get; set; }
    }

    public class EventLogFilterDto
    {
        public Guid? UserID { get; set; }
        public string? Action { get; set; }
        public string? EntityName { get; set; }
        public Guid? EntityID { get; set; }
        public string? LogLevel { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string? SearchText { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;
    }
}