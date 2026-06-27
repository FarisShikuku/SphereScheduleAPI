// Domain/Entities/EventCategory.cs
using System;
using System.Collections.Generic;

namespace SphereScheduleAPI.Domain.Entities
{
    public class EventCategory : BaseEntity
    {
        public Guid CategoryID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// NULL = system-wide category (available to all users).
        /// Non-null = user-specific custom category.
        /// </summary>
        public Guid? UserID { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string? Icon { get; set; }

        public string ColorCode { get; set; } = "#7C6CF8";

        public bool IsSystem { get; set; } = false;

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}