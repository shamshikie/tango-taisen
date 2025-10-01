//using LearningWordsOnline.Models;
using LearningWordsOnline.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace LearningWordsOnline.Controllers
{
    public class AccountsController : Controller
    {
        private readonly ILogger<AccountsController> _logger;
        private readonly IAppUserService _appUserService;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountsController(ILogger<AccountsController> logger, IAppUserService appUserService, SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _appUserService = appUserService;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        /// <summary>
        /// ユーザーネームが使用可能かどうか
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<JsonResult> CheckUsernameAvailability(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { available = false, message = "ユーザー名が空です。" });
            }

            bool isAvailable = !await _appUserService.UsernameExistsAsync(username); // 非同期呼び出し
            return Json(new { available = isAvailable });
        }

    }
}
