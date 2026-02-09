using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Domain.Entities;
using SphereScheduleAPI.Infrastructure.Data;
using SphereScheduleAPI.Application.Services;

namespace SphereScheduleAPI.Application.Services
{
    public class ParticipantService : IParticipantService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ParticipantService> _logger;
        private readonly IEmailService _emailService;

        public ParticipantService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<ParticipantService> logger,
            IEmailService emailService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<ParticipantDto> CreateParticipantAsync(Guid appointmentId, CreateParticipantDto createDto)
        {
            try
            {
                // Validate appointment exists and is active
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && !a.IsDeleted);

                if (appointment == null)
                {
                    throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found");
                }

                // Check if participant already exists for this appointment
                var existingParticipant = await _context.Participants
                    .FirstOrDefaultAsync(p => p.AppointmentId == appointmentId &&
                                             p.Email.ToLower() == createDto.Email.ToLower());

                if (existingParticipant != null)
                {
                    throw new InvalidOperationException($"Participant with email {createDto.Email} already exists for this appointment");
                }

                // If UserId is provided, validate user exists
                if (createDto.UserId.HasValue)
                {
                    var userExists = await _context.Users.AnyAsync(u => u.UserId == createDto.UserId.Value && u.IsActive);
                    if (!userExists)
                    {
                        throw new ArgumentException($"User with ID {createDto.UserId} not found or inactive");
                    }
                }

                var participant = _mapper.Map<Participant>(createDto);
                participant.ParticipantId = Guid.NewGuid();
                participant.AppointmentId = appointmentId;
                participant.CreatedAt = DateTimeOffset.UtcNow;
                participant.UpdatedAt = DateTimeOffset.UtcNow;

                _context.Participants.Add(participant);
                await _context.SaveChangesAsync();

                // Send invitation email if status is pending and SendInvitations is true
                if (participant.InvitationStatus == "pending")
                {
                    await SendInvitationEmailAsync(participant, appointment);
                }

                _logger.LogInformation("Created participant {ParticipantId} for appointment {AppointmentId}",
                    participant.ParticipantId, appointmentId);

                return await GetParticipantByIdAsync(participant.ParticipantId, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating participant for appointment {AppointmentId}", appointmentId);
                throw;
            }
        }

