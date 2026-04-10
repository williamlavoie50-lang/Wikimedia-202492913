using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using static Controllers.AccessControl;

[UserAccess(Access.View)]
public class MediasController : Controller
{
    private void InitSessionVariables()
    {
        if (Session["CurrentMediaId"] == null) Session["CurrentMediaId"] = 0;
        if (Session["CurrentMediaTitle"] == null) Session["CurrentMediaTitle"] = "";
        if (Session["Search"] == null) Session["Search"] = false;
        if (Session["SearchString"] == null) Session["SearchString"] = "";
        if (Session["SelectedCategory"] == null) Session["SelectedCategory"] = "";
        if (Session["SelectedOwnerId"] == null) Session["SelectedOwnerId"] = 0;
        if (Session["Categories"] == null) Session["Categories"] = DB.Medias.MediasCategories();
        if (Session["SortByTitle"] == null) Session["SortByTitle"] = true;
        if (Session["MediaSortBy"] == null) Session["MediaSortBy"] = MediaSortBy.PublishDate;
        if (Session["SortAscending"] == null) Session["SortAscending"] = false;
        ValidateSelectedCategory();
        ValidateSelectedOwner();

        if (Session["pageNum"] == null) Session["pageNum"] = 1;
        if (Session["firstPageSize"] == null) Session["firstPageSize"] = 12;
        if (Session["pageSize"] == null) Session["pageSize"] = 3;
        if (Session["EndOfMedias"] == null) Session["EndOfMedias"] = false;
    }

    private void ResetMediasPaging()
    {
        Session["pageNum"] = 1;
        Session["EndOfMedias"] = false;
    }

    private IEnumerable<Media> BuildFilteredQuery()
    {
        InitSessionVariables();

        IEnumerable<Media> result;
        bool search = (bool)Session["Search"];
        string searchString = ((string)Session["SearchString"] ?? "").ToLower();
        string selectedCategory = (string)Session["SelectedCategory"];
        int selectedOwnerId = (int)Session["SelectedOwnerId"];

        if (Models.User.ConnectedUser.IsAdmin)
            result = DB.Medias.ToList();
        else
            result = DB.Medias.ToList().Where(c => c.Shared || Models.User.ConnectedUser.Id == c.OwnerId);

        if (search)
        {
            if (!string.IsNullOrWhiteSpace(searchString))
                result = result.Where(c => (c.Title.ToLower() + " " + c.Description.ToLower()).Contains(searchString));

            if (!string.IsNullOrWhiteSpace(selectedCategory))
                result = result.Where(c => c.Category == selectedCategory);

            if (selectedOwnerId > 0)
                result = result.Where(c => c.OwnerId == selectedOwnerId);
        }

        bool sortAscending = (bool)Session["SortAscending"];
        switch ((MediaSortBy)Session["MediaSortBy"])
        {
            case MediaSortBy.Title:
                result = sortAscending ? result.OrderBy(c => c.Title).ThenBy(c => c.PublishDate)
                                       : result.OrderByDescending(c => c.Title).ThenByDescending(c => c.PublishDate);
                break;
            case MediaSortBy.PublishDate:
                result = sortAscending ? result.OrderBy(c => c.PublishDate).ThenBy(c => c.Title)
                                       : result.OrderByDescending(c => c.PublishDate).ThenByDescending(c => c.Title);
                break;
            case MediaSortBy.Likes:
                result = sortAscending ? result.OrderBy(c => c.LikesCount).ThenBy(c => c.Title)
                                       : result.OrderByDescending(c => c.LikesCount).ThenByDescending(c => c.PublishDate);
                break;
        }

        return result;
    }

    private List<Media> _getItems(int index, int nbItems)
    {
        try
        {
            var result = BuildFilteredQuery();
            int total = result.Count();
            if (total <= index)
            {
                Session["EndOfMedias"] = true;
                return new List<Media>();
            }
            if (total < nbItems + index)
            {
                nbItems = total - index;
                Session["EndOfMedias"] = true;
            }
            return result.Skip(index).Take(nbItems).ToList();
        }
        catch
        {
            return null;
        }
    }

    public ActionResult SetFirstPageSize(int pageSize)
    {
        Session["firstPageSize"] = pageSize;
        return null;
    }

