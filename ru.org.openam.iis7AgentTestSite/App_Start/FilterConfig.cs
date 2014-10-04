using System.Web;
using System.Web.Mvc;

namespace ru.org.openam.iis7AgentTestSite
{
	public class FilterConfig
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
		}
	}
}
