using Microsoft.AspNetCore.Mvc.RazorPages;
using LearningWordsOnline.Data;
using System.Security.Claims;
using LearningWordsOnline.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LearningWordsOnline.Views.Shared
{
    public class LoginPartialModel : PageModel
    {
        private readonly LearningWordsOnlineDbContext _appContext;
        public Profile Profile { get; private set; } = default!;

        public LoginPartialModel(LearningWordsOnlineDbContext appContext)
        {
            _appContext = appContext;
        }

        public async Task OnGetAsync()
        {
            var user = await _appContext.AppUsers
                .Include(a => a.Profile)
                .FirstOrDefaultAsync(a => a.AspNetUserId == GetAspNetUserId())
                ?? throw new NullReferenceException("userがNullです。");
            Profile = user.Profile;
        }

        private string GetAspNetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("User is null.");
        }
    }
}
