using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using puck.core;
using puck.core.Entities;
using System.Web.Security;
using puck.core.Filters;

namespace puck.core.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /admin/Home/
        //[Auth]
        public ActionResult Index()
        {
            return View();
        }

        //
        // GET: /admin/Home/Details/5

        public ActionResult Details(int id)
        {
            return View();
        }

        //
        // GET: /admin/Home/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /admin/Home/Create

        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        //
        // GET: /admin/Home/Edit/5

        public ActionResult Edit(int id)
        {
            return View();
        }

        //
        // POST: /admin/Home/Edit/5

        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        //
        // GET: /admin/Home/Delete/5

        public ActionResult Delete(int id)
        {
            return View();
        }

        //
        // POST: /admin/Home/Delete/5

        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
