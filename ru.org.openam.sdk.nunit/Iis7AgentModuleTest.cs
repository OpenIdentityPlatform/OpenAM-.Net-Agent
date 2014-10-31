using System;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Web;
using Moq;
using NUnit.Framework;
using ru.org.openam.iis7Agent;
using ru.org.openam.sdk.auth;
using ru.org.openam.sdk.auth.callback;

namespace ru.org.openam.sdk.nunit
{
	[TestFixture]
	public class Iis7AgentModuleTest
	{	 
		private Mock<HttpContextBase> _httpContext;
		private Mock<HttpRequestBase> _httpRequest;
		private Mock<HttpResponseBase> _httpResponse;

		[TestFixtureSetUp]
		public void Init()
		{
			_httpContext = new Mock<HttpContextBase>();
			_httpRequest = new Mock<HttpRequestBase>();
			_httpResponse = new Mock<HttpResponseBase>();

			_httpContext.SetupGet(c => c.Request).Returns(_httpRequest.Object);
			_httpContext.SetupGet(c => c.Response).Returns(_httpResponse.Object);
		}

		#region Fqdn

		[Test]
		public void Fqdn_редиректит_если_урлы_не_совпадают()
		{
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/"));

			_httpResponse.Setup(response => response.Redirect("http://ibank.staging.rapidsoft.ru:80/"));

			var module = new iis7AgentModule();
			module.OnAuthentication(_httpContext.Object);

			_httpResponse.VerifyAll();
		}

		[Test]
		public void Fqdn_не_переопределяет_протокол_и_порт()
		{
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/"));

			_httpResponse.Setup(response => response.Redirect("https://ibank.staging.rapidsoft.ru:444/"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.override.host")).Returns("true");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.fqdn.default")).Returns("ibank.staging.rapidsoft.ru");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.agenturi.prefix"))
				.Returns("http://ibank.staging.rapidsoft.ru:80/");
			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpResponse.VerifyAll();
		}

		[Test]
		public void Fqdn_не_редиректит_если_отключена_настройка()
		{
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("http://www.mysite.com/"));

			Login();

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.override.host")).Returns("false");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");
			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpResponse.Verify(r => r.Redirect("http://ibank.staging.rapidsoft.ru:80/"), Times.Never());
		}

		[Test]
		public void Fqdn_не_редиректит_если_нет_редирект_урла()
		{
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("http://www.mysite.com/"));

			Login();

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.override.host")).Returns("true");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");
			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpResponse.Verify(r => r.Redirect("http://ibank.staging.rapidsoft.ru:80/"), Times.Never());
		}

		[Test]
		public void Fqdn_не_редиректит_если_урлы_совпадают()
		{
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("http://ibank.staging.rapidsoft.ru:80/"));

			Login();

			var module = new iis7AgentModule();
			module.OnAuthentication(_httpContext.Object);

			_httpResponse.Verify(r => r.Redirect("http://ibank.staging.rapidsoft.ru:80/"), Times.Never());
		}

		#endregion

		#region логофф

		[Test]
		public void Тречит_логофф_урл()
		{
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/"));

			_httpResponse.Setup(response => response.Redirect("https://www.mysite.com:444/logoff"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetOrderedArray("com.sun.identity.agents.config.agent.logout.url"))
				.Returns(new[] {"https://www.mysite.com:444/"});
			agent.Setup(a => a.GetFirst("com.sun.identity.agents.config.logout.redirect.url"))
				.Returns("https://www.mysite.com:444/logoff");
			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpResponse.VerifyAll();
		}

		[Test]
		public void Тречит_логофф_урл_и_редиректит_на_логин()
		{
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/"));

			_httpResponse.Setup(response => response.Redirect("https://www.mysite.com:444/login"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetOrderedArray("com.sun.identity.agents.config.agent.logout.url"))
				.Returns(new[] {"https://www.mysite.com:444/"});
			agent.Setup(a => a.GetFirst("com.sun.identity.agents.config.login.url")).Returns("https://www.mysite.com:444/login");
			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpResponse.VerifyAll();
		}

		[Test]
		public void Не_тречит_логофф_если_нет_настройки()
		{
			Login();

			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetOrderedArray("com.sun.identity.agents.config.agent.logout.url")).Returns(new string[0]);
			agent.Setup(a => a.GetFirst("com.sun.identity.agents.config.logout.redirect.url"))
				.Returns("https://www.mysite.com:444/logoff");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpResponse.Verify(r => r.Redirect("https://www.mysite.com:444/logoff"), Times.Never());
		}

		[Test]
		public void Ресетит_куки_на_логофф()
		{
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetOrderedArray("com.sun.identity.agents.config.agent.logout.url"))
				.Returns(new[] {"https://www.mysite.com:444/"});
			agent.Setup(a => a.GetFirst("com.sun.identity.agents.config.logout.redirect.url"))
				.Returns("https://www.mysite.com:444/logoff");
			agent.Setup(a => a.GetOrderedArray("com.sun.identity.agents.config.logout.cookie.reset")).Returns(new[] {"test"});
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.agenturi.prefix"))
				.Returns("http://ibank.staging.rapidsoft.ru:80/");
			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpResponse.Verify(response => response.Redirect("https://www.mysite.com:444/logoff"));
			_httpResponse.Verify(r => r.AddHeader("Set-Cookie", "test"));
		}

		#endregion

		[Test]
		public void Пропускает_фри_урлы_со_звездой()
		{
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/static/image.jpg"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetOrderedArray("com.sun.identity.agents.config.notenforced.url"))
				.Returns(new[] {"https://www.mysite.com:444/static/*"});

			_httpContext.SetupSet(c => c.User = It.Is<GenericPrincipal>(p => p.Identity.Name == "")).Verifiable();

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpContext.VerifyAll();
		}

		[Test]
		public void Не_пропускает_левые_фри_урлы_со_звездой()
		{
			InitEmptyCookies();
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/Home/Index"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetOrderedArray("com.sun.identity.agents.config.notenforced.url"))
				.Returns(new[] {"https://www.mysite.com:444/static/*"});
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");

			_httpResponse.SetupSet(c => c.StatusCode = 401).Verifiable();
			_httpResponse.Setup(c => c.End()).Verifiable();

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpResponse.VerifyAll();
		}

		[Test]
		public void Пропускает_фри_урлы_без_звезды()
		{
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/static/image.jpg"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetOrderedArray("com.sun.identity.agents.config.notenforced.url"))
				.Returns(new[] {"https://www.mysite.com:444/static/image.jpg"});

			_httpContext.SetupSet(c => c.User = It.Is<GenericPrincipal>(p => p.Identity.Name == "")).Verifiable();

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpContext.VerifyAll();
		}

		[Test]
		public void Не_пропускает_левые_фри_урлы_без_звезды()
		{
			InitEmptyCookies();
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/Home/Index"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetOrderedArray("com.sun.identity.agents.config.notenforced.url"))
				.Returns(new[] {"https://www.mysite.com:444/static/image.jpg"});
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");

			_httpResponse.SetupSet(c => c.StatusCode = 401).Verifiable();
			_httpResponse.Setup(c => c.End()).Verifiable();

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpResponse.VerifyAll();
		}

		private void InitEmptyCookies(){
			var cookieCollection = new HttpCookieCollection();
			_httpRequest.SetupGet(r => r.Cookies).Returns(cookieCollection);
		}

		private void Login(){
			var cookieCollection = new HttpCookieCollection();
 			var sid = Auth.login("/clients", indexType.service, "ldap", new Callback[] { new NameCallback("11111111111"), new PasswordCallback("1111111111") }).sessionId;
			cookieCollection.Set(new HttpCookie("svbid", sid));
			_httpRequest.SetupGet(r => r.Cookies).Returns(cookieCollection);
		}
	}
}

