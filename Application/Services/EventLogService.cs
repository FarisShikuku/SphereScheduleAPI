// Application/Services/EventLogService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Domain.Entities;
using SphereScheduleAPI.Infrastructure.Data;

namespace SphereScheduleAPI.Application.Services
{
    public class EventLogService : IEventLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<EventLogService> _logger;

        public EventLogService(ApplicationDbContext context, IMapper mapper, ILogger<EventLogService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<EventLogDto>> GetLogsAsync(EventLogFilterDto? filter = null)
        {
            var query = _context.EventLogs
                .Include(l => l.User)
                .AsQueryable();

            if (filter != null)
            {
                if (filter.UserID.HasValue)
                    query = query.Where(l => l.UserID == filter.UserID.Value);
                if (!string.IsNullOrEmpty(filter.Action))
                    query = query.Where(l => l.Action == filter.Action);
                if (!string.IsNullOrEmpty(filter.EntityName))
                    query = query.Where(l => l.EntityName == filter.EntityName);
                if (filter.EntityID.HasValue)
                    query = query.Where(l => l.EntityID == filter.EntityID.Value);
                if (!string.IsNullOrEmpty(filter.LogLevel))
                    query = query.Where(l => l.LogLevel == filter.LogLevel);
                if (filter.StartDate.HasValue)
                    query = query.Where(l => l.CreatedAt >= filter.StartDate.Value);
                if (filter.EndDate.HasValue)
                    query = query.Where(l => l.CreatedAt <= filter.EndDate.Value);
                if (!string.IsNullOrEmpty(filter.SearchText))
                    query = query.Where(l => l.Message!.Contains(filter.SearchText) || l.EntityName.Contains(filter.SearchText));

                query = query.OrderByDescending(l => l.CreatedAt)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize);
            }
            else
            {
                query = query.OrderByDescending(l => l.CreatedAt).Take(100);
            }

            var logs = await query.ToListAsync();
            return _mapper.Map<IEnumerable<EventLogDto>>(logs);
        }

        public async Task<EventLogDto?> GetLogByIdAsync(long logID)
        {
            var log = await _context.EventLogs
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.LogID == logID);

            return log == null ? null : _mapper.Map<EventLogDto>(log);
        }

        public async Task<EventLogDto> LogEventAsync(
            Guid? userID,
            string action,
            string entityName,
            Guid? entityID = null,
            string? oldValues = null,
            string? newValues = null,
            string? ipAddress = null,
            string? userAgent = null,
            string logLevel = "Info",
            string? message = null)
        {
            var log = new EventLog
            {
                UserID = userID,
                Action = action,
                EntityName = entityName,
                EntityID = entityID,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                LogLevel = logLevel,
                Message = message
            };

            _context.EventLogs.Add(log);
            await _context.SaveChangesAsync();

            return _mapper.Map<EventLogDto>(log);
        }

        public async Task<IEnumerable<EventLogDto>> GetEntityHistoryAsync(string entityName, Guid entityID)
        {
            var logs = await _context.EventLogs
                .Include(l => l.User)
                .Where(l => l.EntityName == entityName && l.EntityID == entityID)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<EventLogDto>>(logs);
        }

        public async Task<IEnumerable<EventLogDto>> GetUserActivityAsync(Guid userID, int count = 50)
        {
            var logs = await _context.EventLogs
                .Include(l => l.User)
                .Where(l => l.UserID == userID)
                .OrderByDescending(l => l.CreatedAt)
                .Take(count)
                .ToListAsync();

            return _mapper.Map<IEnumerable<EventLogDto>>(logs);
        }

        public async Task<int> CleanupOldLogsAsync(int daysToKeep = 90)
        {
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-daysToKeep);
            var oldLogs = await _context.EventLogs
                .Where(l => l.CreatedAt < cutoffDate)
                .ToListAsync();

            _context.EventLogs.RemoveRange(oldLogs);
            var count = await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} old event logs", count);
            return count;
        }
    }
}