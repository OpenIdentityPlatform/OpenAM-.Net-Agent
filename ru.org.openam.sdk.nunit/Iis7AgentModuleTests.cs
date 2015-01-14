using System;
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
    public class Iis7AgentModuleTests
    {
        private const string DefaultStringValue = "~DefaultStringValue~";
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
            _agent = new Mock<Agent>(MockBehavior.Strict);

            _module = new iis7AgentModule(_agent.Object);

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
            _request.Setup(x => x.Url).Returns((Uri) null);

            _module.OnAuthentication(_context.Object);

            _context.Verify(x => x.Request, Times.Once());
            _request.Verify(x => x.Url, Times.Once());
        }

        [Test]
        [Description("Редиректим, если урлы не совпадают без переопределения протокола и порта")]
        public void OnAuthentication_CheckUrlTest()
        {
            CheckUrlTest(DefaultUrl + "path/?id=1", "http://redirect.url:80/path/?id=1", "true", "redirect.url", "", "false", "false");
        }

        [Test]
        [Description("Редиректим, если урлы не совпадают без переопределения протокола и порта")]
        public void OnAuthentication_CheckUrlWithRewriteProtocolTrueTest()
        {
            CheckUrlTest(DefaultUrl + "path/?id=1", "http://redirect.url:80/path/?id=1", "true", "redirect.url", "", "true", "false");
        }

        [Test]
        [Description("Редиректим, если урлы не совпадают без переопределения протокола и порта")]
        public void OnAuthentication_CheckUrlWithRewritePortTrueTest()
        {
            CheckUrlTest(DefaultUrl + "path/?id=1", "http://redirect.url:80/path/?id=1", "true", "redirect.url", "", "false", "true");
        }

        [Test]
        [Description("Редиректим, если урлы не совпадают без переопределения протокола и порта")]
        public void OnAuthentication_CheckUrlWithRewriteProtocolTrueAndRewritePortTrueTest()
        {
            CheckUrlTest(DefaultUrl + "path/?id=1", "http://redirect.url:80/path/?id=1", "true", "redirect.url", "", "true", "true");
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
            const string logoutRedirectUrl = "redirect.url";

            SetupAgent("false", "", logoutUrls: new[] { TestUrl, DefaultUrl }, resetCookies: new[] { "cookie1", "cookie2" }, logoutRedirectUrl: logoutRedirectUrl);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _response.Setup(x => x.AddHeader("Set-Cookie", "cookie1"));
            _response.Setup(x => x.AddHeader("Set-Cookie", "cookie2"));
            _response.Setup(x => x.Redirect(logoutRedirectUrl));

            _module.OnAuthentication(_context.Object);

            _request.Verify(x => x.Url, Times.Exactly(2));
            VerifyAgentConfig(logout: true);
            _response.Verify(x => x.AddHeader("Set-Cookie", "cookie1"), Times.Once());
            _response.Verify(x => x.AddHeader("Set-Cookie", "cookie2"), Times.Once());
            _response.Verify(x => x.Redirect(logoutRedirectUrl), Times.Once());
        }

        [Test]
        [Description("Логаутим пользователя по com.sun.identity.agents.config.login.url, если урл совпадает с одним из com.sun.identity.agents.config.agent.logout.url")]
        public void OnAuthentication_IsLogoffWithoutLogoutRedirectUrlTest()
        {
            const string loginUrl = "login.url";

            SetupAgent("false", "", logoutUrls: new[] { TestUrl, DefaultUrl }, resetCookies: new string[0], logoutRedirectUrl: null, loginUrl: loginUrl);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _response.Setup(x => x.Redirect(loginUrl));

            _module.OnAuthentication(_context.Object);

            _request.Verify(x => x.Url, Times.Exactly(2));
            VerifyAgentConfig(logout: true);
            _response.Verify(x => x.Redirect(loginUrl), Times.Once());
        }

        [Test]
        [ExpectedException(typeof (InvalidOperationException))]
        [Description("Бросаем InvalidOperationException, если не определены com.sun.identity.agents.config.logout.redirect.url и com.sun.identity.agents.config.login.url")]
        public void OnAuthentication_IsLogoffWithoutLogoutRedirectUrlAndLoginUrlTest()
        {
            SetupAgent("false", "", logoutUrls: new[] { TestUrl, DefaultUrl }, resetCookies: new string[0], logoutRedirectUrl: null, loginUrl: null);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));

            _module.OnAuthentication(_context.Object);
        }

        [Test]
        [Description("Пользователь зашел по нетребующему авторизации урлу без AuthCookie")]
        public void OnAuthentication_IsFreeUrlWithoutAuthCookieTest()
        {
            var cookies = new HttpCookieCollection();

            SetupAgent("false", "", logoutUrls: new string[0], notEnforcedUrls: new[] { TestUrl, DefaultUrl + "*" }, notEnforcedEnabled: "true");
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(cookies);
            _context.SetupSet(x => x.User = null);

            _module.OnAuthentication(_context.Object);

            _request.Verify(x => x.Url, Times.Exactly(3));
            VerifyAgentConfig(freeUrl: true);
            _context.VerifySet(x => x.User = null, Times.Once());
        }

        [Test]
        [Description("Пользователь зашел по нетребующему авторизации урлу c AuthCookie")]
        public void OnAuthentication_IsFreeUrlWithAuthCookieTest()
        {
            var cookies = new HttpCookieCollection { new HttpCookie("svbid", GetAuthCookie()) };

            SetupAgent("false", "", logoutUrls: new string[0], notEnforcedUrls: new[] { TestUrl, DefaultUrl }, notEnforcedEnabled: "true");
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(cookies);
            _context.SetupSet(x => x.User = It.IsAny<GenericPrincipal>());

            _module.OnAuthentication(_context.Object);

            _request.Verify(x => x.Url, Times.Exactly(3));
            VerifyAgentConfig(freeUrl: true);
            _context.VerifySet(x => x.User = It.Is<GenericPrincipal>(u => u.Identity.Name == "11111111111" && u.Identity.IsAuthenticated), Times.Once());
        }

        [Test]
        [Description("Пользователь зашел по нетребующему авторизации урлу и com.sun.identity.agents.config.notenforced.url.attributes.enable выключена")]
        public void OnAuthentication_IsFreeUrlAndAnonymousTest()
        {
            var cookies = new HttpCookieCollection { new HttpCookie("svbid", GetAuthCookie()) };

            SetupAgent("false", "", logoutUrls: new string[0], notEnforcedUrls: new[] { TestUrl, DefaultUrl }, notEnforcedEnabled: "false");
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(cookies);
            _context.SetupSet(x => x.User = It.IsAny<GenericPrincipal>());

            _module.OnAuthentication(_context.Object);

            _request.Verify(x => x.Url, Times.Exactly(3));
            VerifyAgentConfig(freeUrl: true);
            _context.VerifySet(x => x.User = It.Is<GenericPrincipal>(u => u.Identity.Name == "" && !u.Identity.IsAuthenticated), Times.Once());
        }

        private void CheckUrlTest(
            string url,
            string redirectUrl,
            string enableRedirect,
            string redirectHost,
            string redirectPrefix = DefaultStringValue,
            string overrideProtocol = DefaultStringValue,
            string overridePort = DefaultStringValue)
        {
            SetupAgent(enableRedirect, redirectHost, redirectPrefix, overrideProtocol, overridePort);
            _request.Setup(x => x.Url).Returns(new Uri(url));
            _response.Setup(x => x.Redirect(redirectUrl));

            _module.OnAuthentication(_context.Object);

            _request.Verify(x => x.Url, Times.Exactly(2));
            VerifyAgentConfig(true);
            _response.Verify(x => x.Redirect(redirectUrl), Times.Once());
        }

        

        private void SetupAgent(
            string enableRedirect = DefaultStringValue,
            string redirectHost = DefaultStringValue,
            string redirectPrefix = DefaultStringValue,
            string overrideProtocol = DefaultStringValue,
            string overridePort = DefaultStringValue,
            string[] logoutUrls = null,
            string[] resetCookies = null,
            string logoutRedirectUrl = DefaultStringValue,
            string loginUrl = DefaultStringValue,
            string[] notEnforcedUrls = null,
            string notEnforcedEnabled = DefaultStringValue)
        {
            if (enableRedirect != DefaultStringValue)
            {
                _agent.Setup(x => x.GetSingle("com.sun.identity.agents.config.override.host")).Returns(enableRedirect);
            }

            if (redirectHost != DefaultStringValue)
            {
                _agent.Setup(x => x.GetSingle("com.sun.identity.agents.config.fqdn.default")).Returns(redirectHost);
            }

            if (redirectPrefix != DefaultStringValue)
            {
                _agent.Setup(x => x.GetSingle("com.sun.identity.agents.config.agenturi.prefix")).Returns(redirectPrefix);
            }

            if (overrideProtocol != DefaultStringValue)
            {
                _agent.Setup(x => x.GetSingle("com.sun.identity.agents.config.override.protocol")).Returns(overrideProtocol);
            }

            if (overridePort != DefaultStringValue)
            {
                _agent.Setup(x => x.GetSingle("com.sun.identity.agents.config.override.port")).Returns(overridePort);
            }

            if (logoutUrls != null)
            {
                _agent.Setup(x => x.GetOrderedArray("com.sun.identity.agents.config.agent.logout.url")).Returns(logoutUrls);
            }

            if (resetCookies != null)
            {
                _agent.Setup(x => x.GetOrderedArray("com.sun.identity.agents.config.logout.cookie.reset")).Returns(resetCookies);
            }

            if (logoutRedirectUrl != DefaultStringValue)
            {
                _agent.Setup(x => x.GetFirst("com.sun.identity.agents.config.logout.redirect.url")).Returns(logoutRedirectUrl);
            }

            if (loginUrl != DefaultStringValue)
            {
                _agent.Setup(x => x.GetFirst("com.sun.identity.agents.config.login.url")).Returns(loginUrl);
            }

            if (notEnforcedUrls != null)
            {
                _agent.Setup(x => x.GetOrderedArray("com.sun.identity.agents.config.notenforced.url")).Returns(notEnforcedUrls);
            }

            if (notEnforcedEnabled != DefaultStringValue)
            {
                _agent.Setup(x => x.GetSingle("com.sun.identity.agents.config.notenforced.url.attributes.enable")).Returns(notEnforcedEnabled);
            }

            _agent.Setup(x => x.GetSingle("com.sun.identity.agents.config.auth.connection.timeout")).Returns("1");
            _agent.Setup(x => x.GetSingle("com.sun.identity.agents.config.userid.param")).Returns("UserId");
        }

        private void VerifyAgentConfig(bool redirect = false, bool logout = false, bool freeUrl = false, bool anonymous = false)
        {
            _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.override.host"), Times.Once());
            _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.fqdn.default"), Times.Once());

            if (redirect)
            {
                _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.agenturi.prefix"), Times.Once());
                _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.override.protocol"), Times.Once());
                _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.override.port"), Times.Once());
                return;
            }

            _agent.Verify(x => x.GetOrderedArray("com.sun.identity.agents.config.agent.logout.url"), Times.Once());
            if (logout)
            {
                _agent.Verify(x => x.GetOrderedArray("com.sun.identity.agents.config.logout.cookie.reset"), Times.Once());
                _agent.Verify(x => x.GetFirst("com.sun.identity.agents.config.logout.redirect.url"), Times.Once());
                return;
            }

            _agent.Verify(x => x.GetOrderedArray("com.sun.identity.agents.config.notenforced.url"), Times.Once());
            if (freeUrl)
            {
                _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.notenforced.url.attributes.enable"), Times.Once());
            }
        }

        private string GetAuthCookie()
        {
            var sid = Auth.login("/clients", indexType.service, "ldap", new Callback[] { new NameCallback("11111111111"), new PasswordCallback("1111111111") }).sessionId;

            return sid;
        }
    }
}