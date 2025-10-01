using Microsoft.AspNetCore.SignalR;
using LearningWordsOnline.Hubs;
using LearningWordsOnline.Models;
using System.Collections.Concurrent;
using LearningWordsOnline.GameLogic;
using LearningWordsOnline.Data;
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace LearningWordsOnline.Services
{
    public class QuizService : IQuizService
    {
        private readonly LearningWordsOnlineDbContext _appContext;
        //選択肢の数
        private readonly int _optionCount;

        public QuizService(LearningWordsOnlineDbContext appContext, IConfiguration configuration)
        {
            _optionCount = configuration.GetValue<int>("CommonMatchSettings:OptionCount");
            _appContext = appContext;
        }

        /// <summary>
        /// 引数に応じたランダムな問題リストを生成する。
        /// </summary>
        /// <param name="questionCount">問題数</param>
        /// <param name="language">どの言語か</param>
        /// <param name="categoryId">カテゴリの指定(子カテゴリがあるにもかかわらず親カテゴリを指定すると問題が作られない</param>
        /// <param name="level">WordのLevelを指定 (Weblio参考)</param>
        /// <param name="appUserId">appUserIdの苦手問題を出題します</param>
        /// <returns>問題リスト</returns>
        public async Task<IList<Question>> GenerateRandomQuestions(int questionCount, Language language, int? categoryId = null, int? level = null, int? appUserId = null)
        {
            if (appUserId.HasValue)
            {
                return await GenerateWeaknessQuestions(questionCount, language, appUserId.Value, categoryId, level);
            }

            return await GenerateNormalQuestions(questionCount, categoryId, level, language);
        }

        /// <summary>
        /// ユーザーの苦手単語を優先した問題リストを作成する
        /// NOTE: categoryID、Level指定はまだ未検証
        /// </summary>
        /// <param name="questionCount">問題数</param>
        /// <param name="language">言語</param>
        /// <param name="appUserId">指定するユーザーAppUserのID</param>
        /// <param name="categoryId">kテゴリID</param>
        /// <param name="level">単語レベル</param>
        /// <returns></returns>
        private async Task<IList<Question>> GenerateWeaknessQuestions(int questionCount, Language language, int appUserId, int? categoryId = null, int? level = null)
        {
            // 指定された言語のユーザーのAppUserDefinitionを取り出す
            var userDefinitionsQuerybase = _appContext.AppUserDefinitions
                .Where(ad => ad.AppUserId == appUserId)
                .Include(ad => ad.Definition).ThenInclude(d => d.Word)
                .Where(ad => ad.Definition.Word.LanguageId == language.Id);

            //NOTE: 未検証
            if (level.HasValue)
            {
                userDefinitionsQuerybase = userDefinitionsQuerybase
                        .Where(ad => ad.Definition.Word.Level == level.Value);
            }
            //NOTE: 未検証
            if (categoryId.HasValue)
            {
                userDefinitionsQuerybase = userDefinitionsQuerybase
                    .Include(ad => ad.Definition)
                        .ThenInclude(d => d.DefinitionCategories)
                        .Where(ad => ad.Definition.DefinitionCategories.Any(dc => dc.CategoryId == categoryId.Value));
            }

            // 正解率が低いものを優先して取り出し、シャッフル
            var userDefinitions = await userDefinitionsQuerybase
                .OrderByDescending(ad => 100 * ad.WrongCount / ad.Count)
                .Take(questionCount)
                .OrderBy(_ => Guid.NewGuid())
                .ToListAsync();

            var questions = new List<Question>();

            // クイズ生成
            foreach (var userDefinition in userDefinitions)
            {
                var word = userDefinition.Definition.Word;
                var options = await GenerateOptions(userDefinition.Definition.Meaning, word.Id, language);

                questions.Add(new Question
                {
                    Text = word.Spelling,
                    CorrectAnswer = userDefinition.Definition.Meaning,
                    Options = options,
                    DefinitionId = userDefinition.Definition.Id
                });
            }
            return questions;
        }


        /// <summary>
        /// 引数で指定された問題リストを生成する
        /// </summary>
        /// <param name="questionCount">問題数</param>
        /// <param name="categoryId">カテゴリID</param>
        /// <param name="level">単語レベル</param>
        /// <param name="language">言語</param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">存在しないカテゴリID</exception>
        /// <exception cref="ArgumentException">引数の言語と指定したカテゴリの言語が一致しない</exception>
        private async Task<IList<Question>> GenerateNormalQuestions(int questionCount, int? categoryId, int? level, Language language)
        {
            // ベースクエリ: 指定された言語のWordsを取得
            var baseQuery = _appContext.Words
                .Where(w => w.LanguageId == language.Id)
                .Include(w => w.Definitions) // Definitionsを含める
                .AsQueryable();

            // Level指定がされている場合
            baseQuery = level.HasValue ? baseQuery.Where(w => w.Level == level.Value) : baseQuery;

            // CategoryIdが指定されている場合、さらに絞り込む
            if (categoryId.HasValue)
            {
                var category = await _appContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId.Value);
                if (category is null)
                {
                    throw new NullReferenceException($"存在しないカテゴリーID ({categoryId.Value}) です。");
                }
                if (category.LanguageId != language.Id)
                {
                    throw new ArgumentException($"カテゴリーの言語 ({category}) と言語コード ({language}) が一致していません。");
                }
                baseQuery = FilterByCategory(category, baseQuery);
            }

            // 指定した問題の数だけ単語ランダムに取り出す
            var words = await baseQuery
                .OrderBy(_ => Guid.NewGuid()) // ランダム化
                .Take(questionCount)        // 必要な質問数を取得
                .ToListAsync(); //メモリにロード

            var questions = new List<Question>();

            // ここから適切な答えの設定と、選択肢を生成
            foreach (var word in words)
            {
                // 各単語の複数の意味を取り出す
                var definitionsQuery = _appContext.Definitions
                    .Where(d => d.WordId == word.Id)
                    //.Include(w => w.DefinitionCategories)
                    .AsQueryable();

                // NOTE:categoryId が指定されている場合のみ Where 条件を追加→選択肢もカテゴリフィルタするかどうか
                //if (categoryId.HasValue)
                //{
                //    definitionsQuery = definitionsQuery
                //        .Where(d => d.DefinitionCategories.Any(dc => dc.CategoryId == categoryId.Value));
                //}

                // Definitionsからランダムに正解を選択
                var randomDefinition = definitionsQuery
                    .OrderBy(_ => Guid.NewGuid()) //ランダム順
                    .First();

                // 選択肢を非同期に生成
                var options = await GenerateOptions(randomDefinition.Meaning, word.Id, language);

                // 質問を生成
                questions.Add(new Question
                {
                    Text = word.Spelling,
                    CorrectAnswer = randomDefinition.Meaning,
                    Options = options,
                    DefinitionId = randomDefinition.Id
                });
            }

            return questions;
        }

        /// <summary>
        /// 選択肢を生成
        /// </summary>
        /// <param name="correctAnswer">正答</param>
        /// <param name="wordId">単語ID</param>
        /// <param name="languageId"></param>
        /// <returns></returns>
        private async Task<IEnumerable<string>> GenerateOptions(string correctAnswer, int wordId, Language language)
        {
            var options = await _appContext.Definitions
                .Where(d => d.Word.LanguageId == language.Id)
                .Where(d => d.Meaning != correctAnswer && d.WordId != wordId) // 正解やWordIdを除外
                .OrderBy(_ => Guid.NewGuid()) // ランダム順
                .Take(_optionCount - 1)                            // 他の3つの意味を取得
                .Select(d => d.Meaning)
                .ToListAsync();

            // 正解を追加し、ランダム順に並べ替える
            options.Add(correctAnswer);
            options = options.OrderBy(_ => Guid.NewGuid()).ToList();
            return options;
        }


        /// <summary>
        /// カテゴリが一致する単語を取り出す
        /// </summary>
        /// <param name="category"></param>
        /// <param name="baseQuery"></param>
        /// <returns></returns>
        private static IQueryable<Word> FilterByCategory(Category category, IQueryable<Word> baseQuery)
        {
            return baseQuery.Where(w => w.Definitions
                .Any(d => d.DefinitionCategories
                    .Any(dc => dc.CategoryId == category.Id)));
        }
    }
}
