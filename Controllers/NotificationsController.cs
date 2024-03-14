using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Notification;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace fuquizlearn_api.Controllers
{
    [Authorize]
    [ApiController]
    public class NotificationController : BaseController
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateNotification(NotificationCreate noti)
        {
                var newNotification = await _notificationService.CreateNotification(noti);
                return Ok(newNotification);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
                await _notificationService.DeleteNotification(id, Account);
                return NoContent();
        }

        [HttpGet("GetCurrentNotifications")]
        public async Task<IActionResult> GetCurrentNotifications()
        {
                var notifications = await _notificationService.GetCurrent(Account);
                return Ok(notifications);
        }

        [HttpGet("account/{accountId}")]
        public async Task<IActionResult> GetNotificationsByAccount(int accountId)
        {
                var notifications = await _notificationService.GetNotificationByAccount(accountId);
                return Ok(notifications);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateNotification(NotificationUpdate noti)
        {
                var updatedNotification = await _notificationService.UpdateNotification(noti);
                return Ok(updatedNotification);
        }

        [HttpPut("read/{id}")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
                var readNotification = await _notificationService.ReadNotification(id, Account);
                return Ok(readNotification);
        }
    }
}