        public async Task<IEnumerable<ParticipantDto>> CreateParticipantsBulkAsync(BulkAddParticipantsDto bulkDto)
        {
            try
            {
                // Validate appointment exists and is active
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.AppointmentId == bulkDto.AppointmentId && !a.IsDeleted);

                if (appointment == null)
                {
                    throw new KeyNotFoundException($"Appointment with ID {bulkDto.AppointmentId} not found");
                }

                var newParticipants = new List<Participant>();
                var createdParticipants = new List<ParticipantDto>();

                foreach (var participantDto in bulkDto.Participants)
                {
                    // Check if participant already exists
                    var existingParticipant = await _context.Participants
                        .FirstOrDefaultAsync(p => p.AppointmentId == bulkDto.AppointmentId &&
                                                 p.Email.ToLower() == participantDto.Email.ToLower());

                    if (existingParticipant != null)
                    {
                        _logger.LogWarning("Participant with email {Email} already exists for appointment {AppointmentId}",
                            participantDto.Email, bulkDto.AppointmentId);
                        continue;
                    }

                    // If UserId is provided, validate user exists
                    if (participantDto.UserId.HasValue)
                    {
                        var userExists = await _context.Users.AnyAsync(u => u.UserId == participantDto.UserId.Value && u.IsActive);
                        if (!userExists)
                        {
                            _logger.LogWarning("User with ID {UserId} not found for participant {Email}",
                                participantDto.UserId, participantDto.Email);
                            continue;
                        }
                    }

                    var participant = _mapper.Map<Participant>(participantDto);
                    participant.ParticipantId = Guid.NewGuid();
                    participant.AppointmentId = bulkDto.AppointmentId;
                    participant.CreatedAt = DateTimeOffset.UtcNow;
                    participant.UpdatedAt = DateTimeOffset.UtcNow;

                    newParticipants.Add(participant);
                }

                if (newParticipants.Any())
                {
                    _context.Participants.AddRange(newParticipants);
                    await _context.SaveChangesAsync();

                    // Send invitations if requested
                    if (bulkDto.SendInvitations)
                    {
                        foreach (var participant in newParticipants.Where(p => p.InvitationStatus == "pending"))
                        {
                            await SendInvitationEmailAsync(participant, appointment);
                        }
                    }

                    // Get created participants with details
                    foreach (var participant in newParticipants)
                    {
                        var participantDto = await GetParticipantByIdAsync(participant.ParticipantId, true);
                        createdParticipants.Add(participantDto);
                    }
                }

                _logger.LogInformation("Created {Count} participants for appointment {AppointmentId}",
                    newParticipants.Count, bulkDto.AppointmentId);

                return createdParticipants;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating participants in bulk for appointment {AppointmentId}", bulkDto.AppointmentId);
                throw;
            }
        }

        public async Task<ParticipantDto> GetParticipantByIdAsync(Guid participantId, bool includeDetails = false)
        {
            var query = _context.Participants.AsQueryable();

            if (includeDetails)
            {
                query = query
                    .Include(p => p.Appointment)
                    .Include(p => p.User);
            }

            var participant = await query
                .FirstOrDefaultAsync(p => p.ParticipantId == participantId);

            if (participant == null)
            {
                throw new KeyNotFoundException($"Participant with ID {participantId} not found");
            }

            var participantDto = _mapper.Map<ParticipantDto>(participant);

            // Add additional details if requested
            if (includeDetails)
            {
                if (participant.User != null)
                {
                    participantDto.UserDisplayName = participant.User.DisplayName;
                    participantDto.UserAvatarUrl = participant.User.AvatarUrl;
                }

                if (participant.Appointment != null)
                {
                    participantDto.AppointmentTitle = participant.Appointment.Title;
                    participantDto.AppointmentStartDateTime = participant.Appointment.StartDateTime;
                    participantDto.AppointmentEndDateTime = participant.Appointment.EndDateTime;
                    participantDto.AppointmentLocation = participant.Appointment.Location;
                    participantDto.AppointmentIsVirtual = participant.Appointment.IsVirtual;
                    participantDto.AppointmentMeetingLink = participant.Appointment.MeetingLink;
                }
            }

            return participantDto;
        }

        public async Task<ParticipantDto> GetParticipantByEmailAsync(Guid appointmentId, string email)
        {
            var participant = await _context.Participants
                .Include(p => p.Appointment)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.AppointmentId == appointmentId &&
                                         p.Email.ToLower() == email.ToLower());

            if (participant == null)
            {
                throw new KeyNotFoundException($"Participant with email {email} not found for appointment {appointmentId}");
            }

            return _mapper.Map<ParticipantDto>(participant);
        }

        public async Task<IEnumerable<ParticipantDto>> GetParticipantsByAppointmentIdAsync(Guid appointmentId, ParticipantFilterDto filterDto)
        {
            filterDto.AppointmentId = appointmentId;
            return await GetParticipantsByFilterAsync(filterDto);
        }

        public async Task<IEnumerable<ParticipantDto>> GetParticipantsByFilterAsync(ParticipantFilterDto filterDto)
        {
            var query = _context.Participants.AsQueryable();

            // Apply filters
            if (filterDto.AppointmentId.HasValue)
            {
                query = query.Where(p => p.AppointmentId == filterDto.AppointmentId.Value);
            }

            if (filterDto.UserId.HasValue)
            {
                query = query.Where(p => p.UserId == filterDto.UserId.Value);
            }

            if (!string.IsNullOrEmpty(filterDto.Email))
            {
                query = query.Where(p => p.Email.ToLower().Contains(filterDto.Email.ToLower()));
            }

            if (!string.IsNullOrEmpty(filterDto.InvitationStatus))
            {
                query = query.Where(p => p.InvitationStatus == filterDto.InvitationStatus);
            }

            if (!string.IsNullOrEmpty(filterDto.ParticipantRole))
            {
                query = query.Where(p => p.ParticipantRole == filterDto.ParticipantRole);
            }

            if (filterDto.ResponseAfter.HasValue)
            {
                query = query.Where(p => p.ResponseReceivedAt >= filterDto.ResponseAfter.Value);
            }

            if (filterDto.ResponseBefore.HasValue)
            {
                query = query.Where(p => p.ResponseReceivedAt <= filterDto.ResponseBefore.Value);
            }

            // Include related data if requested
            if (filterDto.IncludeAppointmentDetails.HasValue && filterDto.IncludeAppointmentDetails.Value)
            {
                query = query.Include(p => p.Appointment);
            }

            if (filterDto.IncludeUserDetails.HasValue && filterDto.IncludeUserDetails.Value)
            {
                query = query.Include(p => p.User);
            }

            // Apply sorting
            query = ApplySorting(query, filterDto.SortBy, filterDto.SortDescending);

            // Apply pagination
            query = query
                .Skip((filterDto.PageNumber - 1) * filterDto.PageSize)
                .Take(filterDto.PageSize);

            var participants = await query.ToListAsync();
            return _mapper.Map<IEnumerable<ParticipantDto>>(participants);
        }

        public async Task<IEnumerable<ParticipantDto>> GetInvitationsByUserAsync(Guid userId, ParticipantFilterDto filterDto)
        {
            // Get appointments where user is invited
            var userEmail = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(userEmail))
            {
                throw new ArgumentException($"User with ID {userId} not found");
            }

            var query = _context.Participants
                .Include(p => p.Appointment)
                .Include(p => p.User)
                .Where(p => (p.UserId == userId || p.Email.ToLower() == userEmail.ToLower()) &&
                           !p.Appointment.IsDeleted);

            // Apply additional filters
            if (!string.IsNullOrEmpty(filterDto.InvitationStatus))
            {
                query = query.Where(p => p.InvitationStatus == filterDto.InvitationStatus);
            }

            if (filterDto.ResponseAfter.HasValue)
            {
                query = query.Where(p => p.ResponseReceivedAt >= filterDto.ResponseAfter.Value);
            }

            if (filterDto.ResponseBefore.HasValue)
            {
                query = query.Where(p => p.ResponseReceivedAt <= filterDto.ResponseBefore.Value);
            }

            // Apply sorting
            query = ApplySorting(query, filterDto.SortBy, filterDto.SortDescending);

            // Apply pagination
            query = query
                .Skip((filterDto.PageNumber - 1) * filterDto.PageSize)
                .Take(filterDto.PageSize);

            var participants = await query.ToListAsync();
            return _mapper.Map<IEnumerable<ParticipantDto>>(participants);
        }

        public async Task<ParticipantDto> UpdateParticipantAsync(Guid participantId, UpdateParticipantDto updateDto)
        {
            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.ParticipantId == participantId);

            if (participant == null)
            {
                throw new KeyNotFoundException($"Participant with ID {participantId} not found");
            }

            // Update properties if provided
            if (!string.IsNullOrEmpty(updateDto.Email))
            {
                // Check if new email already exists for this appointment
                if (updateDto.Email.ToLower() != participant.Email.ToLower())
                {
                    var existingParticipant = await _context.Participants
                        .FirstOrDefaultAsync(p => p.AppointmentId == participant.AppointmentId &&
                                                 p.Email.ToLower() == updateDto.Email.ToLower() &&
                                                 p.ParticipantId != participantId);

                    if (existingParticipant != null)
                    {
                        throw new InvalidOperationException($"Participant with email {updateDto.Email} already exists for this appointment");
                    }
                }

                participant.Email = updateDto.Email;
            }

            if (updateDto.FullName != null)
            {
                participant.FullName = updateDto.FullName;
            }

            if (!string.IsNullOrEmpty(updateDto.InvitationStatus))
            {
                participant.InvitationStatus = updateDto.InvitationStatus;

                // If status is accepted/declined/tentative, set response time
                if (updateDto.InvitationStatus == "accepted" ||
                    updateDto.InvitationStatus == "declined" ||
                    updateDto.InvitationStatus == "tentative")
                {
                    participant.ResponseReceivedAt = DateTimeOffset.UtcNow;
                }
            }

            if (!string.IsNullOrEmpty(updateDto.ParticipantRole))
            {
                participant.ParticipantRole = updateDto.ParticipantRole;
            }

            if (updateDto.UserId.HasValue)
            {
                // Validate user exists
                var userExists = await _context.Users.AnyAsync(u => u.UserId == updateDto.UserId.Value && u.IsActive);
                if (!userExists)
                {
                    throw new ArgumentException($"User with ID {updateDto.UserId} not found or inactive");
                }
                participant.UserId = updateDto.UserId.Value;
            }

            participant.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated participant {ParticipantId}", participantId);

            return await GetParticipantByIdAsync(participantId, true);
        }

        public async Task<ParticipantDto> UpdateParticipantStatusAsync(Guid participantId, UpdateParticipantStatusDto statusDto)
        {
            var participant = await _context.Participants
                .Include(p => p.Appointment)
                .FirstOrDefaultAsync(p => p.ParticipantId == participantId);

            if (participant == null)
            {
                throw new KeyNotFoundException($"Participant with ID {participantId} not found");
            }

            // Check if appointment is still active
            if (participant.Appointment == null || participant.Appointment.IsDeleted)
            {
                throw new InvalidOperationException("Cannot update status for a deleted appointment");
            }

            participant.InvitationStatus = statusDto.Status;
            participant.ResponseReceivedAt = DateTimeOffset.UtcNow;
            participant.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            // Log the response
            _logger.LogInformation("Participant {ParticipantId} updated status to {Status} for appointment {AppointmentId}",
                participantId, statusDto.Status, participant.AppointmentId);

            return await GetParticipantByIdAsync(participantId, true);
        }

        public async Task<bool> DeleteParticipantAsync(Guid participantId)
        {
            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.ParticipantId == participantId);

            if (participant == null)
            {
                throw new KeyNotFoundException($"Participant with ID {participantId} not found");
            }

            _context.Participants.Remove(participant);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted participant {ParticipantId} from appointment {AppointmentId}",
                participantId, participant.AppointmentId);

            return true;
        }

        public async Task<bool> DeleteParticipantsByAppointmentAsync(Guid appointmentId)
        {
            var participants = await _context.Participants
                .Where(p => p.AppointmentId == appointmentId)
                .ToListAsync();

            if (!participants.Any())
            {
                return true; // No participants to delete
            }

            _context.Participants.RemoveRange(participants);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} participants from appointment {AppointmentId}",
                participants.Count, appointmentId);

            return true;
        }

        public async Task<int> GetParticipantCountByAppointmentAsync(Guid appointmentId, string? status = null)
        {
            var query = _context.Participants
                .Where(p => p.AppointmentId == appointmentId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.InvitationStatus == status);
            }

            return await query.CountAsync();
        }

        public async Task<ParticipantStatisticsDto> GetParticipantStatisticsAsync(Guid appointmentId)
        {
            var participants = await _context.Participants
                .Where(p => p.AppointmentId == appointmentId)
                .ToListAsync();

            var statistics = new ParticipantStatisticsDto
            {
                TotalParticipants = participants.Count,
                AcceptedCount = participants.Count(p => p.InvitationStatus == "accepted"),
                DeclinedCount = participants.Count(p => p.InvitationStatus == "declined"),
                PendingCount = participants.Count(p => p.InvitationStatus == "pending"),
                TentativeCount = participants.Count(p => p.InvitationStatus == "tentative"),
                OrganizerCount = participants.Count(p => p.ParticipantRole == "organizer"),
                AttendeeCount = participants.Count(p => p.ParticipantRole == "attendee"),
                OptionalCount = participants.Count(p => p.ParticipantRole == "optional")
            };

            // Calculate acceptance rate
            var respondedCount = statistics.AcceptedCount + statistics.DeclinedCount + statistics.TentativeCount;
            statistics.AcceptanceRate = respondedCount > 0
                ? (double)statistics.AcceptedCount / respondedCount * 100
                : 0;

            // Group by status
            statistics.ParticipantsByStatus = participants
                .GroupBy(p => p.InvitationStatus)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by role
            statistics.ParticipantsByRole = participants
                .GroupBy(p => p.ParticipantRole)
                .ToDictionary(g => g.Key, g => g.Count());

            return statistics;
        }

        public async Task<bool> ResendInvitationAsync(Guid participantId)
        {
            var participant = await _context.Participants
                .Include(p => p.Appointment)
                .FirstOrDefaultAsync(p => p.ParticipantId == participantId);

            if (participant == null)
            {
                throw new KeyNotFoundException($"Participant with ID {participantId} not found");
            }

            // Check if appointment is still active
            if (participant.Appointment == null || participant.Appointment.IsDeleted)
            {
                throw new InvalidOperationException("Cannot resend invitation for a deleted appointment");
            }

            // Update invitation status
            participant.InvitationStatus = "sent";
            participant.UpdatedAt = DateTimeOffset.UtcNow;

            // Send invitation email
            await SendInvitationEmailAsync(participant, participant.Appointment);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Resent invitation to participant {ParticipantId} for appointment {AppointmentId}",
                participantId, participant.AppointmentId);

            return true;
        }

        public async Task<bool> CheckIfUserIsParticipantAsync(Guid appointmentId, Guid userId)
        {
            // Check by UserId
            var byUserId = await _context.Participants
                .AnyAsync(p => p.AppointmentId == appointmentId && p.UserId == userId);

            if (byUserId) return true;

            // Check by email
            var userEmail = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(userEmail))
            {
                return false;
            }

            return await _context.Participants
                .AnyAsync(p => p.AppointmentId == appointmentId &&
                              p.Email.ToLower() == userEmail.ToLower());
        }

        public async Task<bool> CheckIfEmailIsParticipantAsync(Guid appointmentId, string email)
        {
            return await _context.Participants
                .AnyAsync(p => p.AppointmentId == appointmentId &&
                              p.Email.ToLower() == email.ToLower());
        }

        public async Task<IEnumerable<ParticipantDto>> SearchParticipantsAsync(string searchTerm, Guid? appointmentId = null)
        {
            var query = _context.Participants
                .Include(p => p.Appointment)
                .Include(p => p.User)
                .Where(p => p.Email.ToLower().Contains(searchTerm.ToLower()) ||
                           (p.FullName != null && p.FullName.ToLower().Contains(searchTerm.ToLower())));

            if (appointmentId.HasValue)
            {
                query = query.Where(p => p.AppointmentId == appointmentId.Value);
            }

            var participants = await query
                .Take(50) // Limit results
                .ToListAsync();

            return _mapper.Map<IEnumerable<ParticipantDto>>(participants);
        }

        private IQueryable<Participant> ApplySorting(IQueryable<Participant> query, string? sortBy, bool sortDescending)
        {
            return (sortBy?.ToLower(), sortDescending) switch
            {
                ("email", false) => query.OrderBy(p => p.Email),
                ("email", true) => query.OrderByDescending(p => p.Email),
                ("fullname", false) => query.OrderBy(p => p.FullName),
                ("fullname", true) => query.OrderByDescending(p => p.FullName),
                ("invitationstatus", false) => query.OrderBy(p => p.InvitationStatus),
                ("invitationstatus", true) => query.OrderByDescending(p => p.InvitationStatus),
                ("participantrole", false) => query.OrderBy(p => p.ParticipantRole),
                ("participantrole", true) => query.OrderByDescending(p => p.ParticipantRole),
                ("responsereceivedat", false) => query.OrderBy(p => p.ResponseReceivedAt),
                ("responsereceivedat", true) => query.OrderByDescending(p => p.ResponseReceivedAt),
                ("createdat", false) => query.OrderBy(p => p.CreatedAt),
                ("createdat", true) => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderBy(p => p.CreatedAt)
            };
        }

        private async Task SendInvitationEmailAsync(Participant participant, Appointment appointment)
        {
            try
            {
                // This would be implemented with your email service
                // For now, just log the action
                _logger.LogInformation("Sending invitation email to {Email} for appointment {AppointmentTitle}",
                    participant.Email, appointment.Title);

                // Example email content
                var emailContent = $@"
                    You have been invited to: {appointment.Title}
                    Date/Time: {appointment.StartDateTime:g} - {appointment.EndDateTime:g}
                    Location: {(appointment.IsVirtual ? appointment.MeetingLink : appointment.Location)}
                    
                    Please respond by accepting or declining this invitation.
                ";

                // Uncomment when you have email service configured
                // await _emailService.SendEmailAsync(
                //     participant.Email,
                //     $"Invitation: {appointment.Title}",
                //     emailContent
                // );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invitation email to {Email}", participant.Email);
                // Don't throw - we don't want email failures to break the participant creation
            }
        }
    }
}