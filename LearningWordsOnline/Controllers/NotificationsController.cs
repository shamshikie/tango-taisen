using LearningWordsOnline.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using LearningWordsOnline.Data;
using Microsoft.EntityFrameworkCore;
using LearningWordsOnline.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;


namespace LearningWordsOnline.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : Controller
    {
        private readonly LearningWordsOnlineDbContext _appContext;

        public NotificationsController(LearningWordsOnlineDbContext appContext)
        {
            _appContext = appContext;
        }

        //[HttpGet("GetNotifications")]
        //public async Task<IActionResult> GetNotifications(string aspNetUserId)
        //{
        //    var notifications = await _viewComponentHelper.InvokeAsync("Notifications", new { aspNetUserId = aspNetUserId });
        //    //Component.InvokeAsync("Notifications", new { aspNetUserId = aspNetUserId })
        //    return Ok(notifications.ToString()); // HTML を返す
        //}
        [HttpGet("GetNotifications")]
        public async Task<IActionResult> GetNotifications(string timeZone)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            List<NotificationItemViewModel> notificationItemViewModels = new();

            var appUser = await _appContext.AppUsers
                .Include(a => a.ReceivedRequests)
                .Include(a => a.ReceivedInvitations)
                .FirstOrDefaultAsync(a => a.AspNetUserId == userId);

            if (appUser is null)
            {
                throw new NullReferenceException("存在しないaspNetUserIdです。");
            }

            // クライアントから送られてきたタイムゾーンを使用
            var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);

            foreach (var request in appUser.ReceivedRequests)
            {
                var sender = await _appContext.AppUsers
                    .Include(a => a.Profile).ThenInclude(p => p.Icon)
                    .FirstOrDefaultAsync(a => a.Id == request.AppUserId1);

                if (sender is null)
                    continue;

                var localCreatedAt = TimeZoneInfo.ConvertTimeFromUtc(request.CreatedAt, localTimeZone);

                notificationItemViewModels.Add(new NotificationItemViewModel
                {
                    //Icon = profile?.Icon,
                    Sender = sender,
                    NotificationType = NotificationType.FriendRequest,
                    CreatedAt = localCreatedAt,
                    Text = request.Id.ToString(),
                    IsReferenced = request.ReferencedAt != null,
                    IsDone = request.RespondedAt != null
                });
            }

            foreach (var request in appUser.ReceivedInvitations)
            {
                var sender = await _appContext.AppUsers
                    .Include(a => a.Profile).ThenInclude(p => p.Icon)
                    .FirstOrDefaultAsync(a => a.Id == request.AppUserId1);
                if (sender is null)
                    continue;

                var localCreatedAt = TimeZoneInfo.ConvertTimeFromUtc(request.CreatedAt, localTimeZone);
                notificationItemViewModels.Add(new NotificationItemViewModel
                {
                    Sender = sender,
                    NotificationType = NotificationType.RoomInvitation,
                    CreatedAt = localCreatedAt,
                    Text = request.RoomId,
                    IsReferenced = request.ReferencedAt != null,
                    IsDone = request.DismissedAt != null
                });
            }

            // 日付順でソート
            notificationItemViewModels = notificationItemViewModels
                .OrderByDescending(n => n.CreatedAt).ToList();

            MarkNewNotificationsAsRead(appUser);

            return PartialView("_NotificationsPartial", notificationItemViewModels);
        }

        [HttpGet("GetNewNotificationCount")]
        public async Task<IActionResult> GetNewNotificationCount(string aspNetUserId)
        {
            List<NotificationItemViewModel> notificationItemViewModels = new();

            var appUser = await _appContext.AppUsers
                .Include(a => a.ReceivedRequests)
                .Include(a => a.ReceivedInvitations)
                .FirstOrDefaultAsync(a => a.AspNetUserId == aspNetUserId);

            if (appUser is null)
            {
                throw new NullReferenceException("存在しないaspNetUserIdです。");
            }

            var newRequestCount = appUser.ReceivedRequests.Where(r => r.ReferencedAt is null).Count();
            var newInvitationCount = appUser.ReceivedInvitations.Where(r => r.ReferencedAt is null).Count();

            return Ok(newRequestCount + newInvitationCount);
        }

        private void MarkNewNotificationsAsRead(AppUser appUser)
        {
            appUser.ReceivedRequests.Where(r => r.ReferencedAt is null).ToList()
                .ForEach(r => r.ReferencedAt = DateTime.UtcNow);

            appUser.ReceivedInvitations.Where(r => r.ReferencedAt is null).ToList()
                .ForEach(r => r.ReferencedAt = DateTime.UtcNow);
            try
            {
                _appContext.Update(appUser);
                _appContext.SaveChanges();
            }
            catch (Exception ex)
            {
                // エラー発生時の処理
                Console.WriteLine($"データベースエラーが発生しました: {ex.Message}");
            }
        }
    }
}
