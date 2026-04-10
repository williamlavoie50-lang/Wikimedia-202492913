using DAL;
using System.Collections.Generic;
using System.Linq;

namespace Models
{
    public class MediasRepository : Repository<Media>
    {
        public List<string> MediasCategories()
        {
            List<string> Categories = new List<string>();
            foreach (Media media in ToList().OrderBy(m => m.Category))
            {
                if (Categories.IndexOf(media.Category) == -1)
                {
                    Categories.Add(media.Category);
                }
            }
            return Categories;
        }

        public List<User> MediaOwners()
        {
            return ToList()
                .Select(m => DB.Users.Get(m.OwnerId))
                .Where(u => u != null)
                .GroupBy(u => u.Id)
                .Select(g => g.First().Copy())
                .OrderBy(u => u.Name)
                .ToList();
        }

        public void RemoveLikesByUser(int userId)
        {
            BeginTransaction();
            try
            {
                foreach (var media in ToList().Where(m => m.LikedByUserIds != null && m.LikedByUserIds.Contains(userId)).ToList())
                {
                    media.RemoveUserLikes(userId);
                    Update(media);
                }
            }
            finally
            {
                EndTransaction();
            }
        }

        public void DeleteByOwner(int ownerId)
        {
            BeginTransaction();
            try
            {
                foreach (var media in ToList().Where(m => m.OwnerId == ownerId).ToList())
                {
                    Delete(media.Id);
                }
            }
            finally
            {
                EndTransaction();
            }
        }
    }
}
