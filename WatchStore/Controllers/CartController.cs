using WatchStore.Library;
using WatchStore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WatchStore.Controllers
{
    public class CartController : Controller
    {
        private WatchStoreDbContext db = new WatchStoreDbContext();
        // GET: Cart
        public ActionResult Index()
        {
            int userId = Convert.ToInt32(Session["User_ID"]);
            var cart = db.Carts.Include("CartDetails").FirstOrDefault(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedDate = DateTime.Now,
                    CartDetails = new List<CartDetail>()
                };
                db.Carts.Add(cart);
                db.SaveChanges();
            }

            foreach (var detail in cart.CartDetails)
            {
                detail.Product = db.Products.FirstOrDefault(p => p.ID == detail.ProductId);
            }

            return View(cart);
        }
        [HttpPost]
        public ActionResult Add(int pid, int qty)
        {
            var p = db.Products.FirstOrDefault(m => m.Status == 1 && m.ID == pid);
            if (p == null || p.Quantity < qty)
            {
                return Json(new { result = 3 });
            }

            int userId = Convert.ToInt32(Session["User_ID"]);
            var cart = db.Carts.Include("CartDetails").FirstOrDefault(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId, CreatedDate = DateTime.Now };
                db.Carts.Add(cart);
                db.SaveChanges();
            }

            var cartDetail = cart.CartDetails.FirstOrDefault(ci => ci.ProductId == pid);
            if (cartDetail == null)
            {
                cartDetail = new CartDetail
                {
                    CartId = cart.CartId,
                    ProductId = p.ID,
                    Quantity = qty,
                    Price = p.Discount == 1 ? p.ProPrice : p.Price
                };
                db.CartDetails.Add(cartDetail);
            }
            else
            {
                cartDetail.Quantity += qty;
            }
            db.SaveChanges();

            return Json(new { result = 1 });
        }

        public ActionResult Set(int pid, int qty)
        {

            var p = db.Products.FirstOrDefault(m => m.Status == 1 && m.ID == pid);
            if (p == null || p.Quantity < qty)
            {
                return Json(new { result = 2 });
            }

            int userId = Convert.ToInt32(Session["User_ID"]);
            var cart = db.Carts.Include("CartDetails").FirstOrDefault(c => c.UserId == userId);
            if (cart != null)
            {
                var cartItem = cart.CartDetails.FirstOrDefault(ci => ci.ProductId == pid);
                if (cartItem != null)
                {
                    cartItem.Quantity = qty;
                    db.SaveChanges();
                    return Json(new { result = 1 });
                }
            }
            return Json(new { result = 0 });
        }

        public JsonResult Update(int pid, String option)
        {
            int userId = Convert.ToInt32(Session["User_ID"]);
            var cart = db.Carts.Include("CartDetails").FirstOrDefault(c => c.UserId == userId);
            if (cart != null)
            {
                var cartItem = cart.CartDetails.FirstOrDefault(ci => ci.ProductId == pid);
                if (cartItem != null)
                {
                    switch (option)
                    {
                        case "add":
                            cartItem.Quantity++;
                            break;
                        case "minus":
                            cartItem.Quantity--;
                            break;
                        case "remove":
                            db.CartDetails.Remove(cartItem);
                            break;
                    }
                    db.SaveChanges();
                    return Json(1);
                }
            }
            return Json(null);
        }
        public ActionResult RemoveAll()
        {
            int userId = Convert.ToInt32(Session["User_ID"]);
            var cart = db.Carts.Include("CartDetails").FirstOrDefault(c => c.UserId == userId);
            if (cart != null)
            {
                db.CartDetails.RemoveRange(cart.CartDetails);
                db.Carts.Remove(cart);
                db.SaveChanges();
            }
            Notification.set_flash("Đã xóa toàn bộ sản phẩm trong giỏ hàng!", "success");
            return Redirect("~/gio-hang");
        }
        //public ActionResult Checkout()
        //{
        //    if (Session["User_Name"] != null)
        //    {
        //        int userId = Convert.ToInt32(Session["User_ID"]);
        //        var user = db.Users.FirstOrDefault(m => m.ID == userId);
        //        if (user != null)
        //        {
        //            ViewBag.Info = user;
        //            return View();
        //        }
        //    }
        //    return RedirectToAction("Index", "Cart");
        //}

        public ActionResult Checkout()
        {
            int userId = Convert.ToInt32(Session["User_ID"]);
            var cart = db.Carts.Include("CartDetails").FirstOrDefault(c => c.UserId == userId);

            if (cart == null || !cart.CartDetails.Any())
            {
                return RedirectToAction("Index", "Cart"); // Chuyển hướng tới trang giỏ hàng nếu giỏ hàng rỗng
            }

            foreach (var detail in cart.CartDetails)
            {
                detail.Product = db.Products.FirstOrDefault(p => p.ID == detail.ProductId);
            }

            var user = db.Users.FirstOrDefault(u => u.ID == userId);
            if (user == null)
            {
                return RedirectToAction("Index", "Home"); // Chuyển hướng tới trang chủ nếu không tìm thấy người dùng
            }

            var viewModel = new CheckoutViewModel
            {
                Cart = cart,
                User = user
            };

            return View(viewModel);
        }

        [HttpPost]
        public JsonResult Payment(String Email,String Address, String FullName, String Phone)
        {
            int userId = Convert.ToInt32(Session["User_ID"]);
            var order = new MOrder
            {
                Code = DateTime.Now.ToString("yyyyMMddhhMMss"),
                CustemerId = userId,
                CreateDate = DateTime.Now,
                DeliveryAddress = Address,
                DeliveryEmail = Email,
                DeliveryPhone = Phone,
                DeliveryName = FullName,
                Status = 1
            };
            db.Orders.Add(order);
            db.SaveChanges();

            var cart = db.Carts.Include("CartDetails").FirstOrDefault(c => c.UserId == userId);
            if (cart != null)
            {
                foreach (var c in cart.CartDetails)
                {
                    var orderdetails = new MOrderdetail
                    {
                        OrderId = order.Id,
                        ProductId = c.ProductId,
                        Price = c.Price,
                        Quantity = c.Quantity,
                        Amount = c.Price * c.Quantity
                    };
                    db.Orderdetails.Add(orderdetails);
                }
                db.CartDetails.RemoveRange(cart.CartDetails);
                db.Carts.Remove(cart);
                db.SaveChanges();
            }

            Notification.set_flash("Bạn đã đặt hàng thành công!", "success");
            return Json(true);

        }

        public JsonResult Tesst()
        {
            if (Session["User_Name"] != null)
                return Json(1);
            return Json(0);
        }
        public JsonResult CheckAuth()
        {
            if (Session["User_Name"] != null)
                return Json(1);
            return Json(0);
        }
    }
}