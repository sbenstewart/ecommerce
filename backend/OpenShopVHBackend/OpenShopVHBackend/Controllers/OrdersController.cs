﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using OpenShopVHBackend.Models;
using OpenShopVHBackend.BussinessLogic;
using Hangfire;
using Syncfusion.EJ.Export;
using Syncfusion.JavaScript.Models;
using Syncfusion.XlsIO;

namespace OpenShopVHBackend.Controllers
{
    [AccessAuthorizeAttribute(Roles = "Admin, SalesAdmin")]
    public class OrdersController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        //GET: Orders
        public ActionResult Index()
        {
            var orders = db.Orders
                .Include(p => p.Client)
                .Include(p => p.DeviceUser)
                .OrderByDescending(o => o.OrderId)
                .ToList()
                .Select(s => new OrderViewModel()
                {
                    OrderId = s.OrderId,
                    CardCode = s.Client.CardCode,
                    Comment = s.Comment,
                    RemoteId = s.RemoteId,
                    DeliveryDate = s.DeliveryDate,
                    SalesPersonCode = s.DeviceUser.SalesPersonId,
                    SalesPersonName = s.DeviceUser.Name,
                    Status = s.Status.ToString(),
                    DateCreated = s.DateCreated,
                    LastErrorMessage = s.LastErrorMessage,
                    Total = s.GetTotal(),
                    UserId = s.DeviceUser.DeviceUserId
                });

            ViewBag.datasource = orders;

            return View(orders.ToList());
        }

        // GET: Orders/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        // GET: Orders/Create
        public ActionResult Create()
        {
            return View();
        }

        [AutomaticRetry(Attempts = 0)]
        public void CreateQuotationOrderOnSap(int orderId)
        {

            String message = "";

            try
            {
                Quotation salesorder = new Quotation();
                message = salesorder.AddQuotation(orderId);
            }
            catch (Exception e)
            {
                MyLogger.GetInstance.Error(e.Message, e);
            }
        }


        [AutomaticRetry(Attempts = 0)]
        public void CreateDraftOrderOnSap(int userId, int orderId)
        {

            String message = "";

            try
            {
                PreliminarSalesOrder salesorder = new PreliminarSalesOrder();
                message = salesorder.AddSalesOrder(userId, orderId);
            }
            catch (Exception e)
            {
                MyLogger.GetInstance.Error(e.Message, e);
            }
        }

        public ActionResult Process(int userId, int id)
        {
            //BackgroundJob.Enqueue(() => CreateDraftOrderOnSap(userId, id));

            SalesOrder salesorder = new SalesOrder();
            salesorder.AddSalesOrder(id, userId);

            return RedirectToAction("Index");
        }

        // POST: Orders/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "OrderId,RemoteId,DateCreated,Status,Total,CardCode,Comment,SalesPersonCode,Series")] Order order)
        {
            if (ModelState.IsValid)
            {
                db.Orders.Add(order);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(order);
        }

        // GET: Orders/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "OrderId,RemoteId,DateCreated,Status,Total,CardCode,Comment,SalesPersonCode,Series")] Order order)
        {
            if (ModelState.IsValid)
            {
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(order);
        }

        // GET: Orders/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Order order = db.Orders.Find(id);
            db.Orders.Remove(order);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public void ExportToExcel(string GridModel)
        {
            var orders = db.Orders
               .Include(p => p.Client).Include(p => p.DeviceUser)
               .OrderByDescending(o => o.OrderId)
               .ToList()
               .Select(s => new OrderViewModel()
               {
                   OrderId = s.OrderId,
                   RemoteId = s.RemoteId,
                   SalesPersonCode = s.DeviceUser.SalesPersonId,
                   Status = s.Status.ToString(),
                   Total = s.GetTotal(),
                   CardCode = s.Client.CardCode,
                   Comment = s.Comment,
                   DeliveryDate = s.DeliveryDate,
                   DateCreated = s.CreatedDate.ToShortDateString(),
                   LastErrorMessage = s.LastErrorMessage
               });

            ExcelExport exp = new ExcelExport();

            GridProperties obj = (GridProperties)Syncfusion.JavaScript.Utils.DeserializeToModel(typeof(GridProperties), GridModel);

            //exp.Export(obj, orders, "Export.xlsx", ExcelVersion.Excel2010, false, false, "flat-saffron");

        }

        public void ExportToPdf(string GridModel)
        {
            var orders = db.Orders
               .Include(p => p.Client).Include(p => p.DeviceUser)
               .OrderByDescending(o => o.OrderId)
               .ToList()
               .Select(s => new OrderViewModel()
               {
                   OrderId = s.OrderId,
                   RemoteId = s.RemoteId,
                   SalesPersonCode = s.DeviceUser.SalesPersonId,
                   Status = s.Status.ToString(),
                   Total = s.GetTotal(),
                   CardCode = s.Client.CardCode,
                   Comment = s.Comment,
                   DeliveryDate = s.DeliveryDate,
                   DateCreated = s.CreatedDate.ToShortDateString(),
                   LastErrorMessage = s.LastErrorMessage
               });

            PdfExport exp = new PdfExport();

            GridProperties obj = (GridProperties)Syncfusion.JavaScript.Utils.DeserializeToModel(typeof(GridProperties), GridModel);

            exp.Export(obj, orders, "Export.pdf", false, false, "flat-saffron");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
