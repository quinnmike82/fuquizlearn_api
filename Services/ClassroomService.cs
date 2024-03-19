using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Extensions;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Models.Quiz;
using fuquizlearn_api.Models.QuizBank;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace fuquizlearn_api.Services
{
    public interface IClassroomService
    {
        Task<ClassroomResponse> CreateClassroom(ClassroomCreate classroom, Account account);
        Task<ClassroomResponse> GetClassroomById(int id);
        Task<PagedResponse<ClassroomResponse>> GetAllClassrooms(PagedRequest options);
        Task<List<ClassroomResponse>> GetAllClassroomsByUserId(int id);
        Task<PagedResponse<ClassroomResponse>> GetAllClassroomsByAccountId(PagedRequest options, Account account);
        Task<ClassroomResponse> UpdateClassroom(ClassroomUpdate classroomUpdate, Account account);
        Task AddMember(AddMember addMember, Account account);
        Task RemoveMember(int memberId, int classroomId, Account account);
        Task DeleteClassroom(int id, Account account);
        Task<ClassroomCodeResponse> GenerateClassroomCode(int classroomId, Account account);
        Task<List<ClassroomCodeResponse>> GetAllClassroomCodes(int classroomId);
        Task JoinClassroomWithCode(string classroomCode, Account account);
        Task<QuizBankResponse> AddQuizBank(int classroomId, QuizBankCreate model, Account account);
        Task CopyQuizBank(int quizbankId, int classroomId, Account account);
        Task BatchRemoveMember(int classroomId, Account account, List<int> memberIds);
        Task BatchAddMember(int classroomId, Account account, List<int> memberIds);
        Task SentInvitationEmail(int classroomId, BatchMemberRequest batchMemberRequest, Account account);
        Task<PagedResponse<ClassroomResponse>> GetCurrentJoinedClassroom(PagedRequest options, Account account);
    }
    public class ClassroomService : IClassroomService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IHelperFrontEnd _helperFrontEnd;


        public ClassroomService(DataContext context, IMapper mapper, IHelperFrontEnd helperFrontEnd, IEmailService emailService)
        {
            _context = context;
            _mapper = mapper;
            _helperFrontEnd = helperFrontEnd;
            _emailService = emailService;
        }
        public async Task AddMember(AddMember addMember, Account account)
        {
            var classRoom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(c => c.Id == addMember.classroomId);
            if (classRoom == null)
            {
                throw new KeyNotFoundException("Could not find Classroom");
            }
            var check = await _context.ClassroomsMembers.FirstOrDefaultAsync(c => c.ClassroomId == addMember.classroomId && c.AccountId == addMember.memberId);
            if (check != null)
                throw new AppException("ClassroomMember already exists.");
            if (classRoom.Account.Id != account.Id)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }
            var classroomMember = new ClassroomMember
            {
                AccountId = addMember.memberId,
                ClassroomId = addMember.classroomId
            };
            _context.ClassroomsMembers.Add(classroomMember);
            if (classRoom.AccountIds == null)
            {
                classRoom.AccountIds = new int[] { classroomMember.Id };
            }
            else
            {
                classRoom.AccountIds = classRoom.AccountIds.Append(addMember.memberId).ToArray();
            }

            _context.Classrooms.Update(classRoom);
            await _context.SaveChangesAsync();
        }

        public async Task<QuizBankResponse> AddQuizBank(int classroomId, QuizBankCreate model, Account account)
        {
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == classroomId);
            if (classroom == null)
                throw new KeyNotFoundException("Cound not find Classroom");
            var quizBank = _mapper.Map<QuizBank>(model);
            quizBank.Created = DateTime.UtcNow;
            quizBank.Author = account;
            _context.QuizBanks.Add(quizBank);
            await _context.SaveChangesAsync();

            if (classroom.BankIds == null)
            {
                classroom.BankIds = new int[] { quizBank.Id };
            }
            else
            {
                classroom.BankIds = classroom.BankIds.Append(quizBank.Id).ToArray();
            }
            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync();

            return _mapper.Map<QuizBankResponse>(quizBank);
        }

        public async Task BatchAddMember(int classroomId, Account account, List<int> memberIds)
        {
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == classroomId);
            if (classroom == null)
                throw new KeyNotFoundException("Cound not find Classroom");
            if (!(classroom.Account.Id == account.Id || account.Role != Role.Admin))
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            memberIds = memberIds.Distinct().ToList();

            if (classroom.AccountIds != null)
            {
                var wasMember = memberIds.Where(id => classroom.AccountIds.Contains(id));
                if (wasMember.Any())
                {
                    throw new KeyNotFoundException($"these userId already be classroom's members:\n {wasMember}");
                }

                classroom.AccountIds = classroom.AccountIds.Concat(memberIds).ToArray();
            }
            else
            {
                classroom.AccountIds = memberIds.ToArray();
            }
            var classroomMembers = memberIds.Select(id => new ClassroomMember { AccountId = id, ClassroomId = classroomId });
            _context.ClassroomsMembers.AddRange(classroomMembers);
            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync();
        }

        public async Task BatchRemoveMember(int classroomId, Account account, List<int> memberIds)
        {
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == classroomId);
            if (classroom == null)
                throw new KeyNotFoundException("Cound not find Classroom");
            if (!(classroom.Account.Id == account.Id || account.Role != Role.Admin))
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            if (classroom.AccountIds == null)
            {
                throw new AppException("Classroom has no member");
            }

            var isNotMember = memberIds.Where(id => !classroom.AccountIds.Contains(id));

            if (isNotMember.Any())
            {
                throw new KeyNotFoundException($"Could not find these userId in classroom's members:\n {isNotMember}");
            }

            classroom.AccountIds = classroom.AccountIds.Where(id => !memberIds.Contains(id)).ToArray();
            var classroomMembers = await _context.ClassroomsMembers.Where(cm => cm.ClassroomId == classroomId && memberIds.Contains(cm.AccountId)).ToArrayAsync();
            _context.ClassroomsMembers.RemoveRange(classroomMembers);
            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync();
        }

        public async Task CopyQuizBank(int quizbankId, int classroomId, Account account)
        {
            var quizBank = await _context.QuizBanks.Include(q => q.Quizes).FirstOrDefaultAsync(i => i.Id == quizbankId);
            if (quizBank == null)
                throw new KeyNotFoundException("Could not find QuizBank");
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == classroomId);
            if (classroom == null)
                throw new KeyNotFoundException("Cound not find Classroom");
            if (account.Id != classroom.Account.Id && account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Unauthorized");
            var newBank = new QuizBank
            {
                BankName = quizBank.BankName,
                Description = quizBank.Description,
                Visibility = quizBank.Visibility,
                Author = account
            };
            _context.QuizBanks.Add(newBank);
            await _context.SaveChangesAsync();
            newBank.Quizes = new List<Quiz>();
            foreach (var quiz in quizBank.Quizes)
            {
                var newQuiz = new QuizCreate
                {
                    Answer = quiz.Answer,
                    Explaination = quiz.Explaination,
                    Question = quiz.Question
                };
                newBank.Quizes.Add(_mapper.Map<Quiz>(newQuiz));
            }
            _context.QuizBanks.Update(newBank);
            if (classroom.BankIds != null)
                classroom.BankIds.Append(newBank.Id);
            else classroom.BankIds = new int[] { newBank.Id };
            _context.QuizBanks.Update(newBank);
            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync();
        }

        public async Task<ClassroomResponse> CreateClassroom(ClassroomCreate classroom, Account account)
        {
            var newClass = _mapper.Map<Classroom>(classroom);
            newClass.Account = account;
            _context.Classrooms.Add(newClass);
            await _context.SaveChangesAsync();
            return _mapper.Map<ClassroomResponse>(newClass);
        }

        public async Task DeleteClassroom(int id, Account account)
        {
            var classRoom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(c => c.Id == id);
            if (classRoom == null)
            {
                throw new KeyNotFoundException("Could not find Classroom");
            }
            if (!(classRoom.Account.Id == account.Id || account.Role != Role.Admin))
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }
            classRoom.DeletedAt = DateTime.UtcNow;
            _context.Classrooms.Update(classRoom);
            await _context.SaveChangesAsync();
        }

        public async Task<ClassroomCodeResponse> GenerateClassroomCode(int classroomId, Account account)
        {
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == classroomId);
            if (classroom == null)
                throw new KeyNotFoundException("Could not find Classroom");

            var isAllow = classroom.Account.Id == account.Id;
            if (!isAllow && classroom.isStudentAllowInvite)
            {
                isAllow = classroom.AccountIds != null && classroom.AccountIds.Contains(account.Id);
            }

            if(!isAllow) throw new UnauthorizedAccessException("Unauthorized");
            string code = generateVerificationToken();
            var classroomCode = new ClassroomCode
            {
                Code = code,
                Expires = DateTime.UtcNow.AddDays(7),
                Classroom = classroom
            };
            classroom.ClassroomCodes ??= new List<ClassroomCode>();

            classroom.ClassroomCodes.Add(classroomCode);
            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync();
            return _mapper.Map<ClassroomCodeResponse>(classroomCode);
        }

        public async Task<List<ClassroomCodeResponse>> GetAllClassroomCodes(int classroomId)
        {
            var classroom = await _context.Classrooms.Include(i => i.ClassroomCodes).FirstOrDefaultAsync(i => i.Id == classroomId && i.DeletedAt == null);
            if (classroom == null)
                throw new KeyNotFoundException("Could not find Classroom");
            return _mapper.Map<List<ClassroomCodeResponse>>(classroom.ClassroomCodes);
        }

        public async Task<PagedResponse<ClassroomResponse>> GetAllClassrooms(PagedRequest options)
        {
            var pagedQuizes = await _context.Classrooms.Include(c => c.Account).Where(i => i.DeletedAt == null).ToPagedAsync(options,
               q => q.Classname.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));
            return new PagedResponse<ClassroomResponse>
            {
                Data = _mapper.Map<IEnumerable<ClassroomResponse>>(pagedQuizes.Data),
                Metadata = pagedQuizes.Metadata
            };
        }

        public async Task<PagedResponse<ClassroomResponse>> GetAllClassroomsByAccountId(PagedRequest options, Account account)
        {
            var classroomsOwned = _context.Classrooms.Where(i => i.Account.Id == account.Id && i.DeletedAt == null);
            var classroomsJoined = _context.ClassroomsMembers
                                                     .Include(i => i.Classroom)
                                                     .Where(i => i.AccountId == account.Id && i.Classroom.DeletedAt == null)
                                                     .Select(cm => cm.Classroom);

            var allClassrooms = await classroomsOwned.Concat(classroomsJoined)
                .ToPagedAsync(options,
                x => x.Classname.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));

            return new PagedResponse<ClassroomResponse>
            {
                Data = _mapper.Map<IEnumerable<ClassroomResponse>>(allClassrooms.Data),
                Metadata = allClassrooms.Metadata
            };
        }

        public async Task<List<ClassroomResponse>> GetAllClassroomsByUserId(int id)
        {
            var classroomsOwned = await _context.Classrooms.Where(i => i.Account.Id == id && i.DeletedAt == null).ToListAsync();
            var classroomsJoined = await _context.ClassroomsMembers
                                                     .Include(i => i.Classroom)
                                                     .Where(i => i.AccountId == id && i.Classroom.DeletedAt == null)
                                                     .Select(cm => cm.Classroom)
                                                     .ToListAsync();

            var allClassrooms = classroomsOwned.Concat(classroomsJoined).ToList();
            return _mapper.Map<List<ClassroomResponse>>(allClassrooms);
        }

        public async Task<ClassroomResponse> GetClassroomById(int id)
        {
            var classroom = await _context.Classrooms.FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);
            return _mapper.Map<ClassroomResponse>(classroom);
        }

        public async Task<PagedResponse<ClassroomResponse>> GetCurrentJoinedClassroom(PagedRequest options, Account account)
        {
            var classroomsJoined = await _context.ClassroomsMembers.Include(i => i.Classroom)
                                                     .Where(i => i.AccountId == account.Id)
                                                     .Select(cm => cm.Classroom)
                                                     .Where(c => c.DeletedAt == null)
                                                     .ToPagedAsync(options,
                x => x.Classname.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));

            return new PagedResponse<ClassroomResponse>
            {
                Data = _mapper.Map<IEnumerable<ClassroomResponse>>(classroomsJoined.Data),
                Metadata = classroomsJoined.Metadata
            };
        }

        public async Task JoinClassroomWithCode(string classroomCode, Account account)
        {
            var classroom = await _context.Classrooms.Include(i => i.ClassroomCodes).FirstOrDefaultAsync(i => i.ClassroomCodes.Any(c => c.Code == classroomCode));
            if (classroom == null)
            {
                throw new KeyNotFoundException("Could not find Classroom");
            }
            var check = await _context.ClassroomsMembers.FirstOrDefaultAsync(i => i.ClassroomId == classroom.Id && i.AccountId == account.Id);
            if (check != null)
            {
                throw new AppException("Already joined");
            }
            var code = classroom.ClassroomCodes.Single(i => i.Code == classroomCode);
            if (code.IsExpired)
                throw new AppException("Invalid Code");
            var classroomMember = new ClassroomMember
            {
                AccountId = account.Id,
                ClassroomId = classroom.Id
            };
            _context.ClassroomsMembers.Add(classroomMember);
            await _context.SaveChangesAsync();
            if (classroom.AccountIds == null)
            {
                classroom.AccountIds = new int[] { account.Id };
            }
            else
            {
                classroom.AccountIds = classroom.AccountIds.Append(account.Id).ToArray();
            }
            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveMember(int memberId, int classroomId, Account account)
        {
            var classroomMember = await _context.ClassroomsMembers.FirstOrDefaultAsync(i => i.AccountId == memberId && i.ClassroomId == classroomId);
            if (classroomMember == null)
            {
                throw new KeyNotFoundException("Could not find Classroom");
            }
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == classroomId);
            if (account.Id != classroom?.Account.Id && account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Unauthorized");
            int indexToRemove = Array.IndexOf(classroom.AccountIds, memberId);

            if (indexToRemove != -1)
            {
                var updatedAccountIds = classroom.AccountIds.ToList();
                updatedAccountIds.RemoveAt(indexToRemove);

                classroom.AccountIds = updatedAccountIds.ToArray();

                _context.Classrooms.Update(classroom);
                await _context.SaveChangesAsync();
            }
            _context.ClassroomsMembers.Remove(classroomMember);
            await _context.SaveChangesAsync();
        }

        public async Task SentInvitationEmail(int classroomId, BatchMemberRequest batchMemberRequest, Account account)
        {
            var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == classroomId);
            if (classroom == null)
            {
                throw new KeyNotFoundException("Could not find Classroom");
            }
            var isAllow = classroom.Account.Id == account.Id;
            if (!isAllow && classroom.isStudentAllowInvite)
            {
                isAllow = classroom.AccountIds != null && classroom.AccountIds.Contains(account.Id);
            }

            if (!isAllow)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            var memberIds = batchMemberRequest.MemberIds.Distinct();

            if (classroom.AccountIds != null)
            {
                var wasMember = memberIds.Where(id => classroom.AccountIds.Contains(id));
                if (wasMember.Any())
                {
                    throw new KeyNotFoundException($"these userId already be classroom's members:\n {wasMember}");
                }
            }

            foreach (var memberId in memberIds)
            {
                var member = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == memberId);
                if (member == null)
                {
                    throw new KeyNotFoundException($"Can not find user with Id: {memberId}");
                }

                string code = generateVerificationToken();
                var classroomCode = new ClassroomCode
                {
                    Code = code,
                    Expires = DateTime.UtcNow.AddDays(7),
                    Classroom = classroom
                };

                classroom.ClassroomCodes ??= new List<ClassroomCode>();
                classroom.ClassroomCodes.Add(classroomCode);
                _context.Classrooms.Update(classroom);
                await _context.SaveChangesAsync();

                await sendInvitationEmail(member, account, _helperFrontEnd.GetUrl($"/classrooms?code={code}"), classroom.Classname);
            }
        }

        public async Task<ClassroomResponse> UpdateClassroom(ClassroomUpdate classroomUpdate, Account account)
        {
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == classroomUpdate.Id);
            if (classroom == null)
                throw new KeyNotFoundException("Could not find Classroom");
            if (account.Id != classroom.Account.Id && account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Unauthorized");
            _mapper.Map(classroomUpdate, classroom);
            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync();
            return _mapper.Map<ClassroomResponse>(classroom);
        }

        private string generateVerificationToken()
        {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
            return token.Substring(0, 10);
        }

        private async Task sendInvitationEmail(Account toAccount, Account fromAccount, string invitationLink, string classroomName)
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var projectDirectory =
                Path.Combine(assemblyDirectory, "..", "..", "..");
            var htmlFilePath = Path.Combine(projectDirectory, "EmailTemplate", "invitation.html");
            var htmlContent = File.ReadAllText(htmlFilePath);
            htmlContent = htmlContent.Replace("{to-user-name}", toAccount.Username);
            htmlContent = htmlContent.Replace("{from-user-name}", fromAccount.Username);
            htmlContent = htmlContent.Replace("{link}", invitationLink);
            htmlContent = htmlContent.Replace("{classroom-name}", classroomName);
            htmlContent = htmlContent.Replace("{mail}", "ngocvlqt1995@gmail.com");

            await _emailService.SendAsync(
                toAccount.Email,
                "QUIZLEARN - Classroom Invitation",
                htmlContent
            );
        }

    }
}
