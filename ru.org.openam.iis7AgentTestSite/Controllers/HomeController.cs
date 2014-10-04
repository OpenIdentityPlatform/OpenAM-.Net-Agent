using System.Web;
using System.Web.Mvc;
using ru.org.openam.sdk;
using ru.org.openam.sdk.auth;
using ru.org.openam.sdk.auth.callback;

namespace ru.org.openam.iis7AgentTestSite.Controllers
{
	public class HomeController : Controller
	{
		public ActionResult Index()
		{
			return View();
		}

		[HttpGet]
		public ActionResult Login()
		{
			return View();
		}

		[HttpPost]
		public ActionResult Login(string login, string password)
		{	
 			var sid = Auth.login("/clients", indexType.service, "ldap", new Callback[] { new NameCallback(login), new PasswordCallback(password) }).sessionId;
			Response.SetCookie(new HttpCookie(Config.getCookieName(), sid));

			return RedirectToAction("Index");
		}
	}
}