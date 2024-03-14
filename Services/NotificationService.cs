using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Notification;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace fuquizlearn_api.Services
{
    public interface INotificationService
    {
        Task<NotificationResponse> CreateNotification(NotificationCreate noti);
        Task<NotificationResponse> UpdateNotification(NotificationUpdate noti);
        Task DeleteNotification(int id, Account account);
        Task<List<NotificationResponse>> GetCurrent(Account account);
        Task<List<NotificationResponse>> GetNotificationByAccount(int Id);
        Task<NotificationResponse> ReadNotification(int id, Account account);
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
                throw new KeyNotFoundException("Not found User");
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
                throw new KeyNotFoundException("Notification not found");
            }
            if(account.Id != noti.Account?.Id && account.Role != Role.Admin)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }
            noti.Deleted = DateTime.UtcNow;
            _context.Notifications.Update(noti);
            await _context.SaveChangesAsync();
        }

        public async Task<List<NotificationResponse>> GetCurrent(Account account)
        {
            var noti = await _context.Notifications.Include(i => i.Account).Where(i => i.Account.Id == account.Id).ToListAsync();
            return _mapper.Map<List<NotificationResponse>>(noti);
        }

        public async Task<List<NotificationResponse>> GetNotificationByAccount(int Id)
        {
            var noti = await _context.Notifications.Include(i => i.Account).Where(i => i.Account.Id == Id).ToListAsync();
            return _mapper.Map<List<NotificationResponse>>(noti);
        }

        public async Task<NotificationResponse> ReadNotification(int id, Account account)
        {
            var noti = await _context.Notifications.Include(i => i.Account).FirstOrDefaultAsync(i => i.Id == id);
            if (noti == null)
            {
                throw new KeyNotFoundException("Notification not found");
            }
            if (account.Id != noti.Account?.Id && account.Role != Role.Admin)
            {
                throw new UnauthorizedAccessException("Unauthorized");
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
                throw new KeyNotFoundException("Notification not found");
            }
            noti = _mapper.Map<Notification>(notiUpdate);
            noti.Deleted = DateTime.UtcNow;
            _context.Notifications.Update(noti);
            await _context.SaveChangesAsync();
            return _mapper.Map<NotificationResponse>(noti);
        }
    }
}
