using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LearningWordsOnline.Data;
using LearningWordsOnline.Models;
using LearningWordsOnline.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Drawing.Printing;


namespace LearningWordsOnline.Controllers
{
    [Authorize]
    public class MyWordsController : Controller
    {
        private readonly LearningWordsOnlineDbContext _appContext;

        public MyWordsController(LearningWordsOnlineDbContext appContext)
        {
            _appContext = appContext;
        }

        public async Task<IActionResult> Index(
            string? searchQuery, string answeredFilter = "all",
            int page = 1, int pageSize = 10, string languageCode = "en")
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var appUser = await _appContext.AppUsers.FirstAsync(a => a.AspNetUserId == userId);

            var language = await _appContext.Languages.FirstAsync(l => l.Code == languageCode);
            if (language is null)
            {
                return NotFound("Language not found.");
            }

            var wordsQuery = GetWordsQuery(searchQuery, answeredFilter, language.Id, appUser.Id);

            var totalWords = await wordsQuery.CountAsync(w => w.LanguageId == language.Id);
            var words = await wordsQuery
                .Select(w => new
                {
                    Word = w,
                    // 1単語に複数のDefinitionがあるため、その中の最終回答日時が最も現在に近い
                    LastAnsweredAt = w.Definitions
                    .SelectMany(d => d.AppUserDefinitions)
                    .Where(ad => ad.AppUserId == appUser.Id)
                    .Max(ad => ad.AnsweredAt)
                })
                .OrderByDescending(x => x.LastAnsweredAt)//リクエストしてきたユーザーの最近解いた順にソート
                .ThenBy(x => x.Word.Spelling)
                .Select(x => x.Word)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var myWordsviewModels = words.Select(w => new MyWordsViewModel
            {
                WordId = w.Id,
                Level = w.Level,
                Spelling = w.Spelling,
                LanguageCode = w.Language.Code,
                DefinitionViewModels = w.Definitions.Select(d => new DefinitionViewModel
                {
                    DefinitionId = d.Id,
                    Meaning = d.Meaning,
                    PartOfSpeech = d.PartOfSpeech.Name,
                    AppUserDefinition = d.AppUserDefinitions.FirstOrDefault(ad => ad.AppUserId == appUser.Id)
                }).OrderBy(dvm => dvm.PartOfSpeech).ToList(),
                CorrectAnswerRate = CalculateCorrectAnswerRate(
                    w.Definitions.SelectMany(d => d.AppUserDefinitions)
                        .Where(ad => ad.AppUserId == appUser.Id)
                )
            }).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalWords / pageSize);
            ViewBag.SearchQuery = searchQuery;
            ViewBag.AnsweredFilter = answeredFilter;
            return View(myWordsviewModels);
        }

        private static int? CalculateCorrectAnswerRate(IEnumerable<AppUserDefinition> appUserDefinitions)
        {
            var totalCount = appUserDefinitions.Sum(ud => ud.Count);
            if (totalCount == 0)
            {
                return null; // 計算不能時は null を返す
            }

            var correctCount = appUserDefinitions.Sum(ud => ud.Count - ud.WrongCount);
            return 100 * correctCount / totalCount;
        }

        private IQueryable<Word> GetWordsQuery(string? searchQuery, string answeredFilter, int languageId, int appUserId)
        {
            var wordsQuery = _appContext.Words
                .Include(w => w.Definitions).ThenInclude(d => d.PartOfSpeech)
                .Include(w => w.Definitions).ThenInclude(d => d.AppUserDefinitions)
                .Where(w => w.LanguageId == languageId);

            switch (answeredFilter)
            {
                case "answered":
                    wordsQuery = wordsQuery.Where(w => w.Definitions.Any(d => d.AppUserDefinitions.Any(ad => ad.AppUserId == appUserId)));
                    break;
                case "unanswered":
                    wordsQuery = wordsQuery.Where(w => w.Definitions.All(d => d.AppUserDefinitions.All(ad => ad.AppUserId != appUserId)));
                    break;
                case "all":
                default:
                    // 全て表示（フィルターなし）
                    break;
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                // スペースで検索ワードを分割してAND検索を作成
                var keywords = searchQuery.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (var keyword in keywords)
                {
                    wordsQuery = wordsQuery.Where(w => w.Spelling.StartsWith(keyword) ||
                                                        w.Definitions.Any(d => d.Meaning.Contains(keyword)));
                }
            }

            return wordsQuery;
        }
    }
}
