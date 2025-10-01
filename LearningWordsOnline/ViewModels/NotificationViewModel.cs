using LearningWordsOnline.Models;
using Microsoft.Data.SqlClient;

namespace LearningWordsOnline.ViewModels
{
    public class NotificationItemViewModel
    {
        //public required AppUser AppUser { get; init; }
        /// <summary>
        /// AppUserId of the sender
        /// </summary>
        public required AppUser Sender { get; init; }
        //public required int SenderId { get; init; }
        //public required Icon? Icon { get; init; }
        public required NotificationType NotificationType { get; init; }
        /// <summary>
        /// if NotificationType is Announce, this property should be normal text.
        /// if NotificationType is FriendRequest, this property must be its Id.
        /// if NotificationType is RoomInvitation, this property must be its Id.
        /// </summary>
        public required string Text { get; init; }
        /// <summary>
        /// This property is used when you want a user to transit a particular page.
        /// If NotificationType is FriendRequest or RoomInvitation, this property is not used.
        /// </summary>
        public string? Url { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required bool IsReferenced { get; init; }
        public required bool IsDone { get; init; }
    }
}
