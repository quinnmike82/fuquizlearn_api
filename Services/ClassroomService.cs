using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Models.Posts;
using fuquizlearn_api.Models.QuizBank;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;

namespace fuquizlearn_api.Services
{
    public interface IClassroomService
    {
        Task<ClassroomResponse> CreateClassroom(ClassroomCreate classroom, Account account);
        Task<ClassroomResponse> GetClassroomById(int id);
        Task<List<ClassroomResponse>> GetAllClassrooms();
        Task<List<ClassroomResponse>> GetAllClassroomsByAccountId(Account acncout);
        Task<ClassroomResponse> UpdateClassroom(ClassroomUpdate classroomUpdate, Account account);
        Task AddMember(AddMember addMember);
        Task RemoveMember(int memberId, int classroomId, Account account);
        Task DeleteClassroom(int id, Account account);
        Task<ClassroomCode> GenerateClassroomCode(int classroomId, Account account);
        Task<List<ClassroomCode>> GetAllClassroomCodes(int classroomId);
        Task JoinClassroomWithCode(string classroomCode, Account account);
        Task<QuizBankResponse> AddQuizBank(QuizBankCreate model, Account account);
        Task CopyQuizBank(int quizbankId, int classroomId, Account account);
    }
    public class ClassroomService : IClassroomService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public ClassroomService(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task AddMember(AddMember addMember)
        {
            var classRoom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == addMember.classroomId);
            if (classRoom == null)
            {
                throw new KeyNotFoundException("Could not find Classroom");
            }
            var check = await _context.ClassroomsMembers.FirstOrDefaultAsync(c => c.ClassroomId == addMember.classroomId && c.AccountId == addMember.memberId);
            if(check != null)
                throw new InvalidOperationException("ClassroomMember already exists.");
            if (classRoom.OwnerId != addMember.currentAccountId)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }
            var classroomMember = new ClassroomMember
            {
                AccountId = addMember.memberId,
                ClassroomId = addMember.classroomId
            };
            _context.ClassroomsMembers.Add(classroomMember);
            await _context.SaveChangesAsync();
        }

        public async Task<QuizBankResponse> AddQuizBank(QuizBankCreate model, Account account)
        {
            if (model.Visibility == null) model.Visibility = Visibility.Public;

            var quizBank = _mapper.Map<QuizBank>(model);
            quizBank.Created = DateTime.UtcNow;
            quizBank.Author = account;
            _context.QuizBanks.Add(quizBank);
            await _context.SaveChangesAsync();

            return _mapper.Map<QuizBankResponse>(quizBank);
        }

        public async Task CopyQuizBank(int quizbankId, int classroomId, Account account)
        {
            var quizBank = await _context.QuizBanks.FirstOrDefaultAsync(i => i.Id == quizbankId);
            if (quizBank == null)
                throw new KeyNotFoundException("Could not find QuizBank");
            var classroom = await _context.Classrooms.FirstOrDefaultAsync(i => i.Id == classroomId);
            if (classroom == null)
                throw new KeyNotFoundException("Cound not find Classroom");
            if (account.Id != classroom.OwnerId && account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Unauthorized");
            var newBank = new QuizBank
            {
                BankName = quizBank.BankName,
                Description = quizBank.Description,
                Visibility = quizBank.Visibility,
                Quizes = quizBank.Quizes
            };
            var newQuizes = new List<Quiz>();
            foreach (var quiz in quizBank.Quizes)
            {
                var newQuiz = new Quiz
                {
                    Answer = quiz.Answer,
                    Explaination = quiz.Explaination,
                    Question = quiz.Question,
                    QuizBankId = quiz.QuizBankId
                };
                _context.Add(newQuiz);
                newQuizes.Add(newQuiz);
            }
            newBank.Quizes = newQuizes;
            _context.QuizBanks.Add(newBank);
            classroom.BankIds.Append(newBank.Id);
            _context.QuizBanks.Update(quizBank);
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
            var classRoom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == id);
            if (classRoom == null)
            {
                throw new KeyNotFoundException("Could not find Classroom");
            }
            if (!(classRoom.OwnerId == account.Id || account.Role != Role.Admin))
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }
            classRoom.DeletedAt = DateTime.UtcNow;
            _context.Classrooms.Update(classRoom);
            await _context.SaveChangesAsync();
        }

        public async Task<ClassroomCode> GenerateClassroomCode(int classroomId, Account account)
        {
            var classroom = await _context.Classrooms.FirstOrDefaultAsync( i => i.Id == classroomId);
            if(classroom == null)
                throw new KeyNotFoundException("Could not find Classroom");
            if(classroom.OwnerId != account.Id)
                throw new UnauthorizedAccessException("Unauthorized");
            string code = generateVerificationToken();
            var classroomCode = new ClassroomCode
            {
                Code = code,
                Expires = DateTime.UtcNow.AddDays(7),
            };
            classroom.ClassroomCodes.Add(classroomCode);
            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync();
            return classroomCode;
        }

        public async Task<List<ClassroomCode>> GetAllClassroomCodes(int classroomId)
        {
            var classroom = await _context.Classrooms.FirstOrDefaultAsync(i => i.Id == classroomId && i.DeletedAt == null);
            if (classroom == null)
                throw new KeyNotFoundException("Could not find Classroom");
            return classroom.ClassroomCodes;
        }

        public async Task<List<ClassroomResponse>> GetAllClassrooms()
        {
            var classrooms = await _context.Classrooms.Where(i => i.DeletedAt == null).ToListAsync();
            return _mapper.Map<List<ClassroomResponse>>(classrooms);
        }

        public async Task<List<ClassroomResponse>> GetAllClassroomsByAccountId(Account account)
        {
            var classroomsOwned = await _context.Classrooms.Where(i => i.OwnerId == account.Id).ToListAsync();
            var classroomsJoined = await _context.ClassroomsMembers
                                                     .Where(i => i.AccountId == account.Id)
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

        public async Task JoinClassroomWithCode(string classroomCode, Account account)
        {
            var classroom = await _context.Classrooms.FirstOrDefaultAsync(i => i.ClassroomCodes.Any( c => c.Code == classroomCode));
            if (classroom == null)
            {
                throw new KeyNotFoundException("Could not find Classroom");
            }
            var code = classroom.ClassroomCodes.Single(i => i.Code == classroomCode);
            if(code.IsExpired)
                throw new AppException("Invalid Code");
            var classroomMember = new ClassroomMember
            {
                AccountId = account.Id,
                ClassroomId = classroom.Id
            };
            _context.ClassroomsMembers.Add(classroomMember);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveMember(int memberId, int classroomId,Account account)
        {
            var classroomMember = await _context.ClassroomsMembers.FirstOrDefaultAsync(i => i.AccountId == memberId && i.ClassroomId == classroomId);
            if (classroomMember == null)
            {
                throw new KeyNotFoundException("Could not find Classroom");
            }
            var classroom = await _context.Classrooms.FirstOrDefaultAsync(i => i.Id == classroomId);
            if(account.Id != classroom.OwnerId && account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Unauthorized");
            _context.ClassroomsMembers.Remove(classroomMember);
            await _context.SaveChangesAsync();
        }

        public async Task<ClassroomResponse> UpdateClassroom(ClassroomUpdate classroomUpdate, Account account)
        {
            var classroom = await _context.Classrooms.FirstOrDefaultAsync(i => i.Id == classroomUpdate.Id);
            if (account.Id != classroom.OwnerId && account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Unauthorized");
            var newUpdate = _mapper.Map<Classroom>(classroomUpdate);
            _context.Classrooms.Update(newUpdate);
            await _context.SaveChangesAsync();
            return _mapper.Map<ClassroomResponse>(newUpdate);
        }

        private string generateVerificationToken()
        {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
            return token;
        }
    }
}
