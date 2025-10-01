using LearningWordsOnline.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LearningWordsOnline.ViewModels;

namespace LearningWordsOnline.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity is not null && User.Identity.IsAuthenticated)
            {
                // ログインしている場合、Rank/Indexにリダイレクト
                return RedirectToAction(nameof(Index), "Rank");
            }
            return View();
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
            //status codeがnullのときは予期しないエラー、サーバーエラーとして処理
            if ((id ?? 500) == 404)
            {
                return View("Error404");
            }

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
