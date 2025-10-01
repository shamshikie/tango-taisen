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
builder.Services.AddSession(options =>
{
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
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
// 存在しないページにアクセス
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

app.UseStaticFiles();

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
