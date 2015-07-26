using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Web;
using Moq;
using NUnit.Framework;
using ru.org.openam.iis;
using ru.org.openam.sdk.auth;
using ru.org.openam.sdk.auth.callback;

namespace ru.org.openam.sdk.nunit
{
    [TestFixture]
    public class Iis7AgentModuleTests
    {
        private const string RapidsoftUrl = "http://localhost.rapidsoft.ru:80";
        private const string DefaultUrl = "http://example.com/";
        private const string TestUrl = "http://test.com/";
        private Mock<Agent> _agent;
        private Mock<HttpContextBase> _context;
        private iis7AgentModule _module;
        private Mock<HttpRequestBase> _request;
        private Mock<HttpResponseBase> _response;

        [SetUp]
        public void Init()
        {
            _context = new Mock<HttpContextBase>(MockBehavior.Strict);
            _request = new Mock<HttpRequestBase>(MockBehavior.Strict);
            _response = new Mock<HttpResponseBase>(MockBehavior.Strict);
            _response = new Mock<HttpResponseBase>(MockBehavior.Strict);
			//_applicationInstance = new Mock<HttpApplication>(MockBehavior.Strict);

            _agent = new Mock<Agent>(MockBehavior.Strict);

            _module = new iis7AgentModule(_agent.Object);

            //_context.Setup(x => x.ApplicationInstance).Returns(_applicationInstance.Object);
            _context.Setup(x => x.Request).Returns(_request.Object);
            _context.Setup(x => x.Response).Returns(_response.Object);
        }

		[Test]
		[ExpectedException(typeof (ArgumentException))]
		[Description("Бросаем ArgumentException, если не определен HttpContext")]
		public void OnAuthentication_WithoutHttpContextTest()
		{
			_module.OnAuthentication(null);
		}

        [Test]
        [ExpectedException(typeof (ArgumentException))]
        [Description("Бросаем ArgumentException, если не определен HttpRequest")]
        public void OnAuthentication_WithoutHttpRequestTest()
        {
            _context.Setup(x => x.Request).Returns((HttpRequestBase) null);

            _module.OnAuthentication(_context.Object);

            _context.Verify(x => x.Request, Times.Once());
        }

        [Test]
        [ExpectedException(typeof (ArgumentException))]
        [Description("Бросаем ArgumentException, если не определен Url в HttpRequest")]
        public void OnAuthentication_WithoutHttpRequestUrlTest()
        {
            _request.Setup(x => x.IsLocal).Returns(true);
            _request.Setup(x => x.Url).Returns((Uri) null);

            _module.OnAuthentication(_context.Object);

            _context.Verify(x => x.Request, Times.Once());
            _request.Verify(x => x.Url, Times.Once());
        }

        [Test]
        [Description("Редиректим, если урлы не совпадают без переопределения протокола и порта")]
        public void OnAuthentication_CheckUrlTest()
        {
            CheckUrlTest(DefaultUrl + "path/?id=1", "http://redirect.url:80/path/?id=1", "true", "redirect.url", null, "false", "false");
        }

        [Test]
        [Description("Редиректим, если урлы не совпадают без переопределения протокола и порта")]
        public void OnAuthentication_CheckUrlWithRewriteProtocolTrueTest()
        {
            CheckUrlTest(DefaultUrl + "path/?id=1", "http://redirect.url:80/path/?id=1", "true", "redirect.url", null, "true", "false");
        }

        [Test]
        [Description("Редиректим, если урлы не совпадают без переопределения протокола и порта")]
        public void OnAuthentication_CheckUrlWithRewritePortTrueTest()
        {
            CheckUrlTest(DefaultUrl + "path/?id=1", "http://redirect.url:80/path/?id=1", "true", "redirect.url", null, "false", "true");
        }

        [Test]
        [Description("Редиректим, если урлы не совпадают без переопределения протокола и порта")]
        public void OnAuthentication_CheckUrlWithRewriteProtocolTrueAndRewritePortTrueTest()
        {
            CheckUrlTest(DefaultUrl + "path/?id=1", "http://redirect.url:80/path/?id=1", "true", "redirect.url", null, "true", "true");
        }

        [Test]
        [Description("Редиректим, если урлы не совпадают без переопределения протокола и порта, но определенным com.sun.identity.agents.config.agenturi.prefix")]
        public void OnAuthentication_CheckUrlWithSetRedirectUriTest()
        {
            CheckUrlTest(DefaultUrl + "path/?id=1", "http://redirect.url:80/path/?id=1", "true", "redirect.url", "ftp://a.b:21", "false", "false");
        }

        [Test]
        [Description("Редиректим, если урлы не совпадают c переопределением протокола и порта")]
        public void OnAuthentication_CheckUrlWithRewritingProtocolTest()
        {
            CheckUrlTest(DefaultUrl + "path/?id=1", "ftp://redirect.url:22/path/?id=1", "true", "redirect.url", "ftp://a.b:22", "true", "false");
        }

        [Test]
        [Description("Редиректим, если урлы не совпадают c переопределением протокола и порта")]
        public void OnAuthentication_CheckUrlWithRewritingPortTest()
        {
            CheckUrlTest(DefaultUrl + "path/?id=1", "ftp://redirect.url:23/path/?id=1", "true", "redirect.url", "ftp://a.b:23", "false", "true");
        }

        [Test]
        [Description("Редиректим, если урлы не совпадают c переопределением протокола и порта")]
        public void OnAuthentication_CheckUrlWithRewritingProtocolAndPortTest()
        {
            CheckUrlTest(DefaultUrl + "path/?id=1", "ftp://redirect.url:24/path/?id=1", "true", "redirect.url", "ftp://a.b:24", "true", "true");
        }

        [Test]
        [Description("Логаутим пользователя по com.sun.identity.agents.config.logout.redirect.url, если урл совпадает с одним из com.sun.identity.agents.config.agent.logout.url")]
        public void OnAuthentication_IsLogoffTest()
        {
            var settings = new Dictionary<string, object>
            {
                { "com.sun.identity.agents.config.override.host", "false" },
                { "com.sun.identity.agents.config.fqdn.default", null },
                { "com.sun.identity.agents.config.agent.logout.url", new[] { TestUrl, DefaultUrl } },
                { "com.sun.identity.agents.config.logout.cookie.reset", new[] { "cookie1", "cookie2" } },
                { "com.sun.identity.agents.config.logout.redirect.url", "redirect.url" }
            };
            
            SetupAgent(settings);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _response.Setup(x => x.AddHeader("Set-Cookie", "cookie1"));
            _response.Setup(x => x.AddHeader("Set-Cookie", "cookie2"));

            var module = new Mock<iis7AgentModule>(_agent.Object);
			module.Setup(m => m.CompleteRequest(_context.Object));
			module.CallBase = true;
			module.Setup(x => x.Redirect((string)settings["com.sun.identity.agents.config.logout.redirect.url"], It.IsAny<HttpContextBase>()));
            module.Object.OnAuthentication(_context.Object);

            VerifyAgent(settings);
            _request.Verify(x => x.Url, Times.Exactly(2));
            _response.Verify(x => x.AddHeader("Set-Cookie", "cookie1"), Times.Once());
            _response.Verify(x => x.AddHeader("Set-Cookie", "cookie2"), Times.Once());
			module.Verify(x => x.Redirect((string)settings["com.sun.identity.agents.config.logout.redirect.url"], It.IsAny<HttpContextBase>()), Times.Once());
        }

        [Test]
        [Description("Логаутим пользователя по com.sun.identity.agents.config.login.url, если урл совпадает с одним из com.sun.identity.agents.config.agent.logout.url")]
        public void OnAuthentication_IsLogoffWithoutLogoutRedirectUrlTest()
        {
            var settings = new Dictionary<string, object>
            {
                { "com.sun.identity.agents.config.override.host", "false" },
                { "com.sun.identity.agents.config.fqdn.default", null },
                { "com.sun.identity.agents.config.agent.logout.url", new[] { TestUrl, DefaultUrl } },
                { "com.sun.identity.agents.config.logout.cookie.reset", new string[0] },
                { "com.sun.identity.agents.config.logout.redirect.url", null },
                { "com.sun.identity.agents.config.login.url", "login.url" }
            };

            SetupAgent(settings);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));

            var module = new Mock<iis7AgentModule>(_agent.Object);
			module.Setup(m => m.CompleteRequest(_context.Object));
			module.CallBase = true;
			module.Setup(x => x.Redirect((string)settings["com.sun.identity.agents.config.login.url"], It.IsAny<HttpContextBase>()));
            module.Object.OnAuthentication(_context.Object);

