using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LearningWordsOnline.Data;
using LearningWordsOnline.Models;
using System.Security.Claims;
using LearningWordsOnline.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace LearningWordsOnline.Controllers
{
    [Authorize]
    public class FriendsController : Controller
    {
        private readonly LearningWordsOnlineDbContext _appContext;
        private readonly IConfiguration _configuration;

        public FriendsController(LearningWordsOnlineDbContext appContext, IConfiguration configuration)
        {
            _appContext = appContext;
            _configuration = configuration;
        }

        private string GetAspNetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User.Identity.Name is null.");
        }

        // GET: Friends
        public async Task<IActionResult> Index()
        {
            var aspNetUserId = GetAspNetUserId();
            var user = await _appContext.AppUsers
                .Include(a => a.Friends1)
                    .ThenInclude(f => f.AppUser2).ThenInclude(a => a.Profile).ThenInclude(p => p.Icon)
                .Include(a => a.Friends1).ThenInclude(f => f.AppUser2).ThenInclude(a => a.UserActivity)
                .Include(a => a.Friends2)
                    .ThenInclude(f => f.AppUser1).ThenInclude(a => a.Profile).ThenInclude(p => p.Icon)
                .Include(a => a.Friends2).ThenInclude(f => f.AppUser1).ThenInclude(a => a.UserActivity)
                .FirstAsync(a => a.AspNetUserId == aspNetUserId);

            var allFriends = user.Friends1.Concat(user.Friends2).ToList();

            var friendViewModels = allFriends.Select(f =>
            {
                var friendUser = f.AppUserId1 == user.Id ? f.AppUser2 : f.AppUser1;
                return new FriendViewModel
                {
                    Id = f.Id,
                    CreatedAt = f.CreatedAt,
                    FriendUser = friendUser,
                    IsActive = IsUserActive(friendUser)
                };
            }).ToList();

            var friendRequestCount = await _appContext.FriendRequests
               .Where(fr => fr.Receiver.Id == user.Id &&
                     fr.FriendRequestStatus == FriendRequestStatus.Pending)
               .CountAsync();

            ViewBag.FriendRequestCount = friendRequestCount;

            return View(friendViewModels);
        }

        // GET: Friends/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var friend = await _appContext.Friends
                .Include(f => f.AppUser1)
                .Include(f => f.AppUser2)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (friend == null)
            {
                return NotFound();
            }

            return View(friend);
        }

        // GET: Friends/Create
        //public IActionResult Create()
        //{
        //    ViewData["AppUserId1"] = new SelectList(_appContext.AppUsers, "Id", "AspNetUserId");
        //    ViewData["AppUserId2"] = new SelectList(_appContext.AppUsers, "Id", "AspNetUserId");
        //    return View();
        //}

        // POST: Friends/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create([Bind("Id,AppUserId1,AppUserId2,CreatedAt")] Friend friend)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _appContext.Add(friend);
        //        await _appContext.SaveChangesAsync();
        //        return RedirectToAction(nameof(Index));
        //    }
        //    ViewData["AppUserId1"] = new SelectList(_appContext.AppUsers, "Id", "AspNetUserId", friend.AppUserId1);
        //    ViewData["AppUserId2"] = new SelectList(_appContext.AppUsers, "Id", "AspNetUserId", friend.AppUserId2);
        //    return View(friend);
        //}

        // GET: Friends/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var friend = await _appContext.Friends.FindAsync(id);
            if (friend == null)
            {
                return NotFound();
            }
            ViewData["AppUserId1"] = new SelectList(_appContext.AppUsers, "Id", "AspNetUserId", friend.AppUserId1);
            ViewData["AppUserId2"] = new SelectList(_appContext.AppUsers, "Id", "AspNetUserId", friend.AppUserId2);
            return View(friend);
        }

        // POST: Friends/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AppUserId1,AppUserId2,CreatedAt")] Friend friend)
        {
            if (id != friend.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _appContext.Update(friend);
                    await _appContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FriendExists(friend.Id))
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
            ViewData["AppUserId1"] = new SelectList(_appContext.AppUsers, "Id", "AspNetUserId", friend.AppUserId1);
            ViewData["AppUserId2"] = new SelectList(_appContext.AppUsers, "Id", "AspNetUserId", friend.AppUserId2);
            return View(friend);
        }

        // GET: Friends/Delete/5
        //public async Task<IActionResult> Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var friend = await _context.Friends
        //        .Include(f => f.AppUser1)
        //        .Include(f => f.AppUser2)
        //        .FirstOrDefaultAsync(m => m.Id == id);
        //    if (friend == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(friend);
        //}

        // POST: Friends/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var friend = await _appContext.Friends.FindAsync(id);
            var appUser = await _appContext.AppUsers
                .FirstAsync(a => a.AspNetUserId == GetAspNetUserId());


            if (friend is not null &&
                (appUser.Friends1.Any(f => f.Id == id) ||
                appUser.Friends2.Any(f => f.Id == id)))
            {
                _appContext.Friends.Remove(friend);
            }

            await _appContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetFriendActivities()
        {
            var appUser = await _appContext.AppUsers
               .Include(a => a.Friends1).ThenInclude(f => f.AppUser2).ThenInclude(a => a.UserActivity)
               .Include(a => a.Friends2).ThenInclude(f => f.AppUser1).ThenInclude(a => a.UserActivity)
               .FirstAsync(a => a.AspNetUserId == GetAspNetUserId());
            var allFriends = appUser.Friends1.Concat(appUser.Friends2).ToList();
            var friendAppUsers = allFriends.Select(f => f.AppUserId1 == appUser.Id ? f.AppUser2 : f.AppUser1);

            var friendActivities = friendAppUsers.Select(fa => new { Id = fa.Id, IsActive = IsUserActive(fa) });

            return Ok(friendActivities);
        }

        private bool FriendExists(int id)
        {
            return _appContext.Friends.Any(e => e.Id == id);
        }

        private bool IsUserActive(AppUser appUser)
        {
            if (appUser.UserActivity is null)
                return false;

            return (DateTime.UtcNow - appUser.UserActivity.LastLoginedAt).TotalMinutes < _configuration.GetValue<int>("AppSettings:ActiveMinutesThreshold");
        }
    }
}
