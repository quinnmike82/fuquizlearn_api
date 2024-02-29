﻿using AutoMapper;
using AutoMapper.Execution;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Migrations;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Models.Posts;
using fuquizlearn_api.Models.Quiz;
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
        Task AddMember(AddMember addMember, Account account);
        Task RemoveMember(int memberId, int classroomId, Account account);
        Task DeleteClassroom(int id, Account account);
        Task<ClassroomCodeResponse> GenerateClassroomCode(int classroomId, Account account);
        Task<List<ClassroomCodeResponse>> GetAllClassroomCodes(int classroomId);
        Task JoinClassroomWithCode(string classroomCode, Account account);
        Task<QuizBankResponse> AddQuizBank(int classroomId, QuizBankCreate model, Account account);
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
        public async Task AddMember(AddMember addMember, Account account)
        {
            var classRoom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == addMember.classroomId);
            if (classRoom == null)
            {
                throw new KeyNotFoundException("Could not find Classroom");
            }
            var check = await _context.ClassroomsMembers.FirstOrDefaultAsync(c => c.ClassroomId == addMember.classroomId && c.AccountId == addMember.memberId);
            if(check != null)
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
            var classroom = await _context.Classrooms.FirstOrDefaultAsync(i => i.Id == classroomId);
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

        public async Task CopyQuizBank(int quizbankId, int classroomId, Account account)
        {
            var quizBank = await _context.QuizBanks.Include(q => q.Quizes).FirstOrDefaultAsync(i => i.Id == quizbankId);
            if (quizBank == null)
                throw new KeyNotFoundException("Could not find QuizBank");
            var classroom = await _context.Classrooms.FirstOrDefaultAsync(i => i.Id == classroomId);
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
            else classroom.BankIds = new int[]{newBank.Id};
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
            var classRoom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == id);
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
            var classroom = await _context.Classrooms.FirstOrDefaultAsync( i => i.Id == classroomId);
            if(classroom == null)
                throw new KeyNotFoundException("Could not find Classroom");
            if(classroom.Account.Id != account.Id)
                throw new UnauthorizedAccessException("Unauthorized");
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

        public async Task<List<ClassroomResponse>> GetAllClassrooms()
        {
            var classrooms = await _context.Classrooms.Where(i => i.DeletedAt == null).ToListAsync();
            return _mapper.Map<List<ClassroomResponse>>(classrooms);
        }

        public async Task<List<ClassroomResponse>> GetAllClassroomsByAccountId(Account account)
        {
            var classroomsOwned = await _context.Classrooms.Where(i => i.Account.Id == account.Id).ToListAsync();
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
            var classroom = await _context.Classrooms.Include(i => i.ClassroomCodes).FirstOrDefaultAsync(i => i.ClassroomCodes.Any( c => c.Code == classroomCode));
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
            if(code.IsExpired)
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

        public async Task RemoveMember(int memberId, int classroomId,Account account)
        {
            var classroomMember = await _context.ClassroomsMembers.FirstOrDefaultAsync(i => i.AccountId == memberId && i.ClassroomId == classroomId);
            if (classroomMember == null)
            {
                throw new KeyNotFoundException("Could not find Classroom");
            }
            var classroom = await _context.Classrooms.FirstOrDefaultAsync(i => i.Id == classroomId);
            if(account.Id != classroom.Account.Id && account.Role != Role.Admin)
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

        public async Task<ClassroomResponse> UpdateClassroom(ClassroomUpdate classroomUpdate, Account account)
        {
            var classroom = await _context.Classrooms.FirstOrDefaultAsync(i => i.Id == classroomUpdate.Id);
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

    }
}