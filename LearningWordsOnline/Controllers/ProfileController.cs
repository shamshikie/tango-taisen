using LearningWordsOnline.Data;
using LearningWordsOnline.Models;
using LearningWordsOnline.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LearningWordsOnline.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly LearningWordsOnlineDbContext _appContext;
        public ProfileController(LearningWordsOnlineDbContext appContext)
        {
            _appContext = appContext;
        }
        private string GetAspNetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User is null.");
        }


        // 自分のプロフィールページ
        [HttpGet("/Profile")]
        public async Task<IActionResult> Index()
        {
            var appUser = await _appContext.AppUsers.Include(a => a.Profile).ThenInclude(p => p.Icon)
                .FirstOrDefaultAsync(a => a.AspNetUserId == GetAspNetUserId());

            if (appUser is null)
            {
                return RedirectToPage("Account/Login", new { ReturnUrl = Url.Page(nameof(Index)) });
            }

            return View(appUser);
        }

        // 他人のプロフィールページ
        [HttpGet("/Profile/{id}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return RedirectToAction(nameof(Index));
            }

            var appUser = await _appContext.AppUsers
                .Include(a => a.Profile).ThenInclude(p => p.Icon)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appUser is null)
            {
                return NotFound();
            }

            if (appUser.AspNetUserId == GetAspNetUserId())
            {
                return RedirectToAction(nameof(Index));
            }

            return View(appUser);
        }

        // GET: Profile/Edit/5
        [HttpGet("/Profile/Edit")]
        public async Task<IActionResult> Edit()
        {
            var appUser = await _appContext.AppUsers
                .Include(a => a.Profile).ThenInclude(p => p.Icon)
                .FirstOrDefaultAsync(a => a.AspNetUserId == GetAspNetUserId());

            if (appUser is null)
            {
                return NotFound();
            }

            ViewBag.UserName = appUser.UserName;
            ViewBag.Icons = await _appContext.Icons.ToListAsync();

            //var editProfileViewModels = new EditProfileViewModel() { AppUser = appUser, Icons = icons };

            return View(appUser.Profile);
        }

        // POST: Profile/Edit/5
        [HttpPost("/Profile/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int? IconId)
        {
            if (IconId is not null && !_appContext.Icons.Any(i => i.Id == IconId))
            {
                return NotFound();
            }

            var appUser = await _appContext.AppUsers
                .FirstOrDefaultAsync(a => a.AspNetUserId == GetAspNetUserId());

            if (appUser is null) //存在しないユーザー
            {
                return NotFound();
            }

            var profile = await _appContext.Profiles.FindAsync(id);

            // appUserのIdと変更希望のProfileのAppUserIdが一致しない
            if (profile is null || appUser.Id != profile.AppUserId)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync(profile, "", p => p.DisplayName, p => p.Bio, p => p.IsPublic, p => p.IconId))
            {
                profile.UpdatedAt = DateTime.UtcNow;
                if (ModelState.IsValid)
                {
                    try
                    {
                        await _appContext.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!_appContext.Profiles.Any(a => a.Id == profile.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }

                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.UserName = appUser.UserName;
            ViewBag.Icons = await _appContext.Icons.ToListAsync();

            return View(profile);
        }
    }
}
