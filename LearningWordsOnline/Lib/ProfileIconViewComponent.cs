using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using LearningWordsOnline.Data;

namespace LearningWordsOnline.Lib
{
    public class ProfileIconViewComponent : ViewComponent
    {
        private readonly LearningWordsOnlineDbContext _appContext;

        public ProfileIconViewComponent(LearningWordsOnlineDbContext appContext)
        {
            _appContext = appContext;
        }

        public async Task<IViewComponentResult> InvokeAsync(string aspNetUserId)
        {
            // aspNetUserIdを持つユーザーのアイコンを取得
            var appUser = await _appContext.AppUsers
                .Include(a => a.Profile).ThenInclude(p => p.Icon)
                .FirstOrDefaultAsync(a => a.AspNetUserId == aspNetUserId);

            return View(appUser?.Profile.Icon);
        }
    }
}
