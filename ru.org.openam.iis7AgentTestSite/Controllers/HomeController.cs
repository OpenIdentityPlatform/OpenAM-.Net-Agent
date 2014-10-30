using System;
using System.Web;
using System.Web.Mvc;

namespace ru.org.openam.iis7AgentTestSite.Controllers
{
	public class HomeController : Controller
	{
		[HttpGet]
		public ActionResult Index()
		{
			return View();
		}

		[HttpPost]
		public ActionResult Post()
		{
			return RedirectToAction("Index");
		}

		public ActionResult Logoff()
		{
			var c =new HttpCookie("svbid", null);
			c.Expires = DateTime.Now;
			c.Domain = "rapidsoft.ru";
			Response.SetCookie(c);
			return RedirectToAction("Index");
		} 
	}
}