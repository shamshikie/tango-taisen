using LearningWordsOnline.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LearningWordsOnline.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using LearningWordsOnline.Models;

namespace LearningWordsOnline.Controllers
{
    [Authorize]
    public class RankingController : Controller
    {
        private readonly LearningWordsOnlineDbContext _appContext;

        public RankingController(LearningWordsOnlineDbContext appContext)
        {
            _appContext = appContext;
        }

        public async Task<IActionResult> Index()
        {
            var appUser = await _appContext.AppUsers.FirstOrDefaultAsync(a => a.AspNetUserId == GetAspNetUserId());
            var rankingViewModel = await LeaderBoard();
            return View(rankingViewModel);
        }

        public async Task<RankingViewModel> LeaderBoard(int topPlayerCount = 30)
        {
            var appUser = await _appContext.AppUsers.FirstOrDefaultAsync(a => a.AspNetUserId == GetAspNetUserId());

            var myOwnPosition = await GetUserPosition(appUser);
            var leaderboard = await GetTopPlayers(topPlayerCount);

            return new RankingViewModel() { Leaderboard = leaderboard, MyOwnPosition = myOwnPosition };
        }

        private async Task<IList<UserPosition>> GetTopPlayers(int topNumber = 30)
        {
            var leaders = await _appContext.AppUsers
               .Include(a => a.Profile).ThenInclude(p => p.Icon)
               .OrderByDescending(a => a.Profile.RankPoint)
               .Take(topNumber) //上位の取り出し
               .ToListAsync();

            var rankingViewModels = new List<UserPosition>();

            int currentRank = 1;  // 初期の順位は1位
            int? previousRankPoint = null;  // 前回のスコア（初期値はnull）

            // 順位を付与してRankingViewModelに変換
            for (int index = 0; index < leaders.Count; index++)
            {
                var user = leaders[index];

                // 同点の場合は順位を変更せず、前回の順位を保持
                // 同点でない場合は順位更新
                if (user.Profile.RankPoint != previousRankPoint)
                {
                    currentRank = index + 1;
                }

                rankingViewModels.Add(new UserPosition
                {
                    Position = currentRank,
                    AppUser = user
                });
                // 前回のスコアを更新
                previousRankPoint = user.Profile.RankPoint;
            }

            return rankingViewModels;
        }

        private async Task<UserPosition?> GetUserPosition(AppUser? appUser)
        {
            if (appUser is null)
            {
                return null;
            }

            var users = await _appContext.AppUsers
               .Include(a => a.Profile).ThenInclude(p => p.Icon)
               .OrderByDescending(a => a.Profile.RankPoint)
               .ToListAsync();

            int currentRank = 1;  // 初期の順位は1位
            int? previousRankPoint = null;  // 前回のスコア（初期値はnull）

            for (int index = 0; index < users.Count; index++)
            {
                var user = users[index];

                // 同点でない場合は順位更新
                if (user.Profile.RankPoint != previousRankPoint)
                {
                    currentRank = index + 1;
                }

                if (user.Id == appUser.Id)
                {
                    return new UserPosition
                    {
                        Position = currentRank,
                        AppUser = user
                    };
                }

                // 前回のスコアを更新
                previousRankPoint = user.Profile.RankPoint;
            }
            return null;
        }

        private string GetAspNetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User.Identity.Name is null.");
        }
    }
}