            VerifyAgent(settings);
            _request.Verify(x => x.Url, Times.Exactly(2));
			module.Verify(x => x.Redirect((string)settings["com.sun.identity.agents.config.login.url"], It.IsAny<HttpContextBase>()), Times.Once());
        }

        [Test]
        [ExpectedException(typeof (InvalidOperationException))]
        [Description("Бросаем InvalidOperationException, если не определены com.sun.identity.agents.config.logout.redirect.url и com.sun.identity.agents.config.login.url")]
        public void OnAuthentication_IsLogoffWithoutLogoutRedirectUrlAndLoginUrlTest()
        {
            var settings = new Dictionary<string, object>
            {
                { "com.sun.identity.agents.config.override.host", "false" },
                { "com.sun.identity.agents.config.fqdn.default", null },
                { "com.sun.identity.agents.config.agent.logout.url", new[] { TestUrl, DefaultUrl } },
                { "com.sun.identity.agents.config.logout.cookie.reset", new string[0] },
                { "com.sun.identity.agents.config.logout.redirect.url", null },
                { "com.sun.identity.agents.config.login.url", null }
            };

            SetupAgent(settings);
            _request.Setup(x => x.IsLocal).Returns(true);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));

            _module.OnAuthentication(_context.Object);
        }

        [Test]
        [Description("Пользователь зашел по нетребующему авторизации урлу без AuthCookie")]
        public void OnAuthentication_IsFreeUrlWithoutAuthCookieTest()
        {
            var settings = new Dictionary<string, object>
            {
                { "com.sun.identity.agents.config.override.host", "false" },
                { "com.sun.identity.agents.config.fqdn.default", null },
                { "com.sun.identity.agents.config.agent.logout.url", new string[0] },
                { "com.sun.identity.agents.config.notenforced.url", new[] { TestUrl, DefaultUrl + "*" } },
                { "com.sun.identity.agents.config.notenforced.url.attributes.enable", "true" },
                { "com.sun.identity.agents.config.audit.accesstype", "false" },
                { "com.sun.identity.agents.config.local.log.rotate", "false" },
                { "com.sun.identity.agents.config.debug.level", "false" },
            };

            SetupAgent(settings); 
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection());
            _context.SetupSet(x => x.User = null);

			var module = new Mock<iis7AgentModule>(_agent.Object);
			module.Setup(m => m.CompleteRequest(_context.Object));
			module.CallBase = true;
            module.Object.OnAuthentication(_context.Object);

            VerifyAgent(settings);
            _request.Verify(x => x.Url, Times.Exactly(3));
            _request.Verify(x => x.Cookies, Times.Once());
            _context.VerifySet(x => x.User = null, Times.Once());
        }

        [Test]
        [Description("Пользователь зашел по нетребующему авторизации урлу c AuthCookie")]
        public void OnAuthentication_IsFreeUrlWithAuthCookieTest()
        {
            var settings = new Dictionary<string, object>
            {
				{ "com.sun.identity.agents.config.receive.timeout", "0" },
				{ "com.sun.identity.agents.config.connect.timeout", "0" },
                { "com.sun.identity.agents.config.override.host", "false" },
                { "com.sun.identity.agents.config.fqdn.default", null },
                { "com.sun.identity.agents.config.agent.logout.url", new string[0] },
                { "com.sun.identity.agents.config.notenforced.url", new[] { TestUrl, DefaultUrl } },
                { "com.sun.identity.agents.config.notenforced.url.attributes.enable", "true" },
                { "com.sun.identity.agents.config.userid.param", "UserId" },
                { "com.sun.identity.agents.config.audit.accesstype", "false" },
                { "com.sun.identity.agents.config.local.log.rotate", "false" },
                { "com.sun.identity.agents.config.debug.level", "false" },
                { "com.sun.identity.agents.config.policy.cache.polling.interval", "false" }
            };

            SetupAgent(settings); 
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection { new HttpCookie("svbid", GetAuthCookie()) });
            _context.SetupSet(x => x.User = It.IsAny<GenericPrincipal>());

            var module = new Mock<iis7AgentModule>(_agent.Object);
			module.Setup(m => m.CompleteRequest(_context.Object));
			module.CallBase = true;
            module.Object.OnAuthentication(_context.Object);

            VerifyAgent(settings);
            _request.Verify(x => x.Url, Times.Exactly(3));
            _request.Verify(x => x.Cookies, Times.Once());
            _context.VerifySet(x => x.User = It.Is<GenericPrincipal>(u => u.Identity.Name == "11111111111" && u.Identity.IsAuthenticated), Times.Once());
        }

        [Test]
        [Description("Пользователь зашел по нетребующему авторизации урлу и com.sun.identity.agents.config.notenforced.url.attributes.enable выключена")]
        public void OnAuthentication_IsFreeUrlAndAnonymousTest()
        {
            var settings = new Dictionary<string, object>
            {
                { "com.sun.identity.agents.config.override.host", "false" },
                { "com.sun.identity.agents.config.fqdn.default", null },
                { "com.sun.identity.agents.config.agent.logout.url", new string[0] },
                { "com.sun.identity.agents.config.notenforced.url", new[] { TestUrl, DefaultUrl } },
                { "com.sun.identity.agents.config.notenforced.url.attributes.enable", "false" }
            }; 
            
            SetupAgent(settings);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _context.SetupSet(x => x.User = It.IsAny<GenericPrincipal>());

            _module.OnAuthentication(_context.Object);

            VerifyAgent(settings);
            _request.Verify(x => x.Url, Times.Exactly(3));
            _context.VerifySet(x => x.User = It.Is<GenericPrincipal>(u => u.Identity.Name == "" && !u.Identity.IsAuthenticated), Times.Once());
        }

        [Test]
        [Description("Попытка авторизации пользователя без куки при включенной анонимной аутенфикации")]
        public void OnAuthentication_WithoutCookieAndWithAnonymousEnabledTest()
        {
            var settings = new Dictionary<string, object>
            {
                { "com.sun.identity.agents.config.override.host", "false" },
                { "com.sun.identity.agents.config.fqdn.default", null },
                { "com.sun.identity.agents.config.agent.logout.url", new string[0] },
                { "com.sun.identity.agents.config.notenforced.url", new string[0] },
                { "com.sun.identity.agents.config.anonymous.user.enable", "true" },
				{ "com.sun.identity.agents.config.audit.accesstype", "false" },
				{ "com.sun.identity.agents.config.local.log.rotate", "false" },
				{ "com.sun.identity.agents.config.debug.level", "false" }
            };

            SetupAgent(settings);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection());
            _context.SetupSet(x => x.User = It.IsAny<GenericPrincipal>());

            _module.OnAuthentication(_context.Object);

            VerifyAgent(settings);
            _request.Verify(x => x.Url, Times.Exactly(3));
            _request.Verify(x => x.Cookies, Times.Once());
            _context.VerifySet(x => x.User = It.Is<GenericPrincipal>(u => u.Identity.Name == "" && !u.Identity.IsAuthenticated), Times.Once());
        }

        [Test]
        [Description(@"Попытка авторизации пользователя без куки при выключенной анонимной аутенфикации 
                        c com.sun.identity.agents.config.login.url и com.sun.identity.agents.config.redirect.param")]
        public void OnAuthentication_WithoutCookieAndWithAnonymousDisabledAndLoginUrlAndRedirectParamTest()
        {
            var settings = new Dictionary<string, object>
            {
                { "com.sun.identity.agents.config.override.host", "false" },
                { "com.sun.identity.agents.config.fqdn.default", null },
                { "com.sun.identity.agents.config.agent.logout.url", new string[0] },
                { "com.sun.identity.agents.config.notenforced.url", new string[0] },
                { "com.sun.identity.agents.config.anonymous.user.enable", "false" },
                { "com.sun.identity.agents.config.cookie.reset", new[] { "cookie2", "cookie1" } },
                { "com.sun.identity.agents.config.login.url", "login.url" },
                { "com.sun.identity.agents.config.redirect.param", "redirect.param" },
				{ "com.sun.identity.agents.config.audit.accesstype", "false" },
				{ "com.sun.identity.agents.config.local.log.rotate", "false" },
				{ "com.sun.identity.agents.config.debug.level", "false" }
            };
            var redirectUrl = string.Format(
                "{0}?{1}={2}", settings["com.sun.identity.agents.config.login.url"], settings["com.sun.identity.agents.config.redirect.param"], DefaultUrl);
                    
            SetupAgent(settings);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection());
            _request.Setup(x => x.IsLocal).Returns(true);
            _response.Setup(x => x.AddHeader("Set-Cookie", "cookie1"));
            _response.Setup(x => x.AddHeader("Set-Cookie", "cookie2"));
            
             var module = new Mock<iis7AgentModule>(_agent.Object);
			module.Setup(m => m.Redirect(redirectUrl, _context.Object));
			module.CallBase = true;
            module.Object.OnAuthentication(_context.Object);

            VerifyAgent(settings);
            _request.Verify(x => x.Url, Times.Exactly(3));
            _request.Verify(x => x.Cookies, Times.Once());
            _response.Verify(x => x.AddHeader("Set-Cookie", "cookie1"), Times.Once());
            _response.Verify(x => x.AddHeader("Set-Cookie", "cookie2"), Times.Once());
            module.Verify(m => m.Redirect(redirectUrl, _context.Object), Times.Once());
        }

        [Test]
        [Description(@"Попытка авторизации пользователя без куки при выключенной анонимной аутенфикации 
                        c com.sun.identity.agents.config.login.url и без com.sun.identity.agents.config.redirect.param")]
        public void OnAuthentication_WithoutCookieAndWithAnonymousDisabledAndLoginUrlAndWithoutRedirectParamTest()
        {
            var settings = new Dictionary<string, object>
            {
                { "com.sun.identity.agents.config.override.host", "false" },
                { "com.sun.identity.agents.config.fqdn.default", null },
                { "com.sun.identity.agents.config.agent.logout.url", new string[0] },
                { "com.sun.identity.agents.config.notenforced.url", new string[0] },
                { "com.sun.identity.agents.config.anonymous.user.enable", "false" },
                { "com.sun.identity.agents.config.cookie.reset", new string[0] },
                { "com.sun.identity.agents.config.login.url", "login.url" },
                { "com.sun.identity.agents.config.redirect.param", null },
				{ "com.sun.identity.agents.config.audit.accesstype", "false" },
				{ "com.sun.identity.agents.config.local.log.rotate", "false" },
				{ "com.sun.identity.agents.config.debug.level", "false" }
            };
            
            SetupAgent(settings);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.IsLocal).Returns(true);
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection());

             var module = new Mock<iis7AgentModule>(_agent.Object);
			module.Setup(m => m.Redirect((string)settings["com.sun.identity.agents.config.login.url"], _context.Object));
			module.CallBase = true;
            module.Object.OnAuthentication(_context.Object);

            VerifyAgent(settings);
            _request.Verify(x => x.Url, Times.Exactly(3));
            _request.Verify(x => x.Cookies, Times.Once());
			module.Verify(m => m.Redirect((string)settings["com.sun.identity.agents.config.login.url"], _context.Object), Times.Once());
        }

        [Test]
        [Description(@"Попытка авторизации пользователя без куки при выключенной анонимной аутенфикации без com.sun.identity.agents.config.login.url")]
        public void OnAuthentication_WithoutCookieAndWithAnonymousDisabledAndWithoutLoginUrlTest()
        {
            var settings = new Dictionary<string, object>
            {
                { "com.sun.identity.agents.config.override.host", "false" },
                { "com.sun.identity.agents.config.fqdn.default", null },
                { "com.sun.identity.agents.config.agent.logout.url", new string[0] },
                { "com.sun.identity.agents.config.notenforced.url", new string[0] },
                { "com.sun.identity.agents.config.anonymous.user.enable", "false" },
                { "com.sun.identity.agents.config.cookie.reset", new string[0] },
                { "com.sun.identity.agents.config.login.url", null },
				{ "com.sun.identity.agents.config.audit.accesstype", "false" },
				{ "com.sun.identity.agents.config.local.log.rotate", "false" },
				{ "com.sun.identity.agents.config.debug.level", "false" }
            }; 
            
            SetupAgent(settings);
			_request.Setup(x => x.IsLocal).Returns(true);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection());
            _response.SetupSet(x => x.StatusCode = 401);

			var module = new Mock<iis7AgentModule>(_agent.Object);
			module.Setup(m => m.CompleteRequest(_context.Object));
			module.CallBase = true;
            module.Object.OnAuthentication(_context.Object);

            VerifyAgent(settings, _agent);
            _request.Verify(x => x.Url, Times.Exactly(3));
            _request.Verify(x => x.Cookies, Times.Once());
            _response.VerifySet(x => x.StatusCode = 401, Times.Once());
            module.Verify(x => x.CompleteRequest(It.IsAny<HttpContextBase>()), Times.Once());
        }

        [Test]
        [Description(@"Попытка авторизации пользователя c кукой без com.sun.identity.agents.config.userid.param")]
        public void OnAuthentication_WithCookieAndUserIdParamTest()
        {
            var settings = new Dictionary<string, object>
            {
				{ "com.sun.identity.agents.config.receive.timeout", "0" },
				{ "com.sun.identity.agents.config.connect.timeout", "0" },
                { "com.sun.identity.agents.config.override.host", "false" },
                { "com.sun.identity.agents.config.fqdn.default", null },
                { "com.sun.identity.agents.config.agent.logout.url", new string[0] },
                { "com.sun.identity.agents.config.notenforced.url", new string[0] },
                { "com.sun.identity.agents.config.userid.param", null },
                { "com.sun.identity.agents.config.anonymous.user.enable", "true" },
                { "com.sun.identity.agents.config.client.ip.validation.enable", "false" },
				{ "com.sun.identity.agents.config.audit.accesstype", "false" },
				{ "com.sun.identity.agents.config.local.log.rotate", "false" },
				{ "com.sun.identity.agents.config.debug.level", "false" },
				{ "com.sun.identity.agents.config.policy.cache.polling.interval", "false" }
            };
            
            SetupAgent(settings);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection { new HttpCookie("svbid", GetAuthCookie()) });
            _context.SetupSet(x => x.User = It.IsAny<GenericPrincipal>());

            _module.OnAuthentication(_context.Object);

            VerifyAgent(settings);
            _request.Verify(x => x.Url, Times.Exactly(3));
            _request.Verify(x => x.Cookies, Times.Once());
            _context.VerifySet(x => x.User = It.Is<GenericPrincipal>(u => u.Identity.Name == "" && !u.Identity.IsAuthenticated), Times.Once());
        }

        [Test]
        [Description(@"Попытка авторизации пользователя c кукой, SSO only, без валидации IP и заполнением HTTP_HEADER'")]
        public void OnAuthentication_WithCookieAndSsoOnlyAndFillHeadersWithoutIpValidationTest()
        {
            var items = new Dictionary<string, object>();
            var serverVariables = new NameValueCollection();
            var authCookie = GetAuthCookie();
            var settings = new Dictionary<string, object>
            {
				{ "com.sun.identity.agents.config.receive.timeout", "0" },
				{ "com.sun.identity.agents.config.connect.timeout", "0" },
                { "com.sun.identity.agents.config.override.host", "false" },
                { "com.sun.identity.agents.config.fqdn.default", null },
                { "com.sun.identity.agents.config.agent.logout.url", new string[0] },
                { "com.sun.identity.agents.config.notenforced.url", new string[0] },
                { "com.sun.identity.agents.config.userid.param", "UserId" },
                { "com.sun.identity.agents.config.sso.only", "true" },
                { "com.sun.identity.agents.config.client.ip.validation.enable", "false" },
                {"com.sun.identity.agents.config.session.attribute.mapping",  new[] { "[MaxIdleTime]=profile-maxidletime", "[ignoreOTP]=profile-ignore-otp", "test" }},
                {"com.sun.identity.agents.config.session.attribute.fetch.mode", "HTTP_HEADER"},
				{ "com.sun.identity.agents.config.audit.accesstype", "false" },
				{ "com.sun.identity.agents.config.local.log.rotate", "false" },
				{ "com.sun.identity.agents.config.debug.level", "false" },
				{ "com.sun.identity.agents.config.policy.cache.polling.interval", "false" }
            };

            SetupAgent(settings);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection { new HttpCookie("svbid", authCookie) });
            _request.Setup(x => x.ServerVariables).Returns(serverVariables);
            _request.Setup(x => x.IsLocal).Returns(true);
            _context.Setup(x => x.Items).Returns(items);
            _context.SetupSet(x => x.User = It.IsAny<GenericPrincipal>());

            _module.OnAuthentication(_context.Object);
            Assert.AreEqual(3, items.Count);
            Assert.AreEqual(4, serverVariables.Count);
            Assert.AreEqual(authCookie, items["am_auth_cookie"]);
            Assert.IsNotNull(items["profile-maxidletime"]);
            Assert.IsNotNull(serverVariables["profile-maxidletime"]);
            Assert.IsNotNull(items["profile-ignore-otp"]);
            Assert.IsNotNull(serverVariables["profile-ignore-otp"]);

            VerifyAgent(settings);
            _request.Verify(x => x.Url, Times.Exactly(4));
            _request.Verify(x => x.Cookies, Times.Exactly(2));
            _request.Verify(x => x.ServerVariables, Times.Exactly(4));
            _context.Verify(x => x.Items, Times.Exactly(3));
            _context.VerifySet(x => x.User = It.Is<GenericPrincipal>(u => u.Identity.Name == "11111111111" && u.Identity.IsAuthenticated), Times.Once());
        }

        [Test]
        [Description(@"Попытка авторизации пользователя c кукой, SSO only, с валидацией IP и заполнением HTTP_COOKIE'")]
        public void OnAuthentication_WithCookieAndSsoOnlyAndFillCookiesAndIpValidationTest()
        {
            var items = new Dictionary<string, object>();
            var authCookie = GetAuthCookie();
            var cookies = new HttpCookieCollection { new HttpCookie("svbid", authCookie) };
            var settings = new Dictionary<string, object>
            {
				{ "com.sun.identity.agents.config.receive.timeout", "0" },
				{ "com.sun.identity.agents.config.connect.timeout", "0" },
                { "com.sun.identity.agents.config.override.host", "false" },
                { "com.sun.identity.agents.config.fqdn.default", null },
                { "com.sun.identity.agents.config.agent.logout.url", new string[0] },
                { "com.sun.identity.agents.config.notenforced.url", new string[0] },
                { "com.sun.identity.agents.config.userid.param", "UserId" },
                { "com.sun.identity.agents.config.sso.only", "true" },
                { "com.sun.identity.agents.config.client.ip.validation.enable", "true" },
                { "com.sun.identity.agents.config.session.attribute.mapping", new[] { "[ignoreOTP]=profile-ignore-otp", "[test]=test" } },
                { "com.sun.identity.agents.config.session.attribute.fetch.mode", "HTTP_COOKIE" },
                { "com.sun.identity.agents.config.client.ip.header", null },
				{ "com.sun.identity.agents.config.audit.accesstype", "false" },
				{ "com.sun.identity.agents.config.local.log.rotate", "false" },
				{ "com.sun.identity.agents.config.debug.level", "false" },
				{ "com.sun.identity.agents.config.policy.cache.polling.interval", "false" }
            };

            SetupAgent(settings);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(cookies);
            _request.Setup(x => x.UserHostAddress).Returns("127.0.0.2");
            _request.Setup(x => x.IsLocal).Returns(true);
            _context.Setup(x => x.Items).Returns(items);
            _context.SetupSet(x => x.User = It.IsAny<GenericPrincipal>());

            _module.OnAuthentication(_context.Object);
            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(2, cookies.Count);
            Assert.AreEqual(authCookie, items["am_auth_cookie"]);
            Assert.IsNotNull(items["profile-ignore-otp"]);

            VerifyAgent(settings);
            _request.Verify(x => x.Url, Times.Exactly(4));
            _request.Verify(x => x.Cookies, Times.Exactly(3));
            _request.Verify(x => x.UserHostAddress, Times.Once());
            _context.Verify(x => x.Items, Times.Exactly(2));
            _context.VerifySet(x => x.User = It.Is<GenericPrincipal>(u => u.Identity.Name == "11111111111" && u.Identity.IsAuthenticated), Times.Once());
        }

        [Test]
        [Description(@"Попытка авторизации пользователя c кукой, SSO only, с валидацией IP и без указания com.sun.identity.agents.config.session.attribute.fetch.mode")]
        public void OnAuthentication_WithCookieAndSsoOnlyAndFillNothingAndIpValidationTest()
        {
            var items = new Dictionary<string, object>();
            var authCookie = GetAuthCookie();
            var settings = new Dictionary<string, object>
            {
				{ "com.sun.identity.agents.config.receive.timeout", "0" },
				{ "com.sun.identity.agents.config.connect.timeout", "0" },
                { "com.sun.identity.agents.config.override.host", "false" },
                { "com.sun.identity.agents.config.fqdn.default", null },
                { "com.sun.identity.agents.config.agent.logout.url", new string[0] },
                { "com.sun.identity.agents.config.notenforced.url", new string[0] },
                { "com.sun.identity.agents.config.userid.param", "UserId" },
                { "com.sun.identity.agents.config.sso.only", "true" },
                { "com.sun.identity.agents.config.client.ip.validation.enable", "true" },
                { "com.sun.identity.agents.config.session.attribute.mapping", new[] { "[ignoreOTP]=profile-ignore-otp" } },
                { "com.sun.identity.agents.config.session.attribute.fetch.mode", null },
                { "com.sun.identity.agents.config.client.ip.header", "ip-header" },
				{ "com.sun.identity.agents.config.audit.accesstype", "false" },
				{ "com.sun.identity.agents.config.local.log.rotate", "false" },
				{ "com.sun.identity.agents.config.debug.level", "false" },
				{ "com.sun.identity.agents.config.policy.cache.polling.interval", "false" }
            };

            SetupAgent(settings);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection { new HttpCookie("svbid", authCookie) });
            _request.Setup(x => x.UserHostAddress).Returns("127.0.0.3");
            _request.Setup(x => x.Headers).Returns(new NameValueCollection { {"ip-header", "127.0.0.2,127.0.0.1" }});
            _context.Setup(x => x.Items).Returns(items);
            _context.SetupSet(x => x.User = It.IsAny<GenericPrincipal>());

            _module.OnAuthentication(_context.Object);
            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(authCookie, items["am_auth_cookie"]);
            Assert.IsNotNull(items["profile-ignore-otp"]);

            VerifyAgent(settings);
            _request.Verify(x => x.Url, Times.Exactly(4));
            _request.Verify(x => x.Cookies, Times.Exactly(2));
            _request.Verify(x => x.UserHostAddress, Times.Once());
            _request.Verify(x => x.Headers, Times.Once());
            _context.Verify(x => x.Items, Times.Exactly(2));
            _context.VerifySet(x => x.User = It.Is<GenericPrincipal>(u => u.Identity.Name == "11111111111" && u.Identity.IsAuthenticated), Times.Once());
        }

        [Test]
        [Description(@"Попытка авторизации пользователя c кукой и анонимной авторизацией, без валидации IP, без SSO only и без Policy")]
        public void OnAuthentication_WithCookieAndWihoutSsoOnlyAndPolicyTest()
        {
            var authCookie = GetAuthCookie();
            var settings = new Dictionary<string, object>
            {
				{ "com.sun.identity.agents.config.receive.timeout", "0" },
				{ "com.sun.identity.agents.config.connect.timeout", "0" },
                { "com.sun.identity.agents.config.policy.cache.polling.interval", null },
                { "com.sun.identity.agents.config.override.host", "false" },
                { "com.sun.identity.agents.config.fqdn.default", null },
                { "com.sun.identity.agents.config.agent.logout.url", new string[0] },
                { "com.sun.identity.agents.config.notenforced.url", new string[0] },
                { "com.sun.identity.agents.config.userid.param", "UserId" },
                { "com.sun.identity.agents.config.sso.only", "false" },
                { "com.sun.identity.agents.config.client.ip.validation.enable", "false" },
                { "com.sun.identity.agents.config.profile.attribute.mapping", new[] { "[test]=test" } },
                { "com.sun.identity.agents.config.fetch.from.root.resource", "true" },
                { "com.sun.identity.agents.config.anonymous.user.enable", "true" },
				{ "com.sun.identity.agents.config.audit.accesstype", "false" },
				{ "com.sun.identity.agents.config.local.log.rotate", "false" },
				{ "com.sun.identity.agents.config.debug.level", "false" }
            };

            SetupAgent(settings);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection { new HttpCookie("svbid", authCookie) });
            _request.Setup(x => x.HttpMethod).Returns("POST");
            _request.Setup(x => x.IsLocal).Returns(true);
            _context.SetupSet(x => x.User = It.IsAny<GenericPrincipal>());

            _module.OnAuthentication(_context.Object);
            
            //VerifyAgent(settings);
            _request.Verify(x => x.Url, Times.Exactly(4));
            _request.Verify(x => x.Cookies, Times.Once());
            _request.Verify(x => x.HttpMethod, Times.Once());
            _context.VerifySet(x => x.User = It.Is<GenericPrincipal>(u => u.Identity.Name == "" && !u.Identity.IsAuthenticated), Times.Once());
        }

        [Test]
        [Description(@"Попытка авторизации пользователя c кукой и анонимной авторизацией, без валидации IP, без SSO only и c Policy и заполнением HEADER из Policy и COOKIE из сессии")]
        public void OnAuthentication_WithCookieAndWihoutSsoOnlyAndWithPolicyTest()
        {
            var items = new Dictionary<string, object>();
            var serverVariables = new NameValueCollection();
            var authCookie = GetAuthCookie();
            var cookies = new HttpCookieCollection { new HttpCookie("svbid", authCookie) };
            var settings = new Dictionary<string, object>
            {
				{ "com.sun.identity.agents.config.receive.timeout", "0" },
				{ "com.sun.identity.agents.config.connect.timeout", "0" },
                { "com.sun.identity.agents.config.policy.cache.polling.interval", null },
                { "com.sun.identity.agents.config.override.host", "false" },
                { "com.sun.identity.agents.config.fqdn.default", null },
                { "com.sun.identity.agents.config.agent.logout.url", new string[0] },
                { "com.sun.identity.agents.config.notenforced.url", new string[0] },
                { "com.sun.identity.agents.config.userid.param", "UserId" },
                { "com.sun.identity.agents.config.sso.only", "false" },
                { "com.sun.identity.agents.config.client.ip.validation.enable", "false" },
                { "com.sun.identity.agents.config.profile.attribute.mapping", new[] { "[employeeNumber]=profile-clientid", "[sn]=profile-type" } },
                { "com.sun.identity.agents.config.profile.attribute.fetch.mode", "HTTP_HEADER" },
                { "com.sun.identity.agents.config.session.attribute.mapping", new[] { "[ignoreOTP]=profile-ignore-otp" } },
                { "com.sun.identity.agents.config.session.attribute.fetch.mode", "HTTP_COOKIE" },
                { "com.sun.identity.agents.config.fetch.from.root.resource", "false" },
                { "com.sun.identity.agents.config.ignore.path.info", "false" },
				{ "com.sun.identity.agents.config.audit.accesstype", "false" },
				{ "com.sun.identity.agents.config.local.log.rotate", "false" },
				{ "com.sun.identity.agents.config.debug.level", "false" }
            };

            SetupAgent(settings);
            _request.Setup(x => x.Url).Returns(new Uri(RapidsoftUrl));
            _request.Setup(x => x.Cookies).Returns(cookies);
            _request.Setup(x => x.HttpMethod).Returns("POST");
            _request.Setup(x => x.ServerVariables).Returns(serverVariables);
            _context.Setup(x => x.Items).Returns(items);
            _context.SetupSet(x => x.User = It.IsAny<GenericPrincipal>());

            _module.OnAuthentication(_context.Object);
            Assert.AreEqual(4, items.Count);
            Assert.AreEqual(4, serverVariables.Count);
            Assert.AreEqual(2, cookies.Count);
            Assert.AreEqual(authCookie, items["am_auth_cookie"]);
            Assert.IsNotNull(items["profile-ignore-otp"]);
            Assert.IsNotNull(cookies["profile-ignore-otp"]);
            Assert.IsNotNull(items["profile-clientid"]);
            Assert.IsNotNull(serverVariables["profile-clientid"]);
            Assert.IsNotNull(items["profile-type"]);
            Assert.IsNotNull(serverVariables["profile-type"]);
            
            //VerifyAgent(settings);
            _request.Verify(x => x.Url, Times.Exactly(5));
            _request.Verify(x => x.Cookies, Times.Exactly(3));
            _request.Verify(x => x.HttpMethod, Times.Once());
            _request.Verify(x => x.ServerVariables, Times.Exactly(4));
            _context.Verify(x => x.Items, Times.Exactly(4));
            _context.VerifySet(x => x.User = It.Is<GenericPrincipal>(u => u.Identity.Name == "11111111111" && u.Identity.IsAuthenticated), Times.Once());
        }

        private void CheckUrlTest(
            string url,
            string redirectUrl,
            string enableRedirect,
            string redirectHost,
            string redirectPrefix = null,
            string overrideProtocol = null,
            string overridePort = null)
        {
            var settings = new Dictionary<string, object>
            {
                { "com.sun.identity.agents.config.override.host", enableRedirect },
                { "com.sun.identity.agents.config.fqdn.default", redirectHost },
                { "com.sun.identity.agents.config.agenturi.prefix", redirectPrefix },
                { "com.sun.identity.agents.config.override.protocol", overrideProtocol },
                { "com.sun.identity.agents.config.override.port", overridePort }
            };

            SetupAgent(settings);
            _request.Setup(x => x.Url).Returns(new Uri(url));
            //_response.Setup(x => x.Redirect(redirectUrl));

			var module = new Mock<iis7AgentModule>(_agent.Object);
			module.CallBase = true;
			module.Setup(a => a.Redirect(redirectUrl, It.IsAny<HttpContextBase>()));

            module.Object.OnAuthentication(_context.Object);

            VerifyAgent(settings);
            _request.Verify(x => x.Url, Times.Exactly(2));
            module.Verify(x => x.Redirect(redirectUrl, It.IsAny<HttpContextBase>()), Times.Once());
        }

        private void SetupAgent(IDictionary<string, object> settings)
        {
            foreach (var pair in settings)
            {
                var setting = pair;
                switch (setting.Key)
                {
                    case "com.sun.identity.agents.config.logout.redirect.url":
                    case "com.sun.identity.agents.config.login.url":
                        _agent.Setup(x => x.GetFirst(setting.Key)).Returns((string)setting.Value);
                        break;
                    case "com.sun.identity.agents.config.session.attribute.mapping":
                    case "com.sun.identity.agents.config.profile.attribute.mapping":
                        _agent.Setup(x => x.GetArray(setting.Key)).Returns((string[])setting.Value);
                        break;
                    case "com.sun.identity.agents.config.agent.logout.url":
                    case "com.sun.identity.agents.config.logout.cookie.reset":
                    case "com.sun.identity.agents.config.notenforced.url":
                    case "com.sun.identity.agents.config.cookie.reset":
                        _agent.Setup(x => x.GetOrderedArray(setting.Key)).Returns((string[])setting.Value);
                        break;
                    default:
                         _agent.Setup(x => x.GetSingle(setting.Key)).Returns((string)setting.Value);
                        break;
                }
            }
        }

        private void VerifyAgent(IDictionary<string, object> settings)
		{
			VerifyAgent(settings, _agent);
		}

        private void VerifyAgent(IDictionary<string, object> settings, Mock<Agent> agent)
        {
            foreach (var pair in settings)
            {
                var setting = pair;
                switch (setting.Key)
                {
                    case "com.sun.identity.agents.config.logout.redirect.url":
                    case "com.sun.identity.agents.config.login.url":
                        agent.Verify(x => x.GetFirst(setting.Key), Times.Once());
                        break;
                    case "com.sun.identity.agents.config.session.attribute.mapping":
                        agent.Verify(x => x.GetArray(setting.Key), Times.Once());
                        break;
                    case "com.sun.identity.agents.config.profile.attribute.mapping":
                        agent.Verify(x => x.GetArray(setting.Key), Times.AtLeastOnce());
                        break;
                    case "com.sun.identity.agents.config.agent.logout.url":
                    case "com.sun.identity.agents.config.logout.cookie.reset":
                    case "com.sun.identity.agents.config.notenforced.url":
                    case "com.sun.identity.agents.config.cookie.reset":
                        agent.Verify(x => x.GetOrderedArray(setting.Key), Times.Once());
                        break;
                    case "com.sun.identity.agents.config.fetch.from.root.resource":
                    case "com.sun.identity.agents.config.ignore.path.info":
                    case "com.sun.identity.agents.config.auth.connection.timeout":
                        agent.Verify(x => x.GetSingle(setting.Key), Times.AtLeastOnce());
                        break;
                    default:
                        agent.Verify(x => x.GetSingle(setting.Key), Times.Once());
                        break;
                }
            }
        }

        private string GetAuthCookie()
        {
            var sid = Auth.login("/clients", indexType.service, "ldap", new Callback[] { new NameCallback("11111111111"), new PasswordCallback("1111111111") }).sessionId;

            return sid;
        }
    }
}