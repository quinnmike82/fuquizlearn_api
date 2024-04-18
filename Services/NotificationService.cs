using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Extensions;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Notification;
using fuquizlearn_api.Models.Posts;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;
using System.Text;
using System.Web;

namespace fuquizlearn_api.Services
{
    public interface INotificationService
    {
        Task<NotificationResponse> CreateNotification(NotificationCreate noti);
        Task<NotificationResponse> UpdateNotification(NotificationUpdate noti);
        Task DeleteNotification(int id, Account account);
        Task<PagedResponse<NotificationResponse>> GetCurrent(Account account, PagedRequest options);
        Task<PagedResponse<NotificationResponse>> GetNotificationByAccount(int Id, PagedRequest options);
        Task<NotificationResponse> ReadNotification(int id, Account account);
        Task<int> GetUnread(Account account);
        Task NotificationTrigger(List<int> userIds, string type, string description, string objectName);
        Task<PagedResponse<NotificationResponse>> GetAll(PagedRequest options, Account account);
    }
    public class NotificationService : INotificationService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public NotificationService(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<NotificationResponse> CreateNotification(NotificationCreate noti)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(i => i.Id == noti.AccountId);
            if(account == null)
            {
                throw new KeyNotFoundException("Errors.classroom.user_not_found");
            }
            var newNoti = _mapper.Map<Notification>(noti);
            newNoti.Account = account;
            _context.Notifications.Add(newNoti);
            await _context.SaveChangesAsync();
            return _mapper.Map<NotificationResponse>(newNoti);
        }

        public async Task DeleteNotification(int id, Account account)
        {
            var noti = await _context.Notifications.Include(i => i.Account).FirstOrDefaultAsync(i => i.Id == id);
            if(noti == null)
            {
                throw new KeyNotFoundException("Errors.Notification.NotFound");
            }
            if(account.Id != noti.Account?.Id && account.Role != Role.Admin)
            {
                throw new UnauthorizedAccessException("Errors.Unauthorized");
            }
            noti.Deleted = DateTime.UtcNow;
            _context.Notifications.Update(noti);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResponse<NotificationResponse>> GetCurrent(Account account, PagedRequest options)
        {
            var noti = await _context.Notifications.Include(i => i.Account).Where(i => i.Account.Id == account.Id).ToPagedAsync(options,
           x => x.Title.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));

            return new PagedResponse<NotificationResponse>
            {
                Data = _mapper.Map<IEnumerable<NotificationResponse>>(noti.Data),
                Metadata = noti.Metadata
            };
        }

        public async Task<PagedResponse<NotificationResponse>> GetNotificationByAccount(int Id, PagedRequest options)
        {
            var noti = await _context.Notifications.Include(i => i.Account).Where(i => i.Account.Id == Id).ToPagedAsync(options,
           x => x.Title.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));

            return new PagedResponse<NotificationResponse>
            {
                Data = _mapper.Map<IEnumerable<NotificationResponse>>(noti.Data),
                Metadata = noti.Metadata
            };
        }
        public async Task<PagedResponse<NotificationResponse>> GetAll(PagedRequest options, Account account)
        {
            if (account.Role != Role.Admin)
                throw new UnauthorizedAccessException();
            var noti = await _context.Notifications.Include(i => i.Account).OrderByDescending(c => c.Created).ToPagedAsync(options,
           x => x.Title.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));

            return new PagedResponse<NotificationResponse>
            {
                Data = _mapper.Map<IEnumerable<NotificationResponse>>(noti.Data),
                Metadata = noti.Metadata
            };
        }

        public async Task<int> GetUnread(Account account)
        {
            return _context.Notifications.Include(c => c.Account).Where(a => a.Account.Id == account.Id && a.Read == null).Count();
        }

        public async Task<NotificationResponse> ReadNotification(int id, Account account)
        {
            var noti = await _context.Notifications.Include(i => i.Account).FirstOrDefaultAsync(i => i.Id == id);
            if (noti == null)
            {
                throw new KeyNotFoundException("Notification not foundErrors.Notification.NotFound");
            }
            if (account.Id != noti.Account?.Id && account.Role != Role.Admin)
            {
                throw new UnauthorizedAccessException("Errors.Unauthorized");
            }
            noti.Read = DateTime.UtcNow;
            _context.Notifications.Update(noti);
            await _context.SaveChangesAsync();
            return _mapper.Map<NotificationResponse>(noti);
        }

        public async Task<NotificationResponse> UpdateNotification(NotificationUpdate notiUpdate)
        {
            var noti = await _context.Notifications.Include(i => i.Account).FirstOrDefaultAsync(i => i.Id == notiUpdate.Id);
            if (noti == null)
            {
                throw new KeyNotFoundException("Errors.Notification.NotFound");
            }
            _mapper.Map(notiUpdate,noti);
            noti.Deleted = DateTime.UtcNow;
            _context.Notifications.Update(noti);
            await _context.SaveChangesAsync();
            return _mapper.Map<NotificationResponse>(noti);
        }

        public async Task NotificationTrigger(List<int> userIds, string type, string description, string objectName)
        {
            foreach(var user in userIds)
            {
                var account = await _context.Accounts.FirstOrDefaultAsync(c => c.Id == user);
                if(account == null)
                {
                    throw new KeyNotFoundException("Errors.classroom.user_not_found");
                }
                var noti = new NotificationCreate
                {
                    AccountId = account.Id,
                    Title = description,
                    Type = type,
                    ObjectName = objectName
                };
                await CreateNotification(noti);
            }
        }

    }
}
