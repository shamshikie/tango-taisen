using LearningWordsOnline.Data;
using LearningWordsOnline.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Security.Claims;

namespace LearningWordsOnline.Middleware
{
    public class LastLoginMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LastLoginMiddleware> _logger;
        private readonly IConfiguration _configuration;


        public LastLoginMiddleware(RequestDelegate next, ILogger<LastLoginMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context, IServiceProvider serviceProvider)
        {
            // リクエストを処理
            _logger.LogInformation("LastLoginMiddleware: Request Path: {Path}", context.Request.Path);
            // ログイン状態か判定
            if (context.User.Identity is not null &&
                context.User.Identity.IsAuthenticated)
            {
                try
                {
                    // スコープを作成してDbContextを取得
                    // NOTE: DbContext:非スレッドセーフ, Scoped, middleware: Singleton)
                    using var scope = serviceProvider.CreateScope();
                    var appContext = scope.ServiceProvider.GetRequiredService<LearningWordsOnlineDbContext>();

                    await UpdateLastLogin(context, appContext);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "最終ログインの更新中にエラーが発生しました。");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    return;
                }
            }

            // 次のミドルウェアにリクエストを渡す
            await _next(context);

            // レスポンスを処理する場合（任意）
            //_logger.LogInformation("Response Status Code: {StatusCode}", context.Response.StatusCode);
        }

        private async Task UpdateLastLogin(HttpContext context, LearningWordsOnlineDbContext appContext)
        {
            // データベース読み込み
            string aspNetUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("ユーザーが認証されていないか、AspNetUserIdが見つかりません。");

            var user = await appContext.AppUsers
                .Include(u => u.UserActivity)
                .FirstOrDefaultAsync(u => u.AspNetUserId == aspNetUserId)
                ?? throw new NullReferenceException($"AspNetUserId '{aspNetUserId}' に対応するユーザーが見つかりません。");

            var userActivity = user.UserActivity;
            var now = DateTime.UtcNow;

            if (userActivity is null)
            {
                appContext.UserActivities.Add(new UserActivity()
                {
                    AppUserId = user.Id,
                    LastLoginedAt = now,
                });
            }
            else
            {

                // 前回の最終ログインがX分未満のときに更新しない
                if ((now - userActivity.LastLoginedAt).TotalMinutes
                    < _configuration.GetValue<int>("AppSettings:LastLoginUpdateIntervalMinutes"))
                {
                    return;
                }

                userActivity.LastLoginedAt = now;
                appContext.UserActivities.Update(userActivity);

            }

            await appContext.SaveChangesAsync();
            _logger.LogInformation("{UserName}の最終ログインを更新", user.UserName);
        }
    }
}
