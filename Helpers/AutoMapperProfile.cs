using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;
using fuquizlearn_api.Models.Accounts;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Models.Posts;
using fuquizlearn_api.Models.Quiz;
using fuquizlearn_api.Models.QuizBank;

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

            CreateMap<QuizCreate, Quiz>().ForMember(x => x.Created, op => op.MapFrom((src) => DateTime.UtcNow));
            CreateMap<QuizUpdate, Quiz>()
                .ForAllMembers(x => x.Condition(
                    (src, dest, prop) =>
                    {
                        if (prop == null) return false;
                        if (prop.GetType() == typeof(string) && string.IsNullOrEmpty((string)prop)) return false;

                        return true;
                    }
                ));
            CreateMap<Quiz, QuizResponse>();

            CreateMap<QuizBankCreate, QuizBank>().ForMember(qb => qb.Visibility, op => op.MapFrom(src => src.Visibility ?? Visibility.Public));
            CreateMap<QuizBankUpdate, QuizBank>().ForAllMembers(x => x.Condition(
                    (src, dest, prop) =>
                    {
                        if (prop == null) return false;
                        if (prop.GetType() == typeof(string) && string.IsNullOrEmpty((string)prop)) return false;
                        return true;
                    }
                ));
            CreateMap<QuizBank, QuizBankResponse>()
                .ForMember(dest => dest.Author, opt => opt.MapFrom((src, dest, destMember, context) =>
                {
                    var mapper = context.Mapper;
                    var accountResponse = mapper.Map<AccountResponse>(src.Author);
                    return accountResponse;
                }))
                .ForMember(dest => dest.QuizCount,
               opt => opt.MapFrom(src => src.Quizes.Count()));
            CreateMap<ClassroomCreate, Classroom>();
            CreateMap<Classroom, ClassroomResponse>()
            .ForMember(dest => dest.Account, opt => opt.MapFrom((src, dest, destMember, context) =>
            {
                var mapper = context.Mapper;
                var accountResponse = mapper.Map<AccountResponse>(src.Account);
                return accountResponse;
            }))
            .ForMember(dest => dest.StudentNumber, opt => opt.MapFrom((src, dest, destMember, context) =>
            {
                return src.AccountIds?.Length;
            }))
            ;
            CreateMap<ClassroomUpdate, Classroom>();
            CreateMap<Post, PostResponse>()
            .ForMember(dest => dest.Author, opt => opt.MapFrom((src, dest, destMember, context) =>
            {
                var mapper = context.Mapper;
                var commentResponse = mapper.Map<AccountResponse>(src.Author);
                return commentResponse;
            }))
            ;
            CreateMap<PostCreate, Post>();
            CreateMap<PostUpdate, Post>();
            CreateMap<ClassroomCode, ClassroomCodeResponse>();
            CreateMap<CommentCreate, Comment>();
            CreateMap<Comment, CommentResponse>()
            .ForMember(dest => dest.Author, opt => opt.MapFrom((src, dest, destMember, context) =>
            {
                var mapper = context.Mapper;
                var commentResponse = mapper.Map<AccountResponse>(src.Author);
                return commentResponse;
            }))
            .ForMember(x => x.PostId, op => op.MapFrom(src => src.Post.Id));

            CreateMap<LearnedProgress, ProgressResponse>();
        }
    }
}
