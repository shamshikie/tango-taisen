using LearningWordsOnline.Data;
using LearningWordsOnline.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace WordImporter {
    internal class Program {

        static async Task Main(string[] args) {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
            // ファイル名
            var fileName = configuration["ImportSettings:FileName"] ?? throw new Exception("FileName is not set");
            // 取り込む言語
            var langugaeCode = configuration["ImportSettings:LanguageCode"] ?? throw new Exception("LanguageCode is not set");
            // DB接続文字列
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (connectionString is null || fileName is null || langugaeCode is null) {
                throw new Exception("appsettings.jsonが正しく設定されていません。");
            }

            var serviceProvider = new ServiceCollection()
               .AddDbContext<LearningWordsOnlineDbContext>(options =>
                   options.UseSqlServer(connectionString))
               .BuildServiceProvider();

            string projectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\.."));
            string relativePath = "Word\\" + fileName;
            string filePath = Path.Combine(projectDirectory, relativePath);

            var wordList = GetWordsFromCSV(filePath);
            using (var dbContext = serviceProvider.GetService<LearningWordsOnlineDbContext>()) {
                await AddToDatabase(langugaeCode, wordList, dbContext, categoryId: 33);

                //一時的に使いたい削除処理
                //var wordsToDelete = dbContext.Words
                //.Where(e => e.Id >= 288);

                //var defsToDelete = dbContext.Definitions
                //.Where(e => e.Id >= 293);

                //var defcastoDelete = dbContext.DefinitionCategories
                //.Where(e => e.DefinitionId >= 693);

                //dbContext.Words.RemoveRange(wordsToDelete);
                //dbContext.Definitions.RemoveRange(defsToDelete);
                //dbContext.DefinitionCategories.RemoveRange(defcastoDelete);
                //await dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// スペルと意味が重複したものは出力しません。
        /// CSV形式:
        /// Spelling,Meaning,PartOfSpeech
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static HashSet<WordDto> GetWordsFromCSV(string filePath) {
            var wordSet = new HashSet<WordDto>(new WordDtoComparer());
            using (var reader = new StreamReader(filePath)) {

                while (!reader.EndOfStream) {
                    var line = reader.ReadLine();

                    if (line is null)
                        continue;

                    var values = line.Split(','); // カンマ区切りで分割
                    if (values.Length != 3) {
                        throw new Exception($"不正な行が発見されました。{values}");
                    }

                    Debug.WriteLine(string.Join("  ", values));

                    var newWord = new WordDto(values[0], values[1], values[2]);

                    if (!wordSet.Add(newWord)) {
                        Console.WriteLine($"重複した単語: {newWord.Spelling}, {newWord.Meaning}");
                    }
                }
            }
            return wordSet;
        }

        /// <summary>
        /// languageCodeはアルファベット2文字
        /// </summary>
        /// <param name="languageCode"></param>
        /// <param name="words"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        private async static Task AddToDatabase(string languageCode, IEnumerable<WordDto> words, LearningWordsOnlineDbContext? dbContext, int? categoryId = null) {
            if (dbContext is null) {
                throw new NullReferenceException("dbContext is null");
            }

            var language = await dbContext.Languages.FirstOrDefaultAsync(l => l.Code == languageCode);

            if (language is null) {
                throw new NullReferenceException($"DB上に存在しない言語コード({languageCode})です。");
            }

            var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId);
            if (categoryId.HasValue && category is null) {
                throw new NullReferenceException($"DB上に存在しない言語コード({languageCode})です。");
            }

            var wordsToAdd = new List<Word>();
            var definitionsToAdd = new List<Definition>();
            var definitionCategoriesToAdd = new List<DefinitionCategory>();

            foreach (var item in words) {
                //itemの品詞を特定
                var partOfSpeech = await dbContext.PartOfSpeeches.FirstOrDefaultAsync(pos => pos.Name == item.PartOfSpeech);

                if (partOfSpeech is null) {
                    throw new NullReferenceException($"{item.PartOfSpeech}が見つかりません。単語: {item.Spelling}");
                }

                //すでにDBにある単語か
                var word = await dbContext.Words
                    .Include(w => w.Definitions)
                    .FirstOrDefaultAsync(w => w.Spelling == item.Spelling);

                if (word is null) //新規単語
                {
                    word = new Word() {
                        LanguageId = language.Id,
                        Spelling = item.Spelling,
                    };
                    wordsToAdd.Add(word);
                } else {
                    Console.WriteLine($"既出単語({item.Spelling}) wordId: {word.Id}");
                }

                //itemの品詞がかぶるDefinitionを集める
                var dupicateDefinitions = word.Definitions.Where(d => d.PartOfSpeechId == partOfSpeech.Id)
                    .Select(d => d).ToList();
                // 品詞が被ってるものがない場合、DBに追加
                if (dupicateDefinitions.Count == 0) {
                    var definition = new Definition() {
                        Meaning = item.Meaning,
                        Word = word, // ナビゲーションプロパティを使う
                        PartOfSpeechId = partOfSpeech.Id,
                    };
                    definitionsToAdd.Add(definition);

                    if (category is not null) {
                        definitionCategoriesToAdd.Add(new DefinitionCategory() { CategoryId = category.Id, Definition = definition });
                    }
                } else {
                    Console.WriteLine($"重複した品詞({item.PartOfSpeech})のDefinition({item.Meaning})の追加をスキップします。");
                    dupicateDefinitions.ToList().ForEach(df => Console.WriteLine($" 重複したDefinition: {df.Id}: {df.Meaning}"));
                }
            }

            dbContext.Words.AddRange(wordsToAdd);
            dbContext.Definitions.AddRange(definitionsToAdd);
            dbContext.DefinitionCategories.AddRange(definitionCategoriesToAdd);

            await dbContext.SaveChangesAsync();
        }
    }

    public class WordDto {
        public WordDto(string spelling, string meaning, string partOfSpeech)
            => (Spelling, Meaning, PartOfSpeech) = (spelling, meaning, partOfSpeech);

        public string Spelling { get; }
        public string Meaning { get; }
        public string PartOfSpeech { get; }
    }
    class WordDtoComparer : IEqualityComparer<WordDto> {
        public bool Equals(WordDto? x, WordDto? y) {
            if (x is null || y is null) return false;
            return x.Spelling == y.Spelling && x.Meaning == y.Meaning;
        }

        public int GetHashCode(WordDto obj) {
            return HashCode.Combine(obj.Spelling, obj.Meaning);
        }
    }
}
