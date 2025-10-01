using LearningWordsOnline.Data;
using LearningWordsOnline.GameLogic;
using LearningWordsOnline.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LearningWordsOnline.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Configuration;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace LearningWordsOnline.Controllers
{
    [Authorize]
    public class RoomsController : Controller
    {
        private readonly int _maxPlayerCount;
        private readonly LearningWordsOnlineDbContext _appContext;
        private readonly IReadOnlyList<int> _points;
        //private readonly MatchSettings _settings;
        private readonly IConfiguration _configuration;

        public RoomsController(LearningWordsOnlineDbContext appContext, IConfiguration configuration)
        {
            _maxPlayerCount = configuration.GetValue<int>("RoomMatchSettings:MaxPlayerCount");
            _appContext = appContext;
            _points = configuration.GetSection("RoomMatchSettings:Points").Get<IReadOnlyList<int>>() ?? throw new NullReferenceException();
            _configuration = configuration;
            //_settings = new MatchSettings()
            //{
            //    MaxPlayerCount = configuration.GetValue<int>("RoomdMatchSettings:MaxPlayerCount"),
            //    QuestionCount = configuration.GetValue<int>("RoomMatchSettings:QuestionCount"),
            //    OptionCount = configuration.GetValue<int>("CommonMatchSettings:OptionCount"),
            //    Timer = configuration.GetValue<int>("CommonMatchSettings:Timer"),
            //    Points = configuration.GetSection("RoomMatchSettings:Points").Get<IReadOnlyList<int>>() ?? throw new Exception("RoomMatchSettingsのPointsが設定されていません")
            //};
        }
        private string GetAspNetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User.Identity.Name is null.");
        }


        public IActionResult Index()
        {
            ViewBag.MaxQuestionCount = _configuration.GetValue<int>("RoomMatchSettings:MaxQuestionCount");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] int questionCount, string languageCode = "en")
        {
            if (questionCount <= 0)
            {
                questionCount = 1;
            }
            else if (questionCount > _configuration.GetValue<int>("RoomMatchSettings:MaxQuestionCount"))
            {
                questionCount = _configuration.GetValue<int>("RoomMatchSettings:MaxQuestionCount");
            }

            // 存在しない言語コードの場合
            Language? language = await _appContext.Languages.FirstOrDefaultAsync(l => l.Code == languageCode);
            if (language is null)
            {
                TempData["ErrorMessage"] = "存在しない言語か、このアプリでは遊べない言語です。";
                return RedirectToAction(nameof(Index));
            }

            var settings = new MatchSettings()
            {
                MaxPlayerCount = _configuration.GetValue<int>("RoomMatchSettings:MaxPlayerCount"),
                QuestionCount = questionCount,
                OptionCount = _configuration.GetValue<int>("CommonMatchSettings:OptionCount"),
                Timer = _configuration.GetValue<int>("CommonMatchSettings:Timer"),
                Points = _configuration.GetSection("RoomMatchSettings:Points").Get<IReadOnlyList<int>>() ?? throw new Exception("RoomMatchSettingsのPointsが設定されていません")
            };
            var aspNetUserId = GetAspNetUserId();
            var room = RoomManager.CreateRoom(aspNetUserId, settings, language);
            return RedirectToAction(nameof(Join), new { roomId = room.Id });
        }

        public async Task<IActionResult> Join(string? roomId)
        {
            if (roomId is null)
            {
                return RedirectToAction(nameof(Index));
            }

            var room = RoomManager.GetRoom(roomId);
            var aspNetUserId = GetAspNetUserId(); // 現在ログインしているユーザーのID

            if (room is null)
            {
                TempData["ErrorMessage"] = "部屋が見つかりませんでした。"; // エラーメッセージをTempDataに保存
                return RedirectToAction(nameof(Index)); // 部屋に移動せずにトップページにリダイレクト
            }

            if (room.Players.Count >= _maxPlayerCount && !room.Players.Any(player => player.Id == aspNetUserId))
            {
                TempData["ErrorMessage"] = "この部屋は既に最大人数に達しています。"; // エラーメッセージをTempDataに保存
                return RedirectToAction(nameof(Index)); // 部屋に移動せずにトップページにリダイレクト
            }

            var appUser = await _appContext.AppUsers
                .Include(a => a.Friends1).ThenInclude(f => f.AppUser2).ThenInclude(a => a.Profile).ThenInclude(p => p.Icon)
                .Include(a => a.Friends1).ThenInclude(f => f.AppUser2).ThenInclude(a => a.UserActivity)
                .Include(a => a.Friends2).ThenInclude(f => f.AppUser1).ThenInclude(a => a.Profile).ThenInclude(p => p.Icon)
                .Include(a => a.Friends2).ThenInclude(f => f.AppUser1).ThenInclude(a => a.UserActivity)
                .FirstAsync(a => a.AspNetUserId == aspNetUserId);

            var allFriends = appUser.Friends1.Concat(appUser.Friends2).ToList();

            var friendViewModels = allFriends.Select(f =>
            {
                var friendUser = f.AppUserId1 == appUser.Id ? f.AppUser2 : f.AppUser1;
                return new FriendViewModel
                {
                    Id = f.Id,
                    CreatedAt = f.CreatedAt,
                    FriendUser = friendUser,
                    IsActive = IsUserActive(friendUser)
                };
            }).ToList();

            var roomViewModel = new RoomViewModel()
            {
                AspNetUserId = aspNetUserId,
                RoomId = room.Id,
                IsInProgress = room.IsInProgress,
                MaxPlayerCount = _maxPlayerCount,
                FriendViewModels = friendViewModels
            };
            return View("Room", roomViewModel);
        }

        // 非同期で部屋の人数を確認し、超えているかどうかを返すAPIエンドポイント
        [HttpGet]
        public IActionResult CheckRoomCapacity(string roomId)
        {
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return Json(new { success = false, message = "部屋が見つかりません。" });
            }

            if (room.Players.Count >= _maxPlayerCount)
            {
                return Json(new { success = false, message = "この部屋にはすでに最大人数が参加しています。" });
            }

            return Json(new { success = true });
        }

        private bool IsUserActive(AppUser appUser)
        {
            if (appUser.UserActivity is null)
                return false;


            return (DateTime.UtcNow - appUser.UserActivity.LastLoginedAt).TotalMinutes < _configuration.GetValue<int>("AppSettings:ActiveMinutesThreshold");
        }
    }
}
