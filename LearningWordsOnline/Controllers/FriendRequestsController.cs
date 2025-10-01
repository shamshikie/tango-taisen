using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LearningWordsOnline.Data;
using LearningWordsOnline.Models;
using System.Security.Claims;
using LearningWordsOnline.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace LearningWordsOnline.Controllers
{
    [Authorize]
    public class FriendRequestsController : Controller
    {
        private readonly LearningWordsOnlineDbContext _appContext;

        public FriendRequestsController(LearningWordsOnlineDbContext appContext)
        {
            _appContext = appContext;
        }

        // GET: FriendRequests
        public async Task<IActionResult> Index()
        {
            var appUser = _appContext.AppUsers.First(a => a.AspNetUserId == GetAspNetUserId());
            var friendRequests = await _appContext.FriendRequests
               .Include(fr => fr.Sender).ThenInclude(s => s.Profile).ThenInclude(p => p.Icon)
               .Where(fr => fr.Receiver.Id == appUser.Id &&
                     fr.FriendRequestStatus == FriendRequestStatus.Pending)
               .OrderByDescending(fr => fr.CreatedAt)
               .ToListAsync();

            return View(friendRequests);
        }

        /// <summary>
        /// フレンド申請を行う
        /// </summary>
        /// <param name="requestedUsername"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] string requestedUsername)
        {
            var user = await _appContext.AppUsers
                .Include(a => a.SentRequests)
                .FirstOrDefaultAsync(u => u.AspNetUserId == GetAspNetUserId());

            if (user is null)
            {
                throw new NullReferenceException("ユーザー (申請者) がnullです。");
            }

            //自分自身にフレンドリクエストは送れない
            if (user.UserName == requestedUsername)
            {
                return Json(new { success = false, message = "自分自身にフレンド申請は送信できません。" });
            }

            var friendUser = await _appContext.AppUsers.FirstOrDefaultAsync(u => u.UserName == requestedUsername);

            if (friendUser == null)
            {
                return Json(new { success = false, message = "指定したユーザーが見つかりません。" });
            }

            // すでにフレンドである場合
            if (await _appContext.Friends
                .AnyAsync(f => (f.AppUserId1 == user.Id && f.AppUserId2 == friendUser.Id) ||
                                          (f.AppUserId1 == friendUser.Id && f.AppUserId2 == user.Id)))
            {
                return Json(new { success = false, message = "すでにフレンドです。" });
            }

            var friendRequest = user.SentRequests
                .FirstOrDefault(sr => sr.AppUserId1 == user.Id && sr.AppUserId2 == friendUser.Id ||
                                      sr.AppUserId2 == user.Id && sr.AppUserId1 == friendUser.Id);

            //リクエストが既にある場合
            if (friendRequest is not null)
            {
                // 申請中にもう一度申請した
                if (friendRequest.FriendRequestStatus == FriendRequestStatus.Pending)
                {
                    return Json(new { success = false, message = "すでにフレンド申請を送っています。" });
                }
                // 拒否後→再度申請、承認後→友達削除された場合
                // NOTE:基本的に承認・拒否後、一定期間を過ぎたらDBから削除する
                _appContext.FriendRequests.Remove(friendRequest);
            }

            // フレンド申請をデータベースに保存する
            var newFriendRequest = new FriendRequest
            {
                AppUserId1 = user.Id,
                AppUserId2 = friendUser.Id,
                CreatedAt = DateTime.UtcNow,
                //LastUpdatedAt = DateTime.UtcNow,
            };

            _appContext.FriendRequests.Add(newFriendRequest);

            await _appContext.SaveChangesAsync();

            return Json(new { success = true, message = "フレンド申請が成功しました。" });
        }

        /// <summary>
        /// フレンド申請の承認
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        [HttpPost]
        public async Task<IActionResult> Accept([FromBody] int id)
        {
            var user = await _appContext.AppUsers
                .Include(a => a.SentRequests).ThenInclude(fr => fr.Receiver)
                .Include(a => a.ReceivedRequests).ThenInclude(fr => fr.Sender)
                .FirstOrDefaultAsync(u => u.AspNetUserId == GetAspNetUserId());

            if (user is null)
            {
                throw new NullReferenceException("ユーザー (申請者) がnullです。");
            }

            // ユーザーの受け取ったリクエストに引数で渡されたIDのものが存在するか
            var friendRequest = user.ReceivedRequests
                .FirstOrDefault(fr => fr.Id == id);

            // 引数のフレンド申請IDが存在しない
            if (friendRequest is null)
            {
                return BadRequest();
            }

            // 既に承認されてるものが再度承認されたとき （お互いに申請中の人たちがほぼ同時にリクエストしたときなど
            if (friendRequest.FriendRequestStatus == FriendRequestStatus.Accepted)
            {
                return BadRequest();
            }

            friendRequest.FriendRequestStatus = FriendRequestStatus.Accepted;
            friendRequest.RespondedAt = DateTime.UtcNow;
            _appContext.FriendRequests.Update(friendRequest);

            //お互いにフレンド申請を送っていた場合
            //もう片方のフレンド申請を承認扱い
            var otherFriendRequest = user.SentRequests
                .FirstOrDefault(fr => fr.Receiver.Id == friendRequest.Sender.Id);
            if (otherFriendRequest is not null)
            {
                otherFriendRequest.FriendRequestStatus = FriendRequestStatus.Accepted;
                otherFriendRequest.RespondedAt = DateTime.UtcNow;
                _appContext.FriendRequests.Update(otherFriendRequest);
            }

            var friend = Friend.Create(friendRequest.Sender, friendRequest.Receiver);

            try
            {
                _appContext.Friends.Add(friend);
                await _appContext.SaveChangesAsync();
            }
            catch (Exception ex) //同時リクエストがあった場合起こり得る
            {
                Console.WriteLine($"Error: {ex.Message}");
                return BadRequest();
            }

            return Ok();
        }

        /// <summary>
        /// フレンド申請の拒否
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        [HttpPost]
        public async Task<IActionResult> Reject([FromBody] int id)
        {
            var appUser = await _appContext.AppUsers
                .Include(a => a.ReceivedRequests)
                .FirstOrDefaultAsync(u => u.AspNetUserId == GetAspNetUserId());

            if (appUser is null)
            {
                throw new NullReferenceException("ユーザー (申請者) がnullです。");
            }

            // ユーザーの受け取ったリクエストに引数で渡されたIDのものが存在するか
            var friendRequest = appUser.ReceivedRequests
                .FirstOrDefault(fr => fr.Id == id);

            // 引数のフレンド申請IDが存在しない
            if (friendRequest is null)
            {
                return BadRequest();
            }

            // 既に承認されてるものが拒否されていた場合
            if (friendRequest.FriendRequestStatus == FriendRequestStatus.Accepted)
            {
                //return BadRequest();
            }

            friendRequest.FriendRequestStatus = FriendRequestStatus.Rejected;
            friendRequest.RespondedAt = DateTime.UtcNow;
            _appContext.FriendRequests.Update(friendRequest);

            await _appContext.SaveChangesAsync();

            return Ok();
        }

        private string GetAspNetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User is null.");
        }
    }
}
