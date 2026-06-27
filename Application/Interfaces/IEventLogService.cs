// Application/Interfaces/IEventLogService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SphereScheduleAPI.Application.DTOs;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface IEventLogService
    {
        Task<IEnumerable<EventLogDto>> GetLogsAsync(EventLogFilterDto? filter = null);
        Task<EventLogDto?> GetLogByIdAsync(long logID);
        Task<EventLogDto> LogEventAsync(
            Guid? userID,
            string action,
            string entityName,
            Guid? entityID = null,
            string? oldValues = null,
            string? newValues = null,
            string? ipAddress = null,
            string? userAgent = null,
            string logLevel = "Info",
            string? message = null);

        // Entity-specific logs
        Task<IEnumerable<EventLogDto>> GetEntityHistoryAsync(string entityName, Guid entityID);
        Task<IEnumerable<EventLogDto>> GetUserActivityAsync(Guid userID, int count = 50);

        // Cleanup
        Task<int> CleanupOldLogsAsync(int daysToKeep = 90);
    }
}