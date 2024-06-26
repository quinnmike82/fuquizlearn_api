﻿using AutoMapper;
using AutoMapper.Execution;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Extensions;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Accounts;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Models.Quiz;
using fuquizlearn_api.Models.QuizBank;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Web;

namespace fuquizlearn_api.Services
{
    public interface IClassroomService
    {
        Task<ClassroomResponse> CreateClassroom(ClassroomCreate classroom, Account account);
        Task<ClassroomResponse> GetClassroomById(int id, Account account);
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
        Task CopyQuizBank(int quizbankId, int classroomId, string newName, Account account);
        Task BatchRemoveMember(int classroomId, Account account, List<int> memberIds);
        Task BatchAddMember(int classroomId, Account account, List<int> memberIds);
        Task SentInvitationEmail(int classroomId, BatchMemberRequest batchMemberRequest, Account account);
        Task<PagedResponse<ClassroomResponse>> GetCurrentJoinedClassroom(PagedRequest options, Account account);
        Task<PagedResponse<ClassroomMemberResponse>> GetAllMember(int id, Account account, PagedRequest options);
        Task BanMember(int id, BatchMemberRequest membersRequest, Account account);
        Task UnbanMember(int id, BatchMemberRequest membersRequest, Account account);
        Task<PagedResponse<AccountResponse>> GetBanAccounts(int id, PagedRequest options);
        Task LeaveClassroom(int id, Account account);
        Task<PagedResponse<QuizBankResponse>> GetAllBankFromClass(int classroomId, PagedRequest options, Account account);
    }
    public class ClassroomService : IClassroomService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IHelperFrontEnd _helperFrontEnd;
        private readonly INotificationService _notificationService;

        public ClassroomService(DataContext context, IMapper mapper, IHelperFrontEnd helperFrontEnd, IEmailService emailService, INotificationService notificationService)
        {
            _context = context;
            _mapper = mapper;
            _helperFrontEnd = helperFrontEnd;
            _emailService = emailService;
            _notificationService = notificationService;
        }
        public async Task AddMember(AddMember addMember, Account account)
        {
            var classRoom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(c => c.Id == addMember.classroomId && c.DeletedAt == null);
            if (classRoom == null)
            {
                throw new KeyNotFoundException("classroom.not_found");
            }
            var banmembers = Array.IndexOf(classRoom.BanMembers, addMember.memberId);
            if (banmembers != -1)
            {
                throw new AppException("UserAdmin.success.ban_user.index");
            }
            var check = await _context.ClassroomsMembers.FirstOrDefaultAsync(c => c.ClassroomId == addMember.classroomId && c.AccountId == addMember.memberId);
            if (check != null)
                throw new AppException("classroom.ExistedMember");
            if (classRoom.Account.Id != account.Id)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }
            await CheckMember(classRoom, 1);
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
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == classroomId && i.DeletedAt == null);
            if (classroom == null)
                throw new KeyNotFoundException("classroom.not_found");
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

        public async Task BanMember(int id, int memberId, Account account)
        {
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);
            if (classroom == null)
                throw new KeyNotFoundException("classroom.not_found");
            if (memberId != classroom.Account.Id && account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Unauthorized");
            var banmembers = Array.IndexOf(classroom.BanMembers, memberId);
            if (banmembers != -1)
            {
                throw new AppException("classroom.AlreadyBanned");
            }
            if (classroom.BanMembers == null)
            {
                classroom.BanMembers = new int[] { memberId };
            }
            else
            {
                classroom.BanMembers = classroom.BanMembers.Append(memberId).ToArray();
            }
            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync();
            var member = banmembers = Array.IndexOf(classroom.AccountIds, memberId);
            if (member != -1)
            {
                await RemoveMember(memberId, id, account);
            }
            await _notificationService.NotificationTrigger(new List<int> { memberId }, "Aleart", "ban_classroom", classroom.Classname);

        }

        public async Task BanMember(int id, BatchMemberRequest membersRequest, Account account)
        {
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);
            if (classroom == null)
                throw new KeyNotFoundException("classroom.not_found");
            if (account.Id != classroom.Account.Id && account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Unauthorized");
            var banmembers = classroom.BanMembers.Intersect(membersRequest.MemberIds).Any();
            if (banmembers)
            {
                throw new AppException("classroom.AlreadyBanned");
            }
            if (classroom.BanMembers == null)
            {
                classroom.BanMembers = membersRequest.MemberIds.ToArray();
            }
            else
            {
                var newBans = classroom.BanMembers.ToList();
                newBans.AddRange(membersRequest.MemberIds);
                classroom.BanMembers = newBans.ToArray();
            }
            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync();
            var remove = classroom.AccountIds.ToList().Intersect(membersRequest.MemberIds);
            if (remove.Count() > 0)
            {
                await BatchRemoveMember(classroom.Id, account,remove.ToList());
            }

        }

        public async Task BatchAddMember(int classroomId, Account account, List<int> memberIds)
        {
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == classroomId && i.DeletedAt == null);
            if (classroom == null)
                throw new KeyNotFoundException("classroom.not_found");
            if (!(classroom.Account.Id == account.Id || account.Role != Role.Admin))
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }
            if ((bool)(classroom.BanMembers?.Intersect(memberIds).Any()))
            {
                throw new AppException("classroom.AlreadyBanned");
            }
            await CheckMember(classroom, memberIds.Count());
            memberIds = memberIds.Distinct().ToList();

            if (classroom.AccountIds != null)
            {
                var wasMember = memberIds.Where(id => classroom.AccountIds.Contains(id));
                if (wasMember.Any())
                {
                    throw new KeyNotFoundException($"classroom.ExistedMember");
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
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == classroomId && i.DeletedAt == null);
            if (classroom == null)
                throw new KeyNotFoundException("classroom.not_found");
            if (!(classroom.Account.Id == account.Id || account.Role != Role.Admin))
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            if (classroom.AccountIds == null)
            {
                throw new AppException("classroom.NoMember");
            }

            var isNotMember = memberIds.Where(id => !classroom.AccountIds.Contains(id));

            if (isNotMember.Any())
            {
                throw new KeyNotFoundException($"classroom.user_not_found");
            }

            classroom.AccountIds = classroom.AccountIds.Where(id => !memberIds.Contains(id)).ToArray();
            var classroomMembers = await _context.ClassroomsMembers.Where(cm => cm.ClassroomId == classroomId && memberIds.Contains(cm.AccountId)).ToArrayAsync();
            _context.ClassroomsMembers.RemoveRange(classroomMembers);
            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync();
            await _notificationService.NotificationTrigger(memberIds, "Aleart", "kick_classroom", classroom.Classname);
        }

        public async Task CopyQuizBank(int quizbankId, int classroomId, string newName, Account account)
        {
            var quizBank = await _context.QuizBanks.Include(q => q.Quizes).FirstOrDefaultAsync(i => i.Id == quizbankId);
            if (quizBank == null)
                throw new KeyNotFoundException("Quizbank.NotFound");
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == classroomId && i.DeletedAt == null);
            if (classroom == null)
                throw new KeyNotFoundException("classroom.not_found");
            if (account.Id != classroom.Account.Id && account.Role != Role.Admin && Array.IndexOf(classroom.AccountIds,account.Id) == -1)
                throw new UnauthorizedAccessException("Unauthorized");
            
            var newQuizBankName = newName.Trim() != "" ? newName : quizBank.BankName;
            var newBank = new QuizBank
            {
                BankName = newQuizBankName,
                Description = quizBank.Description,
                Visibility = quizBank.Visibility,
                Author = account,
                Tags = quizBank.Tags
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
                classroom.BankIds = classroom.BankIds.Append(newBank.Id).ToArray();
            else classroom.BankIds = new int[] { newBank.Id };
            _context.QuizBanks.Update(newBank);
            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync();
        }

        public async Task<ClassroomResponse> CreateClassroom(ClassroomCreate classroom, Account account)
        {
            if(account.Role != Role.Admin)
                await CheckMaxClassroom(account);
            var newClass = _mapper.Map<Classroom>(classroom);
            newClass.Account = account;
            _context.Classrooms.Add(newClass);
            await _context.SaveChangesAsync();
            return _mapper.Map<ClassroomResponse>(newClass);
        }

        public async Task DeleteClassroom(int id, Account account)
        {
            var classRoom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);
            if (classRoom == null)
            {
                throw new KeyNotFoundException("classroom.not_found");
            }
            if (classRoom.Account.Id != account.Id && account.Role != Role.Admin)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }
            classRoom.DeletedAt = DateTime.UtcNow;
            _context.Classrooms.Update(classRoom);
            await _context.SaveChangesAsync();
        }

        public async Task<ClassroomCodeResponse> GenerateClassroomCode(int classroomId, Account account)   
        {
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == classroomId && i.DeletedAt == null);
            if (classroom == null)
                throw new KeyNotFoundException("classroom.not_found");
            var isAllow = classroom.Account.Id == account.Id;
            if (!isAllow && classroom.isStudentAllowInvite)
            {
                isAllow = classroom.AccountIds != null && classroom.AccountIds.Contains(account.Id);
            }
            if (!isAllow) throw new UnauthorizedAccessException("Unauthorized");
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

        public async Task<PagedResponse<QuizBankResponse>> GetAllBankFromClass(int classroomId, PagedRequest options, Account account)
        {
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == classroomId && i.DeletedAt == null);
            if (classroom == null)
                throw new KeyNotFoundException("classroom.not_found");
            var isAllow = classroom.Account.Id == account.Id;
            if (!isAllow)
            {
                isAllow = classroom.AccountIds.Contains(account.Id) || account.Role == Role.Admin;
            }
            if (!isAllow) throw new UnauthorizedAccessException("Unauthorized");

            var result = new PagedResponse<QuizBankResponse>
            {
                Data = new List<QuizBankResponse>(),
                Metadata = new PagedMetadata(options.Skip,options.Take,0,false)
            };

            if (classroom.BankIds != null)
            {
                var quizbanks = await _context.QuizBanks.Include(c => c.Author).Include(q => q.Quizes).Where(q => classroom.BankIds.Contains(q.Id) && q.DeletedAt == null)
                    .ToPagedAsync(options,
                    x => x.BankName.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));
                result.Data = _mapper.Map<List<QuizBankResponse>>(quizbanks.Data);
                result.Metadata = quizbanks.Metadata;
            }
            return result;

        }

        public async Task<List<ClassroomCodeResponse>> GetAllClassroomCodes(int classroomId)
        {
            var classroom = await _context.Classrooms.Include(i => i.ClassroomCodes).FirstOrDefaultAsync(i => i.Id == classroomId && i.DeletedAt == null);
            if (classroom == null)
                throw new KeyNotFoundException("classroom.not_found");
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
            var classroomsOwned = await _context.Classrooms
                                                 .Include(c => c.Account)
                                                 .Where(i => i.Account.Id == account.Id && i.DeletedAt == null)
                                                 .ToPagedAsync(options,
                                                    x => x.Classname.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));

            var classroomsJoined = await _context.ClassroomsMembers
                                              .Include(cm => cm.Classroom)
                                                  .ThenInclude(c => c.Account)
                                              .Where(cm => cm.AccountId == account.Id && cm.Classroom.DeletedAt == null)
                                              .Select(cm => cm.Classroom)
                                              .ToPagedAsync(options,
                                                    x => x.Classname.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));

            var mergedData = classroomsOwned.Data.Concat(classroomsJoined.Data);

            return new PagedResponse<ClassroomResponse>
            {
                Data = _mapper.Map<IEnumerable<ClassroomResponse>>(mergedData),
                Metadata = classroomsOwned.Metadata
            };
        }

        public async Task<List<ClassroomResponse>> GetAllClassroomsByUserId(int id)
        {
            var classroomsOwned = await _context.Classrooms.Include(c => c.Account).Where(i => i.Account.Id == id && i.DeletedAt == null).ToListAsync();
            var classroomsJoined = await _context.ClassroomsMembers
                                                     .Include(i => i.Classroom).ThenInclude(c => c.Account)
                                                     .Where(i => i.AccountId == id && i.Classroom.DeletedAt == null)
                                                     .Select(cm => cm.Classroom)
                                                     .ToListAsync();

            var allClassrooms = classroomsOwned.Concat(classroomsJoined).ToList();
            return _mapper.Map<List<ClassroomResponse>>(allClassrooms);
        }

        public async Task<PagedResponse<ClassroomMemberResponse>> GetAllMember(int id, Account account, PagedRequest options)
        {
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);
            if (classroom == null)
            {
                throw new KeyNotFoundException("classroom.not_found");
            }
            var permission = classroom.Account.Id == account.Id 
                || (classroom.AccountIds != null && classroom.AccountIds.Contains(account.Id))
                || account.Role == Role.Admin;
            if (!permission)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }
            var members = await _context.ClassroomsMembers.Include(cm => cm.Account).Include(cm => cm.Classroom)
                                                    .Where(cm => cm.ClassroomId == id)
                                                    .ToPagedAsync(options,
                                                    mb => mb.Account.FullName.ToLower().Contains(HttpUtility.UrlDecode(options.Search.ToLower(), Encoding.ASCII).ToLower()));
            return new PagedResponse<ClassroomMemberResponse>
            {
                Data = _mapper.Map<List<ClassroomMemberResponse>>(members.Data),
                Metadata = members.Metadata
            };
        }

        public async Task<PagedResponse<AccountResponse>> GetBanAccounts(int id, PagedRequest options)
        {
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(c => c.Id == id);
            if (classroom == null)
            {
                throw new KeyNotFoundException("classroom.not_found");
            }

            var banUsers = await _context.Accounts.Where(c => classroom.BanMembers.Contains(c.Id))
                                                     .ToPagedAsync(options,
                x => x.FullName.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));

            return new PagedResponse<AccountResponse>
            {
                Data = _mapper.Map<IEnumerable<AccountResponse>>(banUsers.Data),
                Metadata = banUsers.Metadata
            };
        }

        public async Task<ClassroomResponse> GetClassroomById(int id, Account account)
        {
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);
            if(Array.IndexOf(classroom.AccountIds, account.Id) == -1 && account.Id != classroom.Account.Id)
            {
                throw new AppException("classroom.not_found");
            }
            return _mapper.Map<ClassroomResponse>(classroom);
        }

        public async Task<PagedResponse<ClassroomResponse>> GetCurrentJoinedClassroom(PagedRequest options, Account account)
        {
            var classroomsJoined = await _context.ClassroomsMembers.Include(i => i.Classroom).ThenInclude(c => c.Account)
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
            var classroom = await _context.Classrooms.Include(i => i.ClassroomCodes).Include(c => c.Account).FirstOrDefaultAsync(i => i.ClassroomCodes.Any(c => c.Code == classroomCode) && i.DeletedAt == null);
            if (classroom == null)
            {
                throw new KeyNotFoundException("classroom.not_found");
            }
            var banmembers = Array.IndexOf(classroom.BanMembers, account.Id);
            if (banmembers != -1)
            {
                throw new AppException("classroom.AlreadyBanned");
            }
            var check = await _context.ClassroomsMembers.FirstOrDefaultAsync(i => i.ClassroomId == classroom.Id && i.AccountId == account.Id);
            if (check != null)
            {
                throw new AppException("classroom.ExistedMember");
            }
            var code = classroom.ClassroomCodes.Single(i => i.Code == classroomCode);
            if (code.IsExpired)
                throw new AppException("classroom.code-invalid");
            await CheckMember(classroom, 1);
            if(classroom.Account.Id == account.Id)
            {
                throw new AppException("classroom.ExistedMember");
            }
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

        public async Task LeaveClassroom(int id, Account account)
        {
            var classroomMember = await _context.ClassroomsMembers.FirstOrDefaultAsync(i => i.AccountId == account.Id && i.ClassroomId == id);
            if (classroomMember == null)
            {
                throw new KeyNotFoundException("classroom.not_found");
            }
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == id);
            var ids = classroom.AccountIds.ToList();
            ids.Remove(account.Id);
            classroom.AccountIds = ids.ToArray();

            _context.ClassroomsMembers.Remove(classroomMember);
            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveMember(int memberId, int classroomId, Account account)
        {
            var classroomMember = await _context.ClassroomsMembers.FirstOrDefaultAsync(i => i.AccountId == memberId && i.ClassroomId == classroomId);
            if (classroomMember == null)
            {
                throw new KeyNotFoundException("classroom.not_found");
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
            await _notificationService.NotificationTrigger(new List<int> { memberId }, "Aleart", "kick_classroom", classroom.Classname);
        }

        public async Task SentInvitationEmail(int classroomId, BatchMemberRequest batchMemberRequest, Account account)
        {
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(c => c.Id == classroomId && c.DeletedAt == null);
            if (classroom == null)
            {
                throw new KeyNotFoundException("classroom.not_found");
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
                    throw new KeyNotFoundException($"classroom.ExistedMember");
                }
            }

            foreach (var memberId in memberIds)
            {
                var member = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == memberId);
                if (member == null)
                {
                    throw new KeyNotFoundException($"classroom.user_not_found");
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
                await _notificationService.NotificationTrigger(new List<int> { memberId }, "Notification", "invite_classroom", classroom.Classname);
                await sendInvitationEmail(member, account, _helperFrontEnd.GetUrl($"/classrooms?code={code}"), classroom.Classname);
            }
        }

        public async Task UnbanMember(int id, BatchMemberRequest membersRequest, Account account)
        {
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == id);
            if (classroom == null)
                throw new KeyNotFoundException("classroom.not_found");
            if (account.Id != classroom.Account.Id && account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Unauthorized");
            var banmembers = membersRequest.MemberIds.Where(item => !classroom.BanMembers.Contains(item));
            if (banmembers.Count() > 0)
            {
                throw new AppException("classroom.AlreadyBanned");
            } 
            var updatedAccountIds = classroom.BanMembers.ToList();
            foreach (var member in membersRequest.MemberIds)
            {
                updatedAccountIds.Remove(member);
            }
            classroom.BanMembers = updatedAccountIds.ToArray();
            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync();
        }

        public async Task<ClassroomResponse> UpdateClassroom(ClassroomUpdate classroomUpdate, Account account)
        {
            var classroom = await _context.Classrooms.Include(c => c.Account).FirstOrDefaultAsync(i => i.Id == classroomUpdate.Id);
            if (classroom == null)
                throw new KeyNotFoundException("classroom.not_found");
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


        private async Task CheckMember(Classroom classroom, int member)
        {
            var plan = await _context.PlanAccounts.Include(c => c.Account).Include(c => c.Plan).FirstOrDefaultAsync(c => c.Account.Id == classroom.Account.Id && c.Cancelled == null);
            if(plan != null)
            {
                if(plan.Plan.MaxStudent < (classroom.AccountIds.Count() + member))
                {
                    throw new AppException("Settings.plans.plans.maxStudent");
                }
            }
            if(classroom.AccountIds.Count() + member > 15)
            {
                throw new AppException("Settings.plans.plans.maxStudent");
            }
        }
        
        private async Task CheckMaxClassroom(Account account)
        {
            var classrooms = await _context.Classrooms.Where(c => c.Account.Id == account.Id && c.DeletedAt == null).CountAsync();
            var plan = await _context.PlanAccounts.Include(c => c.Account).Include(c => c.Plan).FirstOrDefaultAsync(c => c.Account.Id == account.Id && c.Cancelled == null);
            if (plan != null)
            {
                if (plan.Plan.MaxClassroom < (classrooms + 1))
                {
                    throw new AppException("classroom.max_classrooms");
                }
            }
            if (classrooms + 1 > 3)
            {
                throw new AppException("classroom.max_classrooms");
            }
        }
    }
}
