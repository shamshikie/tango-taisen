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
                // ���O�C�����Ă���ꍇ�ARank/Index�Ƀ��_�C���N�g
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
            //status code��null�̂Ƃ��͗\�����Ȃ��G���[�A�T�[�o�[�G���[�Ƃ��ď���
            if ((id ?? 500) == 404)
            {
                return View("Error404");
            }

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
