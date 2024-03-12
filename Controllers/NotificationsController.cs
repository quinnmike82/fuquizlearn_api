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
            try
            {
                var newNotification = await _notificationService.CreateNotification(noti);
                return Ok(newNotification);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id, [FromQuery] int accountId)
        {
            try
            {
                var account = new Account { Id = accountId }; // Assuming you get the account id from somewhere
                await _notificationService.DeleteNotification(id, account);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{accountId}")]
        public async Task<IActionResult> GetCurrentNotifications(int accountId)
        {
            try
            {
                var account = new Account { Id = accountId }; // Assuming you get the account id from somewhere
                var notifications = await _notificationService.GetCurrent(account);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("account/{accountId}")]
        public async Task<IActionResult> GetNotificationsByAccount(int accountId)
        {
            try
            {
                var notifications = await _notificationService.GetNotificationByAccount(accountId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateNotification(NotificationUpdate noti)
        {
            try
            {
                var updatedNotification = await _notificationService.UpdateNotification(noti);
                return Ok(updatedNotification);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkNotificationAsRead(int id, [FromQuery] int accountId)
        {
            try
            {
                var account = new Account { Id = accountId }; // Assuming you get the account id from somewhere
                var readNotification = await _notificationService.ReadNotification(id, account);
                return Ok(readNotification);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
