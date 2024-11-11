using WatchStore.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WatchStore.Controllers
{
    public class SiteController : Controller
    {
        private WatchStoreDbContext db = new WatchStoreDbContext();
        // GET: Site
        public ActionResult Index(String slug = "")
        {
            int pageNumber = 1;
            Session["keywords"] = null;
            if (!string.IsNullOrEmpty(Request.QueryString["page"]))
            {
                pageNumber = int.Parse(Request.QueryString["page"].ToString());
            }
            if (slug == "")
            {
                return this.Home();
            }
            else if (slug.Equals("Dưới 1 triệu") || slug.Equals("Từ 1 - 2 triệu") || slug.Equals("Từ 2 - 3 triệu") || slug.Equals("Từ 3 triệu"))
            {
                return this.ProductPrice(slug, pageNumber);
            }
            else
                    {
                var link = db.Links.Where(m => m.Slug == slug);
                if (link.Count() > 0)
                {
                    var res = link.First();
                    if (res.Type == "page")
                    {
                        return this.PostPage(slug);
                    }
                    else if (res.Type == "topic")
                    {
                        return this.PostTopic(slug, pageNumber);
                    }
                    else if (res.Type == "category")
                    {
                        return this.ProductCategory(slug, pageNumber);
                    }
                }
                else
                {
                    if (db.Products.Where(m => m.Slug == slug && m.Status == 1).Count() > 0)
                    {
                        return this.ProductDetail(slug);
                    }
                    else if (db.Posts.Where(m => m.Slug == slug && m.Type == "post" && m.Status == 1).Count() > 0)
                    {
                        return this.PostDetail(slug);
                    }
                }
                return this.Error(slug);
            }
        }

        public ActionResult ProductDetail(String slug)
        {
            //??
            var model = db.Products
                .Where(m => m.Slug == slug && m.Status == 1)
                .First();
            int catid = model.CateID;

            var recentlyViews = (List<MProduct>)Session["RecentlyViewedProducts"];
            MProduct pDelete = null;
            foreach (MProduct p in recentlyViews)
            {
                if (p.ID.Equals(model.ID))
                {
                    pDelete = p;
                    break;
                }
            }

            recentlyViews.Remove(pDelete);
            recentlyViews.Insert(0, model);
            List<int> listcatid = new List<int>();
            listcatid.Add(catid);

            var list2 = db.Categorys
                .Where(m => m.ParentId == catid)
                .Select(m => m.Id)
                .ToList();
            foreach (var id2 in list2)
            {
                listcatid.Add(id2);
                var list3 = db.Categorys
                    .Where(m => m.ParentId == id2)
                    .Select(m => m.Id)
                    .ToList();
                foreach (var id3 in list3)
                {
                    listcatid.Add(id3);
                }
            }
            // danh mục cùng sản phẩm
            ViewBag.listother = db.Products
                .Where(m => m.Status == 1 && listcatid
                .Contains(m.CateID) && m.ID != model.ID)
                .OrderByDescending(m => m.Created_at)
                .Take(12)
                .ToList();
            // sản phẩm mới nhập
            ViewBag.news = db.Products
                .Where(m => m.Status == 1 /*&& listcatid.Contains(m.CatId)*/ && m.ID != model.ID)
                .OrderByDescending(m => m.Created_at).Take(4).ToList();

            ViewBag.Rates = db.Rates.Where(m => m.ProductID == model.ID);

            return View("ProductDetail", model);
        }
        public ActionResult PostDetail(String slug)
        {
            var model = db.Posts
                 .Where(m => m.Slug == slug && m.Status == 1)
                 .First();
            int topid = model.Topid;

            List<int> listtopid = new List<int>();
            listtopid.Add(topid);

            var list2 = db.Topics
                .Where(m => m.ParentId == topid)
                .Select(m => m.Id)
                .ToList();
            foreach (var id2 in list2)
            {
                listtopid.Add(id2);
                var list3 = db.Topics
                    .Where(m => m.ParentId == id2)
                    .Select(m => m.Id)
                    .ToList();
                foreach (var id3 in list3)
                {
                    listtopid.Add(id3);
                }
            }
            // danh mục cùng bài viết
            ViewBag.listother = db.Posts
                .Where(m => m.Status == 1 && listtopid
                .Contains(m.Topid) && m.Id != model.Id)
                .OrderByDescending(m => m.Created_At)
                .Take(12)
                .ToList();

            return View("PostDetail", model);
        }
        public ActionResult Error(String slug)
        {
            return View("Error");
        }

        public ActionResult PostPage(String slug)
        {
            var item = db.Posts
                .Where(m => m.Slug == slug && m.Type == "page")
                 .First();
            return View("PostPage", item);
        }

        public ActionResult PostTopic(String slug, int pageNumber)
        {
            int pageSize = 8;
            var row_cat = db.Topics
                .Where(m => m.Slug == slug)
                .First();
            List<int> listtopid = new List<int>();
            listtopid.Add(row_cat.Id);

            var list2 = db.Topics
                .Where(m => m.ParentId == row_cat.Id)
                .Select(m => m.Id)
                .ToList();
            foreach (var id2 in list2)
            {
                listtopid.Add(id2);
                var list3 = db.Topics
                    .Where(m => m.ParentId == id2)
                    .Select(m => m.Id)
                    .ToList();
                foreach (var id3 in list3)
                {
                    listtopid.Add(id3);
                }
            }
            var list = db.Posts
                .Where(m => m.Status == 1 && listtopid.Contains(m.Topid))
                .OrderByDescending(m => m.Created_At);


            ViewBag.Slug = slug;
            ViewBag.CatName = row_cat.Name;
            return View("PostTopic", list
                .ToPagedList(pageNumber, pageSize));
        }
        public ActionResult ProductPrice(String slug, int pageNumber)
        {
            int pageSize = 16;
            double start = 0;
            double end = 0;
            if (slug.Equals("Dưới 1 triệu"))
            {
                end = 1000000;
            }
            else if (slug.Equals("Từ 1 - 2 triệu"))
            {
                start = 1000000;
                end = 2000000;
            }
            else if (slug.Equals("Từ 2 - 3 triệu"))
            {
                start = 2000000;
                end = 3000000;
            }
            else if (slug.Equals("Từ 3 triệu"))
            {
                start = 3000000;
                end = 1000000000000;
            }
            var listt = db.Products
            .Where(m => m.Status == 1 && m.Price > start && m.Price < end)
            .OrderByDescending(m => m.Created_at);

            ViewBag.Slug = "Sản phẩm " + slug;
            ViewBag.CatName = slug;
            ViewBag.Sl = listt.Count();
            return View("ProductCategory", listt
                .ToPagedList(pageNumber, pageSize));
            
        }
        public ActionResult ProductCategory(String slug, int pageNumber)
        {
          
            int pageSize = 16;
            var row_cat = db.Categorys
                .Where(m => m.Slug == slug)
                .First();
            List<int> listcatid = new List<int>();
            listcatid.Add(row_cat.Id);

            var list2 = db.Categorys
                .Where(m => m.ParentId == row_cat.Id)
                .Select(m => m.Id)
                .ToList();
            foreach (var id2 in list2)
            {
                listcatid.Add(id2);
                var list3 = db.Categorys
                    .Where(m => m.ParentId == id2)
                    .Select(m => m.Id)
                    .ToList();
                foreach (var id3 in list3)
                {
                    listcatid.Add(id3);
                }
            }
            var list = db.Products
                .Where(m => m.Status == 1 && listcatid.Contains(m.CateID))
                .OrderByDescending(m => m.Created_at);

            ViewBag.Slug = slug;
            ViewBag.CatName = row_cat.Name;
            ViewBag.Sl = db.Products.Where(m => m.Status != 0 && m.CateID == row_cat.Id).Count();
            return View("ProductCategory", list
                .ToPagedList(pageNumber, pageSize));
        }


        //Trang Chủ
        public ActionResult Home()
        {
            var list = db.Categorys
               .Where(m => m.Status == 1 && m.ParentId == 0)
               .Take(8)
               .ToList();
            return View("Home", list);
        }
        public ActionResult Other()
        {
            return View("Other");
        }
        //Sản phẩm trang chủ
        public ActionResult ProductHome(int catid)
        {
            List<int> listcatid = new List<int>();
            listcatid.Add(catid);

            var list2 = db.Categorys
                .Where(m => m.ParentId == catid).Select(m => m.Id)
                .ToList();
            foreach (var id2 in list2)
            {
                listcatid.Add(id2);
                var list3 = db.Categorys
                    .Where(m => m.ParentId == id2)
                    .Select(m => m.Id).ToList();
                foreach (var id3 in list3)
                {
                    listcatid.Add(id3);
                }
            }

            var list = db.Products
                .Where(m => m.Status == 1 && listcatid
                .Contains(m.CateID))
                .Take(12)
                .OrderByDescending(m => m.Created_at);

            return View("_ProductHome", list);
        }
        //Tat ca sp
        public ActionResult Product(int? page)
        {
            int pageSize = 12;
            int pageNumber = (page ?? 1);
            var list = db.Products.Where(m => m.Status == 1)
                .OrderByDescending(m => m.Created_at)
                .ToPagedList(pageNumber, pageSize);
            return View(list);
        }
        // tìm kiếm sản phẩm
        public ActionResult Search(String key, int? page)
        {
            int pageSize = 10;
            int pageNumber = (page ?? 1);
            if (String.IsNullOrEmpty(key.Trim()))
            {
                return RedirectToAction("Index", "Site");
            }
            else
            {
                var list = db.Products.Where(m => m.Status == 1).OrderByDescending(m => m.Created_at);
                var l = list.ToList();
                List<MProduct> rsList = new List<MProduct>();
                foreach (MProduct mPro in l)
                {
                    var ca = db.Categorys.Find(mPro.CateID);
                    var paCa = db.Categorys.Find(ca.ParentId);


                    if (mPro.Name.ToUpper().Contains(key.ToUpper()) || ca.Name.ToUpper().Contains(key.ToUpper()) || paCa.Name.ToUpper().Contains(key.ToUpper()))
                        rsList.Add(mPro);
                }
                ViewBag.Count = list.Count();
                Session["keywords"] = key;
                return View(rsList.ToPagedList(pageNumber, pageSize));
            }
           
        }
        public ActionResult Contact()
        {
            return View();
        }
        [HttpPost]
        public ActionResult SubmitContact(MContact mContact)
        {
            mContact.FullName = Request.Form["Fullname"];
            mContact.Email = Request.Form["Email"];
            mContact.Phone = Convert.ToInt32(Request.Form["Phone"]);
            mContact.Title = Request.Form["Title"];
            mContact.Detail = Request.Form["Detail"];
            mContact.Status = 1;
            mContact.Created_at = DateTime.Now;
            mContact.Updated_at = DateTime.Now;
            mContact.Updated_by = 1;
            db.Contacts.Add(mContact);
            db.SaveChanges();
            Notification.set_flash("Chúng tôi sẽ phản hồi lại trong thời gian sớm nhất. Xin cảm ơn!", "success");
            return RedirectToAction("Contact", "Site");
        }

        public ActionResult AddRate(int pID, int rate, String comment)
        {
            if (Session["User_ID"] == null)
                return Json(new { result = 0 });
            int uID = int.Parse(Session["User_ID"].ToString());
            var orderDetailCount = (from o in db.Orders
                                    join d in db.Orderdetails on o.Id equals d.OrderId 
                                    where o.CustemerId == uID && d.ProductId == pID
                                    select new { d.Id }).Count();
            var rateCount = db.Rates.Where(r => r.ProductID == pID && r.UserID == uID).Count();

            if (rateCount >= orderDetailCount)
            {
                return Json(new { result = 1 });
            }

            db.Rates.Add(new MRate()
            {
                ProductID = pID,
                UserID = uID,
                UName = Session["User_Name"].ToString(),
                Comment = comment,
                Rate = rate,
                CreateAt = DateTime.Now
            });
            db.SaveChanges();
            var rateSum = db.Rates.Where(p => p.ProductID == pID).Sum(i=>i.Rate);
            rateCount = db.Rates.Where(r => r.ProductID == pID && r.UserID == uID).Count();

            db.Products.Find(pID).ProRate = ((double)rateSum) / rateCount;
            db.SaveChanges();
            return Json(new { result = 2 });
        }
    }
}