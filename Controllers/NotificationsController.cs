using fuquizlearn_api.Authorization;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Notification;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Services;
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
        [Authorize]
        public async Task<IActionResult> CreateNotification([FromBody] NotificationCreate noti)
        {
                var newNotification = await _notificationService.CreateNotification(noti);
                return Ok(newNotification);
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
                await _notificationService.DeleteNotification(id, Account);
                return NoContent();
        }
        [Authorize]
        [HttpGet("GetCurrentNotifications")]
        public async Task<IActionResult> GetCurrentNotifications([FromQuery] PagedRequest options)
        {
                var notifications = await _notificationService.GetCurrent(Account, options);
                return Ok(notifications);
        }
        [Authorize]
        [HttpGet("account/{accountId}")]
        public async Task<IActionResult> GetNotificationsByAccount(int accountId, [FromQuery] PagedRequest options)
        {
                var notifications = await _notificationService.GetNotificationByAccount(accountId, options);
                return Ok(notifications);
        }
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateNotification(NotificationUpdate noti)
        {
                var updatedNotification = await _notificationService.UpdateNotification(noti);
                return Ok(updatedNotification);
        }
        [Authorize]
        [HttpPost("read/{id}")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
                var readNotification = await _notificationService.ReadNotification(id, Account);
                return Ok(readNotification);
        }
        [Authorize]
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread()
        {
            var notifications = await _notificationService.GetUnread(Account);
            return Ok(notifications);
        }
    }
}
