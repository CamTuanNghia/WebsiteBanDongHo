using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WatchStore.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

namespace WatchStore.Areas.Admin.Controllers
{
    public class OrderController : BaseController
    {
        private WatchStoreDbContext db = new WatchStoreDbContext();

        public ActionResult Index()
        {
            ViewBag.countTrash = db.Orders.Where(m => m.Trash == 1).Count();
            var results = (from od in db.Orderdetails
                           join o in db.Orders on od.OrderId equals o.Id
                           where o.Trash != 1

                           group od by new { od.OrderId, o } into groupb
                           orderby groupb.Key.o.CreateDate descending
                           select new ListOrder
                           {
                               ID = groupb.Key.OrderId,
                               SAmount = groupb.Sum(m => m.Amount),
                               CustomerName = groupb.Key.o.DeliveryName,
                               Status = groupb.Key.o.Status,
                               CreateDate = groupb.Key.o.CreateDate,
                               ExportDate = groupb.Key.o.ExportDate,


                           });

            return View(results.ToList());
        }
        public ActionResult Trash()
        {
            ViewBag.countTrash = db.Orders.Where(m => m.Status == 0).Count();
            var results = (from od in db.Orderdetails
                           join o in db.Orders on od.OrderId equals o.Id
                           where o.Trash == 1

                           group od by new { od.OrderId, o } into groupb
                           orderby groupb.Key.o.CreateDate descending
                           select new ListOrder
                           {
                               ID = groupb.Key.OrderId,
                               SAmount = groupb.Sum(m => m.Amount),
                               CustomerName = groupb.Key.o.DeliveryName,
                               CustomerAddress = groupb.Key.o.DeliveryAddress,
                               CustomerEmail = groupb.Key.o.DeliveryEmail,
                               Status = groupb.Key.o.Status,
                               CreateDate = groupb.Key.o.CreateDate,
                               ExportDate = groupb.Key.o.ExportDate,
                           });

            return View(results.ToList());
        }

