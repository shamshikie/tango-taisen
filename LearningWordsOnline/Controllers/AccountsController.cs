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
        /// ���[�U�[�l�[�����g�p�\���ǂ���
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<JsonResult> CheckUsernameAvailability(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { available = false, message = "���[�U�[������ł��B" });
            }

            bool isAvailable = !await _appUserService.UsernameExistsAsync(username); // �񓯊��Ăяo��
            return Json(new { available = isAvailable });
        }

    }
}