    public ActionResult getNextMediasPage()
    {
        bool EndOfMedias = (bool)Session["EndOfMedias"];
        if (!EndOfMedias)
        {
            Session["pageNum"] = (int)Session["pageNum"] + 1;
            int pageNum = (int)Session["pageNum"];
            int pageSize = (int)Session["pageSize"];
            int firstPageSize = (int)Session["firstPageSize"];
            Debug.WriteLine("PageNum: " + pageNum);
            IEnumerable<Media> mediasPage = _getItems(
                pageNum == 1 ? 0 : (pageNum - 2) * pageSize + firstPageSize,
                pageNum == 1 ? firstPageSize : pageSize);
            return PartialView("GetMedias", mediasPage);
        }
        return null;
    }

    public ActionResult EndOfMedias()
    {
        bool EndOfMedias = (bool)Session["EndOfMedias"];
        return Json(EndOfMedias, JsonRequestBehavior.AllowGet);
    }

    private void ResetCurrentMediaInfo()
    {
        Session["CurrentMediaId"] = 0;
        Session["CurrentMediaTitle"] = "";
    }

    private void ValidateSelectedCategory()
    {
        if (Session["SelectedCategory"] != null)
        {
            var selectedCategory = (string)Session["SelectedCategory"];
            var medias = DB.Medias.ToList().Where(c => c.Category == selectedCategory);
            if (medias.Count() == 0)
                Session["SelectedCategory"] = "";
        }
    }

    private void ValidateSelectedOwner()
    {
        if (Session["SelectedOwnerId"] != null)
        {
            int selectedOwnerId = (int)Session["SelectedOwnerId"];
            if (selectedOwnerId > 0 && !DB.Users.ToList().Any(u => u.Id == selectedOwnerId))
                Session["SelectedOwnerId"] = 0;
        }
    }

    public ActionResult GetMediasCategoriesList(bool forceRefresh = false)
    {
        try
        {
            InitSessionVariables();
            bool search = (bool)Session["Search"];
            if (search)
                return PartialView();
            return null;
        }
        catch (Exception ex)
        {
            return Content("Erreur interne" + ex.Message, "text/html");
        }
    }

    public ActionResult GetMediaOwnersList(bool forceRefresh = false)
    {
        try
        {
            InitSessionVariables();
            bool search = (bool)Session["Search"];
            if (search)
                return PartialView();
            return null;
        }
        catch (Exception ex)
        {
            return Content("Erreur interne" + ex.Message, "text/html");
        }
    }

    public ActionResult GetMediaDetails(bool forceRefresh = false)
    {
        try
        {
            InitSessionVariables();
            int mediaId = (int)Session["CurrentMediaId"];
            Media Media = DB.Medias.Get(mediaId);
            if (DB.Users.HasChanged || DB.Medias.HasChanged || forceRefresh)
                return PartialView(Media);
            return null;
        }
        catch (Exception ex)
        {
            return Content("Erreur interne" + ex.Message, "text/html");
        }
    }

    public ActionResult GetMedias(bool forceRefresh = false)
    {
        try
        {
            if (DB.Users.HasChanged || DB.Medias.HasChanged || forceRefresh)
            {
                InitSessionVariables();
                int pageNum = (int)Session["pageNum"];
                int pageSize = (int)Session["pageSize"];
                int firstPageSize = (int)Session["firstPageSize"];
                return PartialView(_getItems(0, pageNum > 1 ? (pageNum - 1) * pageSize + firstPageSize : firstPageSize));
            }
            return null;
        }
        catch (Exception ex)
        {
            return Content("Erreur interne" + ex.Message, "text/html");
        }
    }

    public ActionResult List()
    {
        ResetCurrentMediaInfo();
        return View();
    }

    public ActionResult ToggleSearch()
    {
        ResetMediasPaging();
        if (Session["Search"] == null) Session["Search"] = false;
        Session["Search"] = !(bool)Session["Search"];
        return RedirectToAction("List");
    }

    public ActionResult SetMediaSortBy(MediaSortBy mediaSortBy)
    {
        ResetMediasPaging();
        Session["MediaSortBy"] = mediaSortBy;
        return RedirectToAction("List");
    }

    public ActionResult ToggleMediaSort()
    {
        ResetMediasPaging();
        int mediaSortBy = (int)Session["MediaSortBy"] + 1;
        if (mediaSortBy >= Enum.GetNames(typeof(MediaSortBy)).Length) mediaSortBy = 0;
        Session["MediaSortBy"] = mediaSortBy;
        return RedirectToAction("List");
    }

    public ActionResult ToggleSort()
    {
        ResetMediasPaging();
        Session["SortAscending"] = !(bool)Session["SortAscending"];
        return RedirectToAction("List");
    }

