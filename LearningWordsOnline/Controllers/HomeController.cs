using LearningWordsOnline.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LearningWordsOnline.ViewModels;

namespace LearningWordsOnline.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ILogger<HomeController> logger, SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            if (User.Identity is not null && User.Identity.IsAuthenticated)
            {
                // ログインしている場合、Rank/Indexへリダイレクト
                return RedirectToAction(nameof(Index), "Rank");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuestLogin()
        {
            var guest = await _userManager.FindByEmailAsync("guest@wordify.app");
            if (guest == null) return RedirectToAction(nameof(Index));
            await _signInManager.SignInAsync(guest, isPersistent: false);
            return RedirectToAction("Index", "Rank");
        }

        public IActionResult Support()
        {
            return View();
        }

        public IActionResult HowTo()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? id)
        {
            //status codeがnullの場合は想定外のエラー、サーバーエラーとして扱う
            if ((id ?? 500) == 404)
            {
                return View("Error404");
            }

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
