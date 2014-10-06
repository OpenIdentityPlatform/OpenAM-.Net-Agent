using System.Web;
using System.Web.Mvc;

namespace ru.org.openam.loginSite
{
	public class FilterConfig
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
		}
	}
}
