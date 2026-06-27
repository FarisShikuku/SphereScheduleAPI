// Application/DTOs/NoteDtos.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace SphereScheduleAPI.Application.DTOs
{
    public class NoteDto
    {
        public Guid NoteID { get; set; }
        public Guid UserID { get; set; }
        public string? Title { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid? TaskID { get; set; }
        public Guid? EventID { get; set; }
        public Guid? AppointmentID { get; set; }
        public Guid? MeetingID { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        // Additional info
        public string? LinkedEntityTitle { get; set; }
    }

    public class CreateNoteDto
    {
        [MaxLength(255)]
        public string? Title { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        // Optional links to entities
        public Guid? TaskID { get; set; }
        public Guid? EventID { get; set; }
        public Guid? AppointmentID { get; set; }
        public Guid? MeetingID { get; set; }
    }

    public class UpdateNoteDto
    {
        [MaxLength(255)]
        public string? Title { get; set; }

        public string? Content { get; set; }
    }

    public class NoteFilterDto
    {
        public Guid? UserID { get; set; }
        public Guid? TaskID { get; set; }
        public Guid? EventID { get; set; }
        public Guid? AppointmentID { get; set; }
        public Guid? MeetingID { get; set; }
        public string? SearchText { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "UpdatedAt";
        public bool SortDescending { get; set; } = true;
    }
}