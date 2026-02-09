using AutoMapper;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Domain.Entities;

namespace SphereScheduleAPI.Application.Mappings
{
    public class ReminderProfile : Profile
    {
        public ReminderProfile()
        {
            CreateMap<CreateReminderDto, Reminder>()
                .ForMember(dest => dest.ReminderId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.SentAt, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "pending"))
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Task, opt => opt.Ignore())
                .ForMember(dest => dest.Appointment, opt => opt.Ignore());

            CreateMap<Reminder, ReminderDto>()
                .ForMember(dest => dest.ReminderId, opt => opt.MapFrom(src => src.ReminderId))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

            CreateMap<UpdateReminderDto, Reminder>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}