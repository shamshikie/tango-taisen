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

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<JapaneseIdentityErrorDescriber>(); // �J�X�^���G���[���b�Z�[�W��K�p
builder.Services.AddSignalR();
builder.Services.AddScoped<IAppUserService, AppUserService>();
builder.Services.AddScoped<IQuizService, QuizService>();
// �Z�b�V�����ŗ��p����L���b�V���T�[�r�X��o�^
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    const int timeoutSeconds = 2;
    //Training�y�[�W�݂̂ŃZ�b�V�������g�p
    options.Cookie.Path = "/Training";
    options.IdleTimeout = TimeSpan.FromSeconds(timeoutSeconds); //�Z�b�V�����̃^�C���A�E�g����
    options.Cookie.IsEssential = false;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddControllersWithViews();

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
// ���݂��Ȃ��y�[�W�ɃA�N�Z�X
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

app.UseStaticFiles();

// �Z�L�����e�B�w�b�_�[�̃|���V�[���`
var policy = new HeaderPolicyCollection()
    .AddDefaultSecurityHeaders()
    .AddContentSecurityPolicy(builder => {
        // �f�t�H���g�͎������g�̂݋���
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

        // �J�����̂� localhost �� http/ws ������
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

        // ���̑��Z�L�����e�B
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
// SessionMiddleware��o�^
app.UseSession();
// �ŏI���O�C���̋L�^ �i���N�G�X�g����Ԃ��тɊm�F
app.UseMiddleware<LastLoginMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapHub<RankedMatchHub>("/rankedMatchHub");
app.MapHub<RoomMatchHub>("/roomMatchHub");
app.Run();
