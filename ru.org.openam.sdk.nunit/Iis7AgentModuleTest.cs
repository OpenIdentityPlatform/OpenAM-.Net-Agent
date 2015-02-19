using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

		[SetUp]
		public void Init()
		{
			_httpContext = new Mock<HttpContextBase>();
			_httpRequest = new Mock<HttpRequestBase>();
			_httpResponse = new Mock<HttpResponseBase>();
			
			_httpContext.SetupGet(c => c.Request).Returns(_httpRequest.Object);
			_httpContext.SetupGet(c => c.Response).Returns(_httpResponse.Object);
			_httpContext.SetupGet(c => c.Items).Returns(new Dictionary<string, object>());
			_httpRequest.SetupGet(r => r.Cookies).Returns(new HttpCookieCollection());	
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
			
			//var module = new iis7AgentModule(agent.Object);
			//module.OnAuthentication(_httpContext.Object);

			var module = new Mock<iis7AgentModule>(agent.Object);
			module.CallBase = true;
			module.Setup(m => m.CompleteRequest(_httpContext.Object));
			module.Object.OnAuthentication(_httpContext.Object);

			module.Verify(x => x.CompleteRequest(It.IsAny<HttpContextBase>()), Times.Once());
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
			var module = new Mock<iis7AgentModule>(agent.Object);
			module.CallBase = true;
			module.Setup(m => m.CompleteRequest(_httpContext.Object));
			module.Object.OnAuthentication(_httpContext.Object);

            module.Verify(x => x.CompleteRequest(It.IsAny<HttpContextBase>()), Times.Once());
			_httpResponse.Verify(r => r.Redirect("http://ibank.staging.rapidsoft.ru:80/"), Times.Never());
		}

		[Test]
		public void Fqdn_не_редиректит_если_урлы_совпадают()
		{
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("http://ibank.staging.rapidsoft.ru:80/"));

			Login();

			var module = new Mock<iis7AgentModule>();
			module.CallBase = true;
			module.Setup(m => m.CompleteRequest(_httpContext.Object));
			module.Object.OnAuthentication(_httpContext.Object);

            module.Verify(x => x.CompleteRequest(It.IsAny<HttpContextBase>()), Times.Once());
			_httpResponse.Verify(r => r.Redirect("http://ibank.staging.rapidsoft.ru:80/"), Times.Never());
		}

		#endregion

		#region логофф

		[Test]
		public void Тречит_логофф_урл()
		{
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/"));

			_httpResponse.Setup(response => response.Redirect("https://www.mysite.com:444/logoff")).Verifiable();

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetOrderedArray("com.sun.identity.agents.config.agent.logout.url"))
				.Returns(new[] {"https://www.mysite.com:444/"});
			agent.Setup(a => a.GetFirst("com.sun.identity.agents.config.logout.redirect.url"))
				.Returns("https://www.mysite.com:444/logoff");
			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpResponse.Verify();
		}

		[Test]
		public void Тречит_логофф_урл_и_редиректит_на_логин()
		{
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/"));

			_httpResponse.Setup(response => response.Redirect("https://www.mysite.com:444/login")).Verifiable();

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetOrderedArray("com.sun.identity.agents.config.agent.logout.url"))
				.Returns(new[] {"https://www.mysite.com:444/"});
			agent.Setup(a => a.GetFirst("com.sun.identity.agents.config.login.url")).Returns("https://www.mysite.com:444/login");
			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpResponse.Verify();
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

			var module = new Mock<iis7AgentModule>(agent.Object);
			module.CallBase = true;
			module.Setup(m => m.CompleteRequest(_httpContext.Object));
			module.Object.OnAuthentication(_httpContext.Object);

            module.Verify(x => x.CompleteRequest(It.IsAny<HttpContextBase>()), Times.Once());

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

		#region фри урлы

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

			_httpContext.Verify();
		}

		[Test]
		public void Не_пропускает_левые_фри_урлы_со_звездой()
		{
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/Home/Index"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetOrderedArray("com.sun.identity.agents.config.notenforced.url"))
				.Returns(new[] {"https://www.mysite.com:444/static/*"});
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");

			_httpResponse.SetupSet(c => c.StatusCode = 401).Verifiable();
			
			var module = new Mock<iis7AgentModule>(agent.Object);
			module.CallBase = true;
			module.Setup(m => m.CompleteRequest(_httpContext.Object));
			module.Object.OnAuthentication(_httpContext.Object);

            module.Verify(x => x.CompleteRequest(It.IsAny<HttpContextBase>()), Times.Once());
			_httpResponse.Verify();
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

			_httpContext.Verify();
		}

		[Test]
		public void Не_пропускает_левые_фри_урлы_без_звезды()
		{
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/Home/Index"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetOrderedArray("com.sun.identity.agents.config.notenforced.url"))
				.Returns(new[] {"https://www.mysite.com:444/static/image.jpg"});
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");

			_httpResponse.SetupSet(c => c.StatusCode = 401).Verifiable();

			var module = new Mock<iis7AgentModule>(agent.Object);
			module.CallBase = true;
			module.Setup(m => m.CompleteRequest(_httpContext.Object));
			module.Object.OnAuthentication(_httpContext.Object);

			module.Verify(x => x.CompleteRequest(It.IsAny<HttpContextBase>()), Times.Once());
			_httpResponse.Verify();
		}

		[Test]
		public void Пропускает_фри_урлы_и_прописывает_юзера()
		{
			Login();

			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/static/image.jpg"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetOrderedArray("com.sun.identity.agents.config.notenforced.url")).Returns(new[] {"https://www.mysite.com:444/static/*"});
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.notenforced.url.attributes.enable")).Returns("true");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");

			_httpContext.SetupSet(c => c.User = It.Is<GenericPrincipal>(p => p.Identity.Name != "")).Verifiable();

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpContext.Verify();
		}

		#endregion

		#region Авторизация

		[Test]
		public void Авторизует()
		{
			Login();

			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("http://localhost.rapidsoft.ru:80"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");

			_httpContext.SetupSet(c => c.User = It.Is<GenericPrincipal>(p => p.Identity.Name != "")).Verifiable();

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpContext.VerifyAll();
		}

		[Test]
		public void Не_авторизует()
		{
			Login();

			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");

			_httpResponse.SetupSet(c => c.StatusCode = 401).Verifiable();
			var module = new Mock<iis7AgentModule>(agent.Object);
			module.CallBase = true;
			module.Setup(m => m.CompleteRequest(_httpContext.Object));
			module.Object.OnAuthentication(_httpContext.Object);

			module.Verify(x => x.CompleteRequest(It.IsAny<HttpContextBase>()), Times.Once());

			_httpContext.Verify();
		}

		[Test]
		public void Авторизует_если_настроено_без_авторизации()
		{
			Login();

			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.sso.only")).Returns("true");

			_httpContext.SetupSet(c => c.User = It.Is<GenericPrincipal>(p => p.Identity.Name != "")).Verifiable();

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpContext.VerifyAll();
		}

		[Test]
		public void Мапит_свойства_полиси_в_сервер_вираблы()
		{
			Login();

			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("http://localhost.rapidsoft.ru:80"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.profile.attribute.fetch.mode")).Returns("HTTP_HEADER");
			agent.Setup(a => a.GetArray("com.sun.identity.agents.config.profile.attribute.mapping"))
				.Returns(new[] {"[uid]=profile-pkn", "[mail]=profile-mail"});

			_httpRequest.SetupGet(c => c.ServerVariables).Returns(new NameValueCollection());

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			Assert.IsNotNull(_httpContext.Object.Items["profile-pkn"]);
			Assert.IsNotNull(_httpRequest.Object.ServerVariables["profile-pkn"]);
		}

		[Test]
		public void Мапит_свойства_полиси_в_куки()
		{
			Login();

			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("http://localhost.rapidsoft.ru:80"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.profile.attribute.fetch.mode")).Returns("HTTP_COOKIE");
			agent.Setup(a => a.GetArray("com.sun.identity.agents.config.profile.attribute.mapping"))
				.Returns(new[] {"[uid]=profile-pkn", "[mail]=profile-mail"});

			_httpRequest.SetupGet(c => c.ServerVariables).Returns(new NameValueCollection());

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			Assert.IsNotNull(_httpContext.Object.Items["profile-pkn"]);
			Assert.IsNotNull(_httpRequest.Object.Cookies["profile-pkn"]);
		}

		#endregion

		#region Проверка по ИП

		[Test]
		public void Проверяет_IP_и_пропускает()
		{
			Login();

			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("http://localhost.rapidsoft.ru:80"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.client.ip.validation.enable")).Returns("true");

			_httpRequest.SetupGet(c => c.UserHostAddress).Returns("127.0.0.2");
			_httpContext.SetupSet(c => c.User = It.Is<GenericPrincipal>(p => p.Identity.Name != "")).Verifiable();

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpContext.VerifyAll();
		}

		[Test]
		public void Проверяет_IP_и_не_пропускает()
		{
			Login();

			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("http://localhost.rapidsoft.ru:80"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.client.ip.validation.enable")).Returns("true");

			_httpRequest.SetupGet(c => c.UserHostAddress).Returns("127.0.0.3");
			_httpResponse.SetupSet(c => c.StatusCode = 401).Verifiable();
			var module = new Mock<iis7AgentModule>(agent.Object);
			module.CallBase = true;
			module.Setup(m => m.CompleteRequest(_httpContext.Object));
			module.Object.OnAuthentication(_httpContext.Object);

			module.Verify(x => x.CompleteRequest(It.IsAny<HttpContextBase>()), Times.Once());

			_httpContext.Verify();
		}

		[Test]
		public void Проверяет_IP_и_пропускает_с_заголовком()
		{
			Login();

			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("http://localhost.rapidsoft.ru:80"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.client.ip.validation.enable")).Returns("true");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.client.ip.header")).Returns("x-forwarded-for");

			var col = new NameValueCollection();
			col.Add("x-forwarded-for", "127.0.0.2, 127.0.0.3");
			_httpRequest.SetupGet(c => c.Headers).Returns(col);
			_httpContext.SetupSet(c => c.User = It.Is<GenericPrincipal>(p => p.Identity.Name != "")).Verifiable();

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpContext.Verify();
		}

		[Test]
		public void Проверяет_IP_и_пропускает_с_вираблами()
		{
			Login();

			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("http://localhost.rapidsoft.ru:80"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.client.ip.validation.enable")).Returns("true");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.client.ip.header")).Returns("HTTP_x-forwarded-for");

			var col = new NameValueCollection();
			col.Add("HTTP_x-forwarded-for", "127.0.0.2, 127.0.0.3");
			_httpRequest.SetupGet(c => c.ServerVariables).Returns(col);
			_httpContext.SetupSet(c => c.User = It.Is<GenericPrincipal>(p => p.Identity.Name != "")).Verifiable();

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpContext.Verify();
		}

		#endregion

		[Test]
		public void Аутентифицирует()
		{
			Login();

			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("http://localhost.rapidsoft.ru:80"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");

			_httpContext.SetupSet(c => c.User = It.Is<GenericPrincipal>(p => p.Identity.Name != "")).Verifiable();

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpContext.VerifyAll();
		}

		[Test]
		public void Не_аутентифицирует()
		{
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("http://localhost.rapidsoft.ru:80"));

			var agent = new Mock<Agent>();

			_httpResponse.SetupSet(c => c.StatusCode = 403).Verifiable();
			var module = new Mock<iis7AgentModule>(agent.Object);
			module.CallBase = true;
			module.Setup(m => m.CompleteRequest(_httpContext.Object));
			module.Object.OnAuthentication(_httpContext.Object);

			module.Verify(x => x.CompleteRequest(It.IsAny<HttpContextBase>()), Times.Once());

			_httpContext.Verify();
		}

		[Test]
		public void Мапит_свойства_сессии_в_сервер_вираблы()
		{
			Login();

			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("http://localhost.rapidsoft.ru:80"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.session.attribute.fetch.mode")).Returns("HTTP_HEADER");
			agent.Setup(a => a.GetArray("com.sun.identity.agents.config.session.attribute.mapping"))
				.Returns(new[] {"[MaxIdleTime]=profile-maxidletime", "[ignoreOTP]=profile-ignore-otp"});

			_httpRequest.SetupGet(c => c.ServerVariables).Returns(new NameValueCollection());

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			Assert.IsNotNull(_httpContext.Object.Items["profile-maxidletime"]);
			Assert.IsNotNull(_httpRequest.Object.ServerVariables["profile-maxidletime"]);
			Assert.IsNotNull(_httpContext.Object.Items["profile-ignore-otp"]);
			Assert.IsNotNull(_httpRequest.Object.ServerVariables["profile-ignore-otp"]);
		}

		[Test]
		public void Мапит_свойства_сессии_в_куки()
		{
			Login();

			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("http://localhost.rapidsoft.ru:80"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.session.attribute.fetch.mode")).Returns("HTTP_COOKIE");
			agent.Setup(a => a.GetArray("com.sun.identity.agents.config.session.attribute.mapping"))
				.Returns(new[] {"[MaxIdleTime]=profile-maxidletime", "[ignoreOTP]=profile-ignore-otp"});

			_httpRequest.SetupGet(c => c.ServerVariables).Returns(new NameValueCollection());

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			Assert.IsNotNull(_httpContext.Object.Items["profile-maxidletime"]);
			Assert.IsNotNull(_httpRequest.Object.Cookies["profile-maxidletime"]);
			Assert.IsNotNull(_httpContext.Object.Items["profile-ignore-otp"]);
			Assert.IsNotNull(_httpRequest.Object.Cookies["profile-ignore-otp"]);
		}

		[Test]
		public void Анонимный_вход()
		{		  
			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("http://localhost.rapidsoft.ru:80"));

			var agent = new Mock<Agent>();
			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.anonymous.user.enable")).Returns("true");

			_httpContext.SetupSet(c => c.User = It.Is<GenericPrincipal>(p => p.Identity.Name == "")).Verifiable();

			var module = new iis7AgentModule(agent.Object);
			module.OnAuthentication(_httpContext.Object);

			_httpContext.Verify();
		}
		
//	todo	[Test]
//		public void Ресетит_куки_если_не_aутентифицировался()
//		{
//			_httpRequest.SetupGet(request => request.Url).Returns(new Uri("https://www.mysite.com:444/"));
//
//			var agent = new Mock<Agent>();
//			agent.Setup(a => a.GetOrderedArray("com.sun.identity.agents.config.agent.logout.url"))
//				.Returns(new[] {"https://www.mysite.com:444/"});
//			agent.Setup(a => a.GetFirst("com.sun.identity.agents.config.logout.redirect.url"))
//				.Returns("https://www.mysite.com:444/logoff");
//			agent.Setup(a => a.GetOrderedArray("com.sun.identity.agents.config.logout.cookie.reset")).Returns(new[] {"test"});
//			agent.Setup(a => a.GetSingle("com.sun.identity.agents.config.agenturi.prefix"))
//				.Returns("http://ibank.staging.rapidsoft.ru:80/");
//			var module = new iis7AgentModule(agent.Object);
//			module.OnAuthentication(_httpContext.Object);
//
//			_httpResponse.Verify(response => response.Redirect("https://www.mysite.com:444/logoff"));
//			_httpResponse.Verify(r => r.AddHeader("Set-Cookie", "test"));
//		}

		private void Login(){
			var cookieCollection = new HttpCookieCollection();
 			var sid = Auth.login("/clients", indexType.service, "ldap", new Callback[] { new NameCallback("11111111111"), new PasswordCallback("1111111111") }).sessionId;
			cookieCollection.Set(new HttpCookie("svbid", sid));
			_httpRequest.SetupGet(r => r.Cookies).Returns(cookieCollection);
			_httpRequest.SetupGet(r => r.HttpMethod).Returns("GET");
		}
	}
}

