using AutoMapper;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Domain.Entities;

namespace SphereScheduleAPI.Application.Mappings
{
    public class ParticipantProfile : Profile
    {
        public ParticipantProfile()
        {
            CreateMap<CreateParticipantDto, Participant>()
                .ForMember(dest => dest.ParticipantId, opt => opt.Ignore())
                .ForMember(dest => dest.AppointmentId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ResponseReceivedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Appointment, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore());

            CreateMap<Participant, ParticipantDto>()
                .ForMember(dest => dest.UserDisplayName, opt => opt.Ignore())
                .ForMember(dest => dest.UserAvatarUrl, opt => opt.Ignore())
                .ForMember(dest => dest.AppointmentTitle, opt => opt.Ignore())
                .ForMember(dest => dest.AppointmentStartDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.AppointmentEndDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.AppointmentLocation, opt => opt.Ignore())
                .ForMember(dest => dest.AppointmentIsVirtual, opt => opt.Ignore())
                .ForMember(dest => dest.AppointmentMeetingLink, opt => opt.Ignore());

            CreateMap<UpdateParticipantDto, Participant>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}