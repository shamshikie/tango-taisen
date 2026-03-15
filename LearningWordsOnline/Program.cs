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
    options.UseNpgsql(connectionString));
builder.Services.AddDbContext<LearningWordsOnlineDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<JapaneseIdentityErrorDescriber>(); // カスタムエラーメッセージを適用
builder.Services.AddSignalR();
builder.Services.AddScoped<IAppUserService, AppUserService>();
builder.Services.AddScoped<IQuizService, QuizService>();
// セッションで利用するキャッシュサービスを登録
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    const int timeoutSeconds = 2;
    // Trainingページのみでセッションを使用
    options.Cookie.Path = "/Training";
    options.IdleTimeout = TimeSpan.FromSeconds(timeoutSeconds); // セッションのタイムアウト設定
    options.Cookie.IsEssential = false;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var db = scope.ServiceProvider.GetRequiredService<LearningWordsOnlineDbContext>();
    appDb.Database.Migrate();
    db.Database.Migrate();

    var seedPath = Path.Combine(AppContext.BaseDirectory, "Data", "seed_pg.sql");
    if (File.Exists(seedPath)) {
        var sql = File.ReadAllText(seedPath);
        db.Database.ExecuteSqlRaw(sql);
    }

    // ゲストアカウントのseed（存在しない場合のみ作成）
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var appUserSvc = scope.ServiceProvider.GetRequiredService<IAppUserService>();
    const string guestEmail = "guest@wordify.app";
    if (await userManager.FindByEmailAsync(guestEmail) == null) {
        var guestIdentityUser = new IdentityUser { Email = guestEmail, UserName = guestEmail, EmailConfirmed = true };
        await userManager.CreateAsync(guestIdentityUser, "Guest1234!");
        var guestUserId = await userManager.GetUserIdAsync(guestIdentityUser);
        await appUserSvc.CreateAsync(guestUserId, guestEmail, "guest_user", "ゲスト");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseMigrationsEndPoint();
} else {
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
// 存在しないページにアクセスした場合のエラーページ設定
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

app.UseStaticFiles();

// セキュリティヘッダーのポリシー設定
var policy = new HeaderPolicyCollection()
    .AddDefaultSecurityHeaders()
    .AddContentSecurityPolicy(builder => {
        // デフォルトは自サイトのみ許可
        builder.AddDefaultSrc().Self();

        // Script
        builder.AddScriptSrc().Self()
               .From("https://cdn.jsdelivr.net")
               .From("https://cdnjs.cloudflare.com")
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

        // 開発環境でlocalhostからのhttps/ws接続を許可
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
                .From("https://cdnjs.cloudflare.com");
        }

        // その他セキュリティ設定
        builder.AddObjectSrc().None();
        builder.AddFrameAncestors().None();
        builder.AddBaseUri().Self();
    })
    .AddStrictTransportSecurityMaxAgeIncludeSubDomainsAndPreload()
    .AddContentTypeOptionsNoSniff()
    .AddFrameOptionsDeny()
    .AddXssProtectionBlock()
    .AddReferrerPolicyStrictOriginWhenCrossOrigin()
    .AddCustomHeader("Cross-Origin-Resource-Policy", "same-origin")
    .AddCustomHeader("Cross-Origin-Embedder-Policy", "unsafe-none");

app.UseSecurityHeaders(policy);

app.UseRouting();

app.UseAuthorization();
// SessionMiddlewareを登録
app.UseSession();
// 最終ログインの記録（リクエストごとに確認）
app.UseMiddleware<LastLoginMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapHub<RankedMatchHub>("/rankedMatchHub");
app.MapHub<RoomMatchHub>("/roomMatchHub");
app.Run();
