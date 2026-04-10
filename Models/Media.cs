using DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models
{
    public enum MediaSortBy { Title, PublishDate, Likes }

    public class Media : Record
    {
        public string Title { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string YoutubeId { get; set; }
        public DateTime PublishDate { get; set; } = DateTime.Now;

        public int OwnerId { get; set; } = 1;
        public bool Shared { get; set; } = true;
        public List<int> LikedByUserIds { get; set; } = new List<int>();

        [JsonIgnore]
        public User Owner => DB.Users.Get(OwnerId)?.Copy();

        [JsonIgnore]
        public List<User> LikedByUsers => LikedByUserIds
            .Distinct()
            .Select(id => DB.Users.Get(id))
            .Where(u => u != null)
            .Select(u => u.Copy())
            .OrderBy(u => u.Name)
            .ToList();

        [JsonIgnore]
        public int LikesCount => LikedByUsers.Count;

        public bool IsLikedBy(int userId) => LikedByUserIds != null && LikedByUserIds.Contains(userId);

        public bool ToggleLike(int userId)
        {
            if (LikedByUserIds == null)
                LikedByUserIds = new List<int>();

            if (LikedByUserIds.Contains(userId))
            {
                LikedByUserIds.Remove(userId);
                return false;
            }

            LikedByUserIds.Add(userId);
            LikedByUserIds = LikedByUserIds.Distinct().ToList();
            return true;
        }

        public void RemoveUserLikes(int userId)
        {
            if (LikedByUserIds == null)
                LikedByUserIds = new List<int>();

            LikedByUserIds.RemoveAll(id => id == userId);
        }

        public override bool IsValid()
        {
            if (!HasRequiredLength(Title, 1)) return false;
            if (!HasRequiredLength(Category, 1)) return false;
            if (!HasRequiredLength(Description, 1)) return false;
            if (DB.Medias.ToList().Where(m => m.YoutubeId == YoutubeId && m.Id != Id).Any()) return false;
            return true;
        }
    }
}
