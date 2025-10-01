using System.Security.Claims;
using LearningWordsOnline.Models;

namespace LearningWordsOnline.Services
{
    public interface IAppUserService
    {
        Task<bool> UsernameExistsAsync(string userName);
        Task<AppUser?> GetAppUserAsync(ClaimsPrincipal user);
        Task<OperationResult> CreateAsync(string aspNetUserId, string email, string userName, string displayName);
        Task<OperationResult> DeleteAsync(AppUser appUser);
    }
}
