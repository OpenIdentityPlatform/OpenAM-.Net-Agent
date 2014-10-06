using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ru.org.openam.sdk;
using ru.org.openam.sdk.auth;
using ru.org.openam.sdk.auth.callback;

namespace ru.org.openam.loginSite.Controllers
{
	public class HomeController : Controller
	{
		public ActionResult Index()
		{
			return View();
		}

		[HttpPost]
		public ActionResult Login(string login, string password)
		{	
 			var sid = Auth.login("/clients", indexType.service, "ldap", new Callback[] { new NameCallback(login), new PasswordCallback(password) }).sessionId;
			var cookie = new HttpCookie("svbid", sid);
			cookie.Domain = "rapidsoft.ru";
			Response.SetCookie(cookie);

			return Redirect("https://ibank.staging.rapidsoft.ru");
		}
	}
}