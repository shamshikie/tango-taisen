using LearningWordsOnline.Data;
using LearningWordsOnline.Services;
using LearningWordsOnline.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LearningWordsOnline.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDbContext<LearningWordsOnlineDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<JapaneseIdentityErrorDescriber>(); // カスタムエラーメッセージを適用
builder.Services.AddSignalR();
builder.Services.AddScoped<IAppUserService, AppUserService>();
builder.Services.AddScoped<IQuizService, QuizService>();
// セッションで利用するキャッシュサービスを登録
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    const int timeoutSeconds = 2;
    //Trainingページのみでセッションを使用
    options.Cookie.Path = "/Training";
    options.IdleTimeout = TimeSpan.FromSeconds(timeoutSeconds); //セッションのタイムアウト時間
    options.Cookie.IsEssential = false;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseMigrationsEndPoint();
} else {
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
// 存在しないページにアクセス
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

app.UseStaticFiles();

// セキュリティヘッダーのポリシーを定義
var policy = new HeaderPolicyCollection()
    .AddDefaultSecurityHeaders()
    .AddContentSecurityPolicy(builder => {
        // デフォルトは自分自身のみ許可
        builder.AddDefaultSrc().Self();

        // Script
        builder.AddScriptSrc().Self()
               .From("https://cdn.jsdelivr.net")
               .From("https://cdnjs.cloudflare.com")
               .From("https://js.monitor.azure.com")
               .UnsafeInline();

        // Style
        builder.AddStyleSrc().Self()
               .From("https://cdn.jsdelivr.net")
               .From("https://cdnjs.cloudflare.com")
               .From("https://fonts.googleapis.com")
               .UnsafeInline();

        // Font
        builder.AddFontSrc().Self()
               .From("https://fonts.gstatic.com")
               .From("https://cdn.jsdelivr.net")
               .From("https://cdnjs.cloudflare.com");

        // Image
        builder.AddImgSrc().Self().Data();

        // 開発環境のみ localhost の http/ws を許可
        if (app.Environment.IsDevelopment()) {
            builder.AddConnectSrc().Self()
                .From("https://localhost:*")
                .From("http://localhost:*")
                .From("wss://localhost:*")
                .From("ws://localhost:*")
                .From("https://cdn.jsdelivr.net")
                .From("https://cdnjs.cloudflare.com");
        } else {
            builder.AddConnectSrc().Self()
                .From("https://cdn.jsdelivr.net")
                .From("https://cdnjs.cloudflare.com")
                .From("https://js.monitor.azure.com")
                .From("https://japanwest-0.in.applicationinsights.azure.com");
        }

        // その他セキュリティ
        builder.AddObjectSrc().None();
        builder.AddFrameAncestors().None();
        builder.AddBaseUri().Self();
    })
    .AddStrictTransportSecurityMaxAgeIncludeSubDomainsAndPreload()
    .AddContentTypeOptionsNoSniff()
    .AddFrameOptionsDeny()
    .AddXssProtectionBlock()
    .AddReferrerPolicyStrictOriginWhenCrossOrigin()
    .AddCustomHeader("Cross-Origin-Resource-Policy", "same-origin");

app.UseSecurityHeaders(policy);

app.UseRouting();

app.UseAuthorization();
// SessionMiddlewareを登録
app.UseSession();
// 最終ログインの記録 （リクエストが飛ぶたびに確認
app.UseMiddleware<LastLoginMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapHub<RankedMatchHub>("/rankedMatchHub");
app.MapHub<RoomMatchHub>("/roomMatchHub");
app.Run();
