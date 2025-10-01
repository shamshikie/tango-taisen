using LearningWordsOnline.Data;
using LearningWordsOnline.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LearningWordsOnline.Services
{
    public class AppUserService : IAppUserService
    {
        private readonly LearningWordsOnlineDbContext _appContext;
        public AppUserService(LearningWordsOnlineDbContext appContext)
        {
            _appContext = appContext;
        }

        /// <summary>
        /// AppUsersに指定するユーザーネームが存在するかどうか
        /// </summary>
        /// <param name="userName">ユーザーネーム</param>
        /// <returns></returns>
        public async Task<bool> UsernameExistsAsync(string userName)
        {
            return await _appContext.AppUsers.AnyAsync(a => a.UserName == userName);
        }

        /// <summary>
        /// ClaimsPrincipal userから取り出されるAspNetUserIdに紐づけられているAppUserを出力
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<AppUser?> GetAppUserAsync(ClaimsPrincipal user)
        {
            if (user.Identity is not null && user.Identity.IsAuthenticated)
            {
                var aspNetUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (aspNetUserId is not null)
                {
                    return await _appContext.AppUsers
                        .FirstOrDefaultAsync(a => a.AspNetUserId == aspNetUserId);
                }
            }

            return null;
        }

        /// <summary>
        /// AppUserを生成し、データベースに保存
        /// </summary>
        /// <param name="aspNetUserId">AspNetUserのID</param>
        /// <param name="email">メールアドレス</param>
        /// <param name="userName">ユーザーネーム</param>
        /// <param name="displayName">ニックネーム</param>
        /// <returns></returns>
        public async Task<OperationResult> CreateAsync(string aspNetUserId, string email, string userName, string displayName)
        {
            if (await UsernameExistsAsync(userName))
            {
                return OperationResult.Failure("このユーザーネームは既に存在しています。使われていないユーザーネームを入力してください。");
            }

            var appUser = new AppUser
            {
                AspNetUserId = aspNetUserId,
                Email = email,
                UserName = userName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            appUser.Profile = new Profile
            {
                AppUser = appUser,
                DisplayName = displayName.Trim(),
                UpdatedAt = DateTime.UtcNow,
                IconId = 1 //初期アイコン
            };

            try
            {
                _appContext.AppUsers.Add(appUser);
                _appContext.Profiles.Add(appUser.Profile);
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(ex.Message);
            }

            await _appContext.SaveChangesAsync();

            return OperationResult.Success();
        }

        /// <summary>
        /// 指定するAppUserをDBから削除
        /// </summary>
        /// <param name="appUser"></param>
        /// <returns></returns>
        public async Task<OperationResult> DeleteAsync(AppUser appUser)
        {
            try
            {
                //NOTE:DeleteBehaviorがNoActionになっているのでそれらをすべて削除
                var friends = _appContext.Friends.Where(f => f.AppUserId1 == appUser.Id || f.AppUserId2 == appUser.Id);
                var friendRequests = _appContext.FriendRequests.Where(fr => fr.AppUserId1 == appUser.Id || fr.AppUserId2 == appUser.Id);
                var roomInvitations = _appContext.RoomInvitations.Where(ri => ri.AppUserId1 == appUser.Id || ri.AppUserId2 == appUser.Id);

                _appContext.Friends.RemoveRange(friends);
                _appContext.FriendRequests.RemoveRange(friendRequests);
                _appContext.RoomInvitations.RemoveRange(roomInvitations);
                _appContext.AppUsers.Remove(appUser);

                await _appContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(ex.Message);
            }

            return OperationResult.Success();
        }
    }
}