        public ActionResult DelTrash(int? id)
        {
            MOrder mOrder = db.Orders.Find(id);
            mOrder.Trash = 1;

            mOrder.Updated_at = DateTime.Now;
            mOrder.Updated_by = 1;
            db.Entry(mOrder).State = EntityState.Modified;
            db.SaveChanges();
            Notification.set_flash("Đã hủy đơn hàng!" + " ID = " + id, "success");
            return RedirectToAction("Index");
        }

        
        public ActionResult UpStatus(int? id)
        {
            MOrder mOrder = db.Orders.Find(id);
            mOrder.Status = mOrder.Status+1;

            mOrder.Updated_at = DateTime.Now;
            mOrder.Updated_by = 1;
            db.Entry(mOrder).State = EntityState.Modified;
            db.SaveChanges();
            Notification.set_flash("Đã cập nhật trạng thái đơn hàng !" + " ID = " + id, "success");
            return RedirectToAction("Index");
        }
        public ActionResult Undo(int? id)
        {
            MOrder mOrder = db.Orders.Find(id);
            mOrder.Trash = 0;

            mOrder.Updated_at = DateTime.Now;
            mOrder.Updated_by = int.Parse(Session["Admin_ID"].ToString());
            db.Entry(mOrder).State = EntityState.Modified;
            db.SaveChanges();
            Notification.set_flash("Khôi phục thành công!" + " ID = " + id, "success");
            return RedirectToAction("Trash");
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                Notification.set_flash("Không tồn tại đơn hàng!", "warning");
                return RedirectToAction("Index", "Order");
            }
            MOrder mOrder = db.Orders.Find(id);
            if (mOrder == null)
            {
                Notification.set_flash("Không tồn tại  đơn hàng!", "warning");
                return RedirectToAction("Index", "Order");
            }
            ViewBag.orderDetails = db.Orderdetails.Where(m => m.OrderId == id).ToList();
            ViewBag.productOrder = db.Products.ToList();
            return View(mOrder);
        }


        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                Notification.set_flash("Không tồn tại đơn hàng!", "warning");
                return RedirectToAction("Trash", "Order");
            }
            MOrder mOrder = db.Orders.Find(id);
            if (mOrder == null)
            {
                Notification.set_flash("Không tồn tại đơn hàng!", "warning");
                return RedirectToAction("Trash", "Order");
            }
            ViewBag.orderDetails = db.Orderdetails.Where(m => m.OrderId == id).ToList();
            ViewBag.productOrder = db.Products.ToList();
            return View(mOrder);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            MOrder mOrder = db.Orders.Find(id);
            db.Orders.Remove(mOrder);
            db.SaveChanges();
            Notification.set_flash("Đã xóa đơn hàng!", "success");
            return RedirectToAction("Trash");
        }
        [HttpPost]
        public JsonResult changeStatus(int id, int op)
        {
            MOrder mOrder = db.Orders.Find(id);
            if (op == 1) { mOrder.Status = 1; } else if (op == 2) { mOrder.Status = 2; } else { mOrder.Status = 3; }

            mOrder.ExportDate = DateTime.Now;
            mOrder.Updated_at = DateTime.Now;
            mOrder.Updated_by = int.Parse(Session["Admin_ID"].ToString());
            db.Entry(mOrder).State = EntityState.Modified;
            db.SaveChanges();
            return Json(new { s = mOrder.Status, t = mOrder.ExportDate.ToString() });
        }

        public ActionResult ExportInvoice(int? id)
        {
            if (id == null)
            {
                Notification.set_flash("Không tồn tại đơn hàng!", "warning");
                return RedirectToAction("Index", "Order");
            }

            MOrder mOrder = db.Orders.Find(id);
            if (mOrder == null)
            {
                Notification.set_flash("Không tồn tại đơn hàng!", "warning");
                return RedirectToAction("Index", "Order");
            }

            var orderDetails = db.Orderdetails.Where(m => m.OrderId == id).ToList();
            if (orderDetails == null || !orderDetails.Any())
            {
                Notification.set_flash("Không có chi tiết đơn hàng!", "warning");
                return RedirectToAction("Details", new { id = id });
            }
            // Tạo tài liệu PDF
            Document doc = new Document(PageSize.A4);
            MemoryStream memoryStream = new MemoryStream();
            PdfWriter writer = PdfWriter.GetInstance(doc, memoryStream);
            doc.Open();

            // Nhúng phông chữ Unicode
            string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "Arial.ttf");
            BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            Font titleFont = new Font(baseFont, 16, Font.BOLD);
            Font bodyFont = new Font(baseFont, 12, Font.NORMAL);

            // Thêm tiêu đề
            //Font titleFont = FontFactory.GetFont("Arial", 16, Font.BOLD);
            Paragraph title = new Paragraph("Hóa Đơn", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            doc.Add(title);

            // Thêm thông tin đơn hàng
            //Font bodyFont = FontFactory.GetFont("Arial", 12, Font.NORMAL);
            doc.Add(new Paragraph($"Mã đơn hàng: {mOrder.Id}", bodyFont));
            doc.Add(new Paragraph($"Tên khách hàng: {mOrder.DeliveryName}", bodyFont));
            doc.Add(new Paragraph($"Địa chỉ giao hàng: {mOrder.DeliveryAddress}", bodyFont));
            doc.Add(new Paragraph($"Ngày tạo: {mOrder.CreateDate.ToString("dd/MM/yyyy")}", bodyFont));
            doc.Add(new Paragraph(" "));

            // Thêm bảng chi tiết đơn hàng
            PdfPTable table = new PdfPTable(4);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 40f, 20f, 20f, 20f });

            // Thêm tiêu đề bảng
            table.AddCell(new PdfPCell(new Phrase("Tên sản phẩm", titleFont)));
            table.AddCell(new PdfPCell(new Phrase("Số lượng", titleFont)));
            table.AddCell(new PdfPCell(new Phrase("Đơn giá", titleFont)));
            table.AddCell(new PdfPCell(new Phrase("Thành tiền", titleFont)));

            // Thêm các hàng vào bảng
            foreach (var item in orderDetails)
            {
                var productName = db.Products.Where(p => p.ID == item.ProductId).Select(p => p.Name).FirstOrDefault();
                table.AddCell(new PdfPCell(new Phrase(productName, bodyFont)));
                table.AddCell(new PdfPCell(new Phrase(item.Quantity.ToString(), bodyFont)));
                table.AddCell(new PdfPCell(new Phrase(item.Price.ToString("C"), bodyFont)));
                table.AddCell(new PdfPCell(new Phrase((item.Quantity * item.Price).ToString("C"), bodyFont)));
            }

            doc.Add(table);

            // Thêm tổng tiền
            doc.Add(new Paragraph(" "));
            double totalAmount = orderDetails.Sum(m => m.Quantity * m.Price);
            doc.Add(new Paragraph($"Tổng tiền: {totalAmount.ToString("C")}", titleFont));

            // Đóng tài liệu
            doc.Close();
            writer.Close();
            memoryStream.Close();

            // Trả về file PDF
            byte[] fileBytes = memoryStream.ToArray();
            return File(fileBytes, "application/pdf", $"HoaDon_{mOrder.Id}.pdf");
        }
    }
}
