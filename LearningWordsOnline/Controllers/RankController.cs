using LearningWordsOnline.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LearningWordsOnline.Models;
using LearningWordsOnline.ViewModels;
using LearningWordsOnline.Helpers;

namespace LearningWordsOnline.Controllers
{
    [Authorize]
    public class RankController : Controller
    {
        private readonly int _maxPlayerCount;
        private readonly LearningWordsOnlineDbContext _appContext;

        public RankController(LearningWordsOnlineDbContext appContext, IConfiguration configuration)
        {
            _maxPlayerCount = configuration.GetValue<int>("RankedMatchSettings:MaxPlayerCount");
            _appContext = appContext;
        }


        public async Task<IActionResult> Index()
        {
            var appUser = await _appContext.AppUsers.Include(a => a.Profile)
                .FirstOrDefaultAsync(a => a.AspNetUserId == GetAspNetUserId());

            if (appUser is null)
            {
                throw new NullReferenceException("ユーザーが存在しません。");
            }

            var histories = new List<HistoryViewModel>();
            // 1ユーザーの対戦履歴を取得
            var battles = await _appContext.Battles
                .Where(b => b.BattleAppUsers.Any(ba => ba.AppUserId == appUser.Id))
                .Include(b => b.BattleAppUsers)
                    .ThenInclude(ba => ba.AppUser)
                        .ThenInclude(a => a.Profile).ThenInclude(p => p.Icon)
                .OrderByDescending(b => b.BattledAt)
                .Take(30) //30個の戦績のみを取得
                .ToListAsync();

            RankViewModel rankViewModel = new()
            {
                AppUser = appUser,
                Battles = battles
            };

            return View(rankViewModel);
        }

        public IActionResult Play(int languageId = 1) //デフォルトで英語
        {
            if (!_appContext.Languages.Any(l => l.Id == languageId))
            {
                TempData["ErrorMessage"] = "存在しない言語、またはこのアプリではサポートしていない言語です。";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.LanguageId = languageId;
            ViewBag.MaxPlayerCount = _maxPlayerCount;
            ViewBag.UserId = GetAspNetUserId();
            return View();
        }

        [HttpGet]
        [Route("api/GetRankAlphabet/{rankPoints}")]
        public IActionResult GetRankAlphabet(int rankPoints)
        {
            if (rankPoints == 0)
            {
                return NotFound(new { message = $"rankPoints is invalid" });
            }
            string rankAlphabet = RankHelper.GetRank(rankPoints);
            return Content(rankAlphabet, "text/plain");
        }
        private string GetAspNetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User.Identity.Name is null.");
        }
    }
}
