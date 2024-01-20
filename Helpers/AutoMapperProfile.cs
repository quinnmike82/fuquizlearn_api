using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Accounts;

namespace fuquizlearn_api.Helpers
{
    public class AutoMapperProfile : Profile
    {
        // mappings between model and entity objects
        public AutoMapperProfile()
        {
            CreateMap<Account, AccountResponse>().ForMember(x => x.Dob, op => op.MapFrom(src => src.Dob.ToLocalTime()));

            CreateMap<Account, AuthenticateResponse>().ForMember(x => x.Dob, op => op.MapFrom(src => src.Dob.ToLocalTime()));

            CreateMap<RegisterRequest, Account>().ForMember(x => x.Dob, op => op.MapFrom(src => src.Dob.ToUniversalTime()))
                .ForAllMembers(x =>
                x.Condition(
                    (src, dest, prop) =>
                    {
                        // ignore null & empty string properties
                        if (prop == null) return false;
                        if (prop.GetType() == typeof(string) && string.IsNullOrEmpty((string)prop)) return false;
                        // ignore avatar
                        if (prop.GetType() == typeof(IFormFile)) return false;
                        return true;
                    }
                )
            );

            CreateMap<CreateRequest, Account>().ForMember(x => x.Dob, op => op.MapFrom(src => src.Dob.ToUniversalTime()));

            CreateMap<UpdateRequest, Account>()
                .ForAllMembers(x => x.Condition(
                    (src, dest, prop) =>
                    {
                        // ignore null & empty string properties
                        if (prop == null) return false;
                        if (prop.GetType() == typeof(string) && string.IsNullOrEmpty((string)prop)) return false;

                        // ignore null role
                        if (x.DestinationMember.Name == "Role" && src.Role == null) return false;
                        return true;
                    }
                ));
        }
    }
}
