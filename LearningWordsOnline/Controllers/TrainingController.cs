using LearningWordsOnline.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LearningWordsOnline.Models;
using Microsoft.EntityFrameworkCore;
using LearningWordsOnline.Services;
using System.Text.Json;
using System.Security.Claims;
using LearningWordsOnline.GameLogic;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace LearningWordsOnline.Controllers
{
    [Authorize]
    public class TrainingController : Controller
    {
        private readonly LearningWordsOnlineDbContext _appContext;
        private readonly IQuizService _quizService;
        private readonly int _timer;
        private readonly int _maxQuestionCount;

        public TrainingController(LearningWordsOnlineDbContext appContext, IQuizService quizService, IConfiguration configuration)
        {
            _appContext = appContext;
            _quizService = quizService;
            _timer = configuration.GetValue<int>("CommonMatchSettings:Timer");
            _maxQuestionCount = configuration.GetValue<int>("TrainingSettings:MaxQuestionCount");
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _appContext.Categories
                .Where(c => c.ParentCategoryId == null)
                .Include(c => c.Language)
                .Include(c => c.ChildCategories)
                .ToListAsync();

            ViewBag.MaxLevel = 20; //単語レベルの最大値 (weblio参考)
            return View(categories);
        }

        [HttpGet]
        public async Task<IActionResult> Play()
        {
            // セッションからフォームデータを取得
            var trainingMode = HttpContext.Session.GetString("TrainingMode");
            var questionLevel = HttpContext.Session.GetInt32("QuestionLevel");
            //var categoryId = HttpContext.Session.GetInt32("CategoryId");
            var parentCategoryId = HttpContext.Session.GetInt32("ParentCategoryId") ?? 0;
            var childCategoryId = HttpContext.Session.GetInt32("ChildCategoryId") ?? 0;
            var questionCount = HttpContext.Session.GetInt32("QuestionCount") ?? 0;
            var languageId = HttpContext.Session.GetInt32("LanguageId");

            if (!Enum.TryParse(trainingMode, out TrainingMode mode)
                || !_appContext.Languages.Any(l => l.Id == languageId) || questionCount <= 0)
            {
                return RedirectToAction(nameof(Index));
            }

            var language = await _appContext.Languages.FindAsync(languageId);
            if (language is null)
            {
                return RedirectToAction(nameof(Index));
            }

            int categoryId = 0;

            //親カテゴリが存在するが、子カテゴリが存在しない
            if (parentCategoryId != 0 && childCategoryId == 0)
                categoryId = parentCategoryId;
            //親カテゴリも子カテゴリも存在する
            else if (parentCategoryId != 0 && childCategoryId != 0)
                categoryId = childCategoryId;

            // 設定以上のクイズ数の設定がされたとき上限値に設定
            questionCount = questionCount > _maxQuestionCount ? _maxQuestionCount : questionCount;

            var questions = await GetQuestions(mode, language, questionCount, questionLevel, categoryId);

            if (questions is null || !questions.Any())
            {
                TempData["ErrorMessage"] = "問題が存在しません。";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Timer = _timer;
            return View(questions);
        }

        [HttpPost]
        public IActionResult Play(string trainingMode, int languageId, int questionCount, int? questionLevel, int? parentCategoryId, int? childCategoryId)
        {
            if (string.IsNullOrEmpty(trainingMode) || languageId <= 0
                || questionCount <= 0)
            {
                TempData["ErrorMessage"] = "予期せぬエラーが発生しました。もう一度やり直してください。";
                return RedirectToAction(nameof(Index));
            }

            // フォームデータをセッションに保存
            HttpContext.Session.SetString("TrainingMode", trainingMode);
            HttpContext.Session.SetInt32("QuestionLevel", questionLevel ?? 0);
            //HttpContext.Session.SetInt32("CategoryId", categoryId);
            HttpContext.Session.SetInt32("ParentCategoryId", parentCategoryId ?? 0);
            HttpContext.Session.SetInt32("ChildCategoryId", childCategoryId ?? 0);
            HttpContext.Session.SetInt32("QuestionCount", questionCount);
            HttpContext.Session.SetInt32("LanguageId", languageId);

            return RedirectToAction(nameof(Play));
        }

        /// <summary>
        /// 回答結果をまとめて送信し、DBに保存
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        [Route("api/submitTrainingResults")]
        [HttpPost]
        public async Task<IActionResult> SubmitResults([FromBody] List<QuestionResult> results)
        {
            var appUser = await _appContext.AppUsers
                .Include(a => a.AppUserDefinitions)
                .FirstOrDefaultAsync(a => a.AspNetUserId == GetAspNetUserId());

            if (appUser is null)
            {
                return Unauthorized(new { error = "Unauthorized access" });
            }

            foreach (var result in results)
            {
                //クライアントから送られてきたDefinitionIdが存在しない場合（不正なId)
                if (!_appContext.Definitions.Any(d => d.Id == result.DefinitionId))
                {
                    continue;
                }

                var userDefinition = appUser.AppUserDefinitions.FirstOrDefault(ud => ud.DefinitionId == result.DefinitionId);
                if (userDefinition is null) //新規単語
                {
                    var newUserDefinition = new AppUserDefinition
                    {
                        AppUserId = appUser.Id,
                        DefinitionId = result.DefinitionId,
                        Count = 1,
                        WrongCount = result.IsCorrect ? 0 : 1,
                        AnsweredAt = DateTime.UtcNow
                    };
                    appUser.AppUserDefinitions.Add(newUserDefinition);
                }
                else
                {
                    // 既存の場合は更新
                    userDefinition.Count++;
                    userDefinition.AnsweredAt = DateTime.UtcNow;
                    if (!result.IsCorrect)
                        userDefinition.WrongCount++;
                }
            }
            await _appContext.SaveChangesAsync();

            return Ok(new { message = "Results successfully received", data = results });
        }

        /// <summary>
        /// 問題リストを生成する
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="language"></param>
        /// <param name="questionCount"></param>
        /// <param name="questionLevel"></param>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        private async Task<IEnumerable<Question>?> GetQuestions(TrainingMode mode, Language language, int questionCount, int? questionLevel, int? categoryId)
        {
            if (mode == TrainingMode.Weakness)
            {
                var appUser = await _appContext.AppUsers.FirstOrDefaultAsync(a => a.AspNetUserId == GetAspNetUserId());
                if (appUser is null)
                    return null;
                return await _quizService
                .GenerateRandomQuestions(questionCount, language, appUserId: appUser.Id);
            }
            else if (mode == TrainingMode.Level)
            {
                return await _quizService
                .GenerateRandomQuestions(questionCount, language, level: questionLevel);
            }
            else if (mode == TrainingMode.Category)
            {
                return await _quizService
                .GenerateRandomQuestions(questionCount, language, categoryId: categoryId);
            }

            return null;
        }

        private string GetAspNetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User is null.");
        }

        public class QuestionResult
        {
            public required bool IsCorrect { get; init; }
            public required int DefinitionId { get; init; }
        }

    }
}