    public ActionResult SetSearchString(string value)
    {
        ResetMediasPaging();
        Session["SearchString"] = (value ?? "").ToLower();
        return RedirectToAction("List");
    }

    public ActionResult SetSearchCategory(string value)
    {
        ResetMediasPaging();
        Session["SelectedCategory"] = value ?? "";
        return RedirectToAction("List");
    }

    public ActionResult SetSearchOwner(int value = 0)
    {
        ResetMediasPaging();
        Session["SelectedOwnerId"] = value;
        return RedirectToAction("List");
    }

    public ActionResult ToggleMediaLike(int id)
    {
        Media media = DB.Medias.Get(id);
        if (media == null)
            return null;

        bool canSee = Models.User.ConnectedUser.IsAdmin || media.Shared || media.OwnerId == Models.User.ConnectedUser.Id;
        if (!canSee)
            return new HttpStatusCodeResult(403);

        bool isLiked = media.ToggleLike(Models.User.ConnectedUser.Id);
        DB.Medias.Update(media);
        DB.Events.Add(isLiked ? $"Like media {media.Title}" : $"Unlike media {media.Title}");
        return null;
    }

    public ActionResult About()
    {
        return View();
    }

    public ActionResult Details(int id)
    {
        Session["CurrentMediaId"] = id;
        Media Media = DB.Medias.Get(id);
        Session["UserCanEditCurrentMedia"] = false;
        if (Media != null)
        {
            Session["CurrentMediaTitle"] = Media.Title;
            Session["UserCanEditCurrentMedia"] = Media.OwnerId == Models.User.ConnectedUser.Id || Models.User.ConnectedUser.IsAdmin;
            return View(Media);
        }
        return RedirectToAction("List");
    }

    [UserAccess(Access.Write)]
    public ActionResult Create()
    {
        return View(new Media());
    }

    [HttpPost]
    [UserAccess(Access.Write)]
    [ValidateAntiForgeryToken()]
    public ActionResult Create(Media Media, string sharedCB = "off")
    {
        if (Media.IsValid())
        {
            Media.OwnerId = Models.User.ConnectedUser.Id;
            Media.Shared = sharedCB == "on";
            DB.Medias.Add(Media);
            DB.Events.Add("Create", Media.Title);
            return RedirectToAction("List");
        }
        DB.Events.Add("Illegal Create Media");
        return Redirect("/Accounts/Login?message=Erreur de creation de Media!&success=false");
    }

    [UserAccess(Access.Write)]
    public ActionResult Edit()
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        if (id != 0)
        {
            Media Media = DB.Medias.Get(id);
            if (Media != null)
            {
                if (Media.OwnerId == Models.User.ConnectedUser.Id || Models.User.ConnectedUser.IsAdmin)
                    return View(Media);
            }
        }
        return Redirect("/Accounts/Login?message=Accès illégal! &success=false");
    }

    [UserAccess(Access.Write)]
    [HttpPost]
    [ValidateAntiForgeryToken()]
    public ActionResult Edit(Media Media, string sharedCB = "off")
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        Media storedMedia = DB.Medias.Get(id);
        if (storedMedia != null)
        {
            Media.Shared = sharedCB == "on";
            Media.Id = id;
            Media.OwnerId = storedMedia.OwnerId;
            Media.PublishDate = storedMedia.PublishDate;
            Media.LikedByUserIds = storedMedia.LikedByUserIds ?? new List<int>();

            if (Media.IsValid())
            {
                DB.Medias.Update(Media);
                return RedirectToAction("Details/" + id);
            }
        }
        DB.Events.Add("Illegal Edit Media");
        return Redirect("/Accounts/Login?message=Erreur de modification de Media!&success=false");
    }

    [UserAccess(Access.Write)]
    public ActionResult Delete()
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        if (id != 0)
        {
            Media Media = DB.Medias.Get(id);
            if (Media != null)
            {
                if (Media.OwnerId == Models.User.ConnectedUser.Id || Models.User.ConnectedUser.IsAdmin)
                {
                    DB.Medias.Delete(id);
                    return RedirectToAction("List");
                }
                return Redirect("/Accounts/Login?message=Accès illégal! &success=false");
            }
        }
        return Redirect("/Accounts/Login?message=Accès illégal! &success=false");
    }

    public JsonResult CheckConflict(string YoutubeId)
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        return Json(DB.Medias.ToList().Where(c => c.YoutubeId == YoutubeId && c.Id != id).Any(), JsonRequestBehavior.AllowGet);
    }
}
