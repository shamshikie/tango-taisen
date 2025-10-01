using LearningWordsOnline.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using LearningWordsOnline.Data;
using Microsoft.EntityFrameworkCore;
using LearningWordsOnline.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace LearningWordsOnline.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RoomInvitationsController : ControllerBase
    {
        private readonly LearningWordsOnlineDbContext _appContext;


        public RoomInvitationsController(LearningWordsOnlineDbContext appContext)
        {
            _appContext = appContext;
        }

        [HttpPost("Invite")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite([FromForm] int friendId, [FromForm] string roomId)
        {
            var aspNetUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var inviterAppUser = await _appContext.AppUsers.FirstOrDefaultAsync(a => a.AspNetUserId == aspNetUserId);
            var friend = await _appContext.Friends.FindAsync(friendId);
            if (inviterAppUser is null || friend is null)
            {
                return BadRequest();
            }

            var existingInvitation = await _appContext.RoomInvitations
                .FirstOrDefaultAsync(ri => ri.RoomId == roomId
                          && ri.AppUserId1 == inviterAppUser.Id
                          && ri.AppUserId2 == (friend.AppUserId1 != inviterAppUser.Id ? friend.AppUserId1 : friend.AppUserId2));

            // 重複した招待状が作られた場合、DBは更新しない
            if (existingInvitation is not null)
            {
                return Ok();
            }


            var roomInvitation = new RoomInvitation()
            {
                RoomId = roomId,
                AppUserId1 = inviterAppUser.Id,
                AppUserId2 = friend.AppUserId1 != inviterAppUser.Id ? friend.AppUserId1 : friend.AppUserId2,
                CreatedAt = DateTime.UtcNow,
            };

            _appContext.RoomInvitations.Add(roomInvitation);
            await _appContext.SaveChangesAsync();

            return Ok();
        }

    }
}
