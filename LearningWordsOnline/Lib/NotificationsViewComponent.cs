using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using LearningWordsOnline.Data;
using System;
using LearningWordsOnline.ViewModels;

namespace LearningWordsOnline.Lib
{
    public class NotificationsViewComponent : ViewComponent
    {
        private readonly LearningWordsOnlineDbContext _appContext;

        public NotificationsViewComponent(LearningWordsOnlineDbContext appContext)
        {
            _appContext = appContext;
        }

        public async Task<IViewComponentResult> InvokeAsync(string aspNetUserId)
        {
            List<NotificationItemViewModel> notificationItemViewModels = new();

            var appUser = await _appContext.AppUsers
                .Include(a => a.ReceivedRequests)
                .Include(a => a.ReceivedInvitations)
                .FirstOrDefaultAsync(a => a.AspNetUserId == aspNetUserId);

            if (appUser is null)
            {
                return View();
            }

            foreach (var request in appUser.ReceivedRequests)
            {
                var sender = await _appContext.AppUsers
                    .Include(a => a.Profile).ThenInclude(p => p.Icon)
                    .FirstOrDefaultAsync(a => a.Id == request.AppUserId1);
                //var profile = await _appContext.Profiles.Include(p => p.Icon)
                //    .FirstOrDefaultAsync(p => p.AppUserId == request.AppUserId1);

                if (sender is null)
                    continue;

                notificationItemViewModels.Add(new NotificationItemViewModel
                {
                    //Icon = profile?.Icon,
                    Sender = sender,
                    NotificationType = NotificationType.FriendRequest,
                    CreatedAt = request.CreatedAt,
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
                //var profile = await _appContext.Profiles.Include(p => p.Icon)
                //    .FirstOrDefaultAsync(p => p.AppUserId == request.AppUserId1);
                if (sender is null)
                    continue;
                notificationItemViewModels.Add(new NotificationItemViewModel
                {
                    //Icon = profile?.Icon,
                    Sender = sender,
                    NotificationType = NotificationType.RoomInvitation,
                    CreatedAt = request.CreatedAt,
                    Text = request.RoomId,
                    IsReferenced = request.ReferencedAt != null,
                    IsDone = request.DismissedAt != null
                });
            }

            // 日付順でソート
            notificationItemViewModels = notificationItemViewModels
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            return View(notificationItemViewModels);
        }
    }
}
