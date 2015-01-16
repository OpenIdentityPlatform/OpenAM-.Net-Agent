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
            const string logoutRedirectUrl = "redirect.url";

            SetupAgent("false", null, logoutUrls: new[] { TestUrl, DefaultUrl }, logoutResetCookies: new[] { "cookie1", "cookie2" }, logoutRedirectUrl: logoutRedirectUrl);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _response.Setup(x => x.AddHeader("Set-Cookie", "cookie1"));
            _response.Setup(x => x.AddHeader("Set-Cookie", "cookie2"));
            _response.Setup(x => x.Redirect(logoutRedirectUrl));

            _module.OnAuthentication(_context.Object);

            VerifyAgent("false", null, logoutUrls: new[] { TestUrl, DefaultUrl }, logoutResetCookies: new[] { "cookie1", "cookie2" }, logoutRedirectUrl: logoutRedirectUrl);
            _request.Verify(x => x.Url, Times.Exactly(2));
            _response.Verify(x => x.AddHeader("Set-Cookie", "cookie1"), Times.Once());
            _response.Verify(x => x.AddHeader("Set-Cookie", "cookie2"), Times.Once());
            _response.Verify(x => x.Redirect(logoutRedirectUrl), Times.Once());
        }

        [Test]
        [Description("Логаутим пользователя по com.sun.identity.agents.config.login.url, если урл совпадает с одним из com.sun.identity.agents.config.agent.logout.url")]
        public void OnAuthentication_IsLogoffWithoutLogoutRedirectUrlTest()
        {
            const string loginUrl = "login.url";

            SetupAgent("false", null, logoutUrls: new[] { TestUrl, DefaultUrl }, logoutResetCookies: new string[0], logoutRedirectUrl: null, loginUrl: loginUrl);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _response.Setup(x => x.Redirect(loginUrl));

            _module.OnAuthentication(_context.Object);

            VerifyAgent("false", null, logoutUrls: new[] { TestUrl, DefaultUrl }, logoutResetCookies: new string[0], logoutRedirectUrl: null, loginUrl: loginUrl);
            _request.Verify(x => x.Url, Times.Exactly(2));
            _response.Verify(x => x.Redirect(loginUrl), Times.Once());
        }

        [Test]
        [ExpectedException(typeof (InvalidOperationException))]
        [Description("Бросаем InvalidOperationException, если не определены com.sun.identity.agents.config.logout.redirect.url и com.sun.identity.agents.config.login.url")]
        public void OnAuthentication_IsLogoffWithoutLogoutRedirectUrlAndLoginUrlTest()
        {
            SetupAgent("false", null, logoutUrls: new[] { TestUrl, DefaultUrl }, logoutResetCookies: new string[0], logoutRedirectUrl: null, loginUrl: null);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));

            _module.OnAuthentication(_context.Object);
        }

        [Test]
        [Description("Пользователь зашел по нетребующему авторизации урлу без AuthCookie")]
        public void OnAuthentication_IsFreeUrlWithoutAuthCookieTest()
        {
            SetupAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new[] { TestUrl, DefaultUrl + "*" }, enableNotEnforced: "true");
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection());
            _context.SetupSet(x => x.User = null);

            _module.OnAuthentication(_context.Object);

            VerifyAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new[] { TestUrl, DefaultUrl + "*" }, enableNotEnforced: "true");
            _request.Verify(x => x.Url, Times.Exactly(3));
            _request.Verify(x => x.Cookies, Times.Once());
            _context.VerifySet(x => x.User = null, Times.Once());
        }

        [Test]
        [Description("Пользователь зашел по нетребующему авторизации урлу c AuthCookie")]
        public void OnAuthentication_IsFreeUrlWithAuthCookieTest()
        {
            SetupAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new[] { TestUrl, DefaultUrl }, enableNotEnforced: "true", userIdParamName: "UserId");
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection { new HttpCookie("svbid", GetAuthCookie()) });
            _context.SetupSet(x => x.User = It.IsAny<GenericPrincipal>());

            _module.OnAuthentication(_context.Object);

            VerifyAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new[] { TestUrl, DefaultUrl }, enableNotEnforced: "true");
            _request.Verify(x => x.Url, Times.Exactly(3));
            _request.Verify(x => x.Cookies, Times.Once());
            _context.VerifySet(x => x.User = It.Is<GenericPrincipal>(u => u.Identity.Name == "11111111111" && u.Identity.IsAuthenticated), Times.Once());
        }

        [Test]
        [Description("Пользователь зашел по нетребующему авторизации урлу и com.sun.identity.agents.config.notenforced.url.attributes.enable выключена")]
        public void OnAuthentication_IsFreeUrlAndAnonymousTest()
        {
            SetupAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new[] { TestUrl, DefaultUrl }, enableNotEnforced: "false");
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _context.SetupSet(x => x.User = It.IsAny<GenericPrincipal>());

            _module.OnAuthentication(_context.Object);

            VerifyAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new[] { TestUrl, DefaultUrl }, enableNotEnforced: "false");
            _request.Verify(x => x.Url, Times.Exactly(3));
            _context.VerifySet(x => x.User = It.Is<GenericPrincipal>(u => u.Identity.Name == "" && !u.Identity.IsAuthenticated), Times.Once());
        }

        [Test]
        [Description("Попытка авторизации пользователя без куки при включенной анонимной аутенфикации")]
        public void OnAuthentication_WithoutCookieAndWithAnonymousEnabledTest()
        {
            SetupAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new string[0], enableAnonymous: "true");
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection());
            _context.SetupSet(x => x.User = It.IsAny<GenericPrincipal>());

            _module.OnAuthentication(_context.Object);

            VerifyAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new string[0], enableAnonymous: "true");
            _request.Verify(x => x.Url, Times.Exactly(3));
            _request.Verify(x => x.Cookies, Times.Once());
            _context.VerifySet(x => x.User = It.Is<GenericPrincipal>(u => u.Identity.Name == "" && !u.Identity.IsAuthenticated), Times.Once());
        }

        [Test]
        [Description(@"Попытка авторизации пользователя без куки при выключенной анонимной аутенфикации 
                        c com.sun.identity.agents.config.login.url и com.sun.identity.agents.config.redirect.param")]
        public void OnAuthentication_WithoutCookieAndWithAnonymousDisabledAndLoginUrlAndRedirectParamTest()
        {
            const string loginUrl = "login.url";
            const string redirectParam = "redirect.param";

            SetupAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new string[0], enableAnonymous: "false", resetCookies: new[] { "cookie2", "cookie1" },
                loginUrl: loginUrl, redirectParam: redirectParam);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection());
            _response.Setup(x => x.AddHeader("Set-Cookie", "cookie1"));
            _response.Setup(x => x.AddHeader("Set-Cookie", "cookie2"));
            _response.Setup(x => x.Redirect(loginUrl + "?" + redirectParam + "=" + DefaultUrl));

            _module.OnAuthentication(_context.Object);

            VerifyAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new string[0], enableAnonymous: "false", resetCookies: new[] { "cookie2", "cookie1" },
                loginUrl: loginUrl, redirectParam: redirectParam);
            _request.Verify(x => x.Url, Times.Exactly(3));
            _request.Verify(x => x.Cookies, Times.Once());
            _response.Verify(x => x.AddHeader("Set-Cookie", "cookie1"), Times.Once());
            _response.Verify(x => x.AddHeader("Set-Cookie", "cookie2"), Times.Once());
            _response.Verify(x => x.Redirect(loginUrl + "?" + redirectParam + "=" + DefaultUrl), Times.Once());
        }

        [Test]
        [Description(@"Попытка авторизации пользователя без куки при выключенной анонимной аутенфикации 
                        c com.sun.identity.agents.config.login.url и без com.sun.identity.agents.config.redirect.param")]
        public void OnAuthentication_WithoutCookieAndWithAnonymousDisabledAndLoginUrlAndWithoutRedirectParamTest()
        {
            const string loginUrl = "login.url";

            SetupAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new string[0], enableAnonymous: "false", resetCookies: new string[0], loginUrl: loginUrl,
                redirectParam: null);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection());
            _response.Setup(x => x.Redirect(loginUrl));

            _module.OnAuthentication(_context.Object);

            VerifyAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new string[0], enableAnonymous: "false", resetCookies: new string[0], loginUrl: loginUrl,
                redirectParam: null);
            _request.Verify(x => x.Url, Times.Exactly(3));
            _request.Verify(x => x.Cookies, Times.Once());
            _response.Verify(x => x.Redirect(loginUrl));
        }

        [Test]
        [Description(@"Попытка авторизации пользователя без куки при выключенной анонимной аутенфикации без com.sun.identity.agents.config.login.url")]
        public void OnAuthentication_WithoutCookieAndWithAnonymousDisabledAndWithoutLoginUrlTest()
        {
            SetupAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new string[0], enableAnonymous: "false", resetCookies: new string[0], loginUrl: null);
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection());
            _response.SetupSet(x => x.StatusCode = 401);
            _response.Setup(x => x.End());

            _module.OnAuthentication(_context.Object);

            VerifyAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new string[0], enableAnonymous: "false", resetCookies: new string[0], loginUrl: null);
            _request.Verify(x => x.Url, Times.Exactly(3));
            _request.Verify(x => x.Cookies, Times.Once());
            _response.VerifySet(x => x.StatusCode = 401, Times.Once());
            _response.Verify(x => x.End(), Times.Once());
        }

        [Test]
        [Description(@"Попытка авторизации пользователя c кукой без com.sun.identity.agents.config.userid.param")]
        public void OnAuthentication_WithCookieAndUserIdParamTest()
        {
            SetupAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new string[0], userIdParamName: null, enableAnonymous: "true", enableIpValidation: "false");
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection { new HttpCookie("svbid", GetAuthCookie()) });
            _context.SetupSet(x => x.User = It.IsAny<GenericPrincipal>());

            _module.OnAuthentication(_context.Object);

            VerifyAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new string[0], userIdParamName: null, enableAnonymous: "true", enableIpValidation: "false");
            _request.Verify(x => x.Url, Times.Exactly(3));
            _request.Verify(x => x.Cookies, Times.Once()); 
            _context.VerifySet(x => x.User = It.Is<GenericPrincipal>(u => u.Identity.Name == "" && !u.Identity.IsAuthenticated), Times.Once());
        }

        [Test]
        [Description(@"Попытка авторизации пользователя c кукой, SSO only, без валидации IP и заполнением HTTP_HEADER'")]
        public void OnAuthentication_WithCookieAndSsoOnlyTest()
        {
            var items = new Dictionary<string, object>();
            var serverVariables = new NameValueCollection();
            var authCookie = GetAuthCookie();

            SetupAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new string[0], userIdParamName: "UserId", ssoOnly: "true", enableIpValidation: "false",
                mappingProps: new[] { "[MaxIdleTime]=profile-maxidletime", "[ignoreOTP]=profile-ignore-otp", "test" }, fetchMode: "HTTP_HEADER");
            _request.Setup(x => x.Url).Returns(new Uri(DefaultUrl));
            _request.Setup(x => x.Cookies).Returns(new HttpCookieCollection { new HttpCookie("svbid", authCookie) });
            _request.Setup(x => x.ServerVariables).Returns(serverVariables);
            _context.Setup(x => x.Items).Returns(items);
            _context.SetupSet(x => x.User = It.IsAny<GenericPrincipal>());

            _module.OnAuthentication(_context.Object);
            Assert.AreEqual(3, items.Count);
            Assert.AreEqual(2, serverVariables.Count);
            Assert.AreEqual(authCookie, items["am_auth_cookie"]);
            Assert.IsNotNull(items["profile-maxidletime"]);
            Assert.IsNotNull(serverVariables["profile-maxidletime"]);
            Assert.IsNotNull(items["profile-ignore-otp"]);
            Assert.IsNotNull(serverVariables["profile-ignore-otp"]);

            VerifyAgent("false", null, logoutUrls: new string[0], notEnforcedUrls: new string[0], userIdParamName: "UserId", ssoOnly: "true", enableIpValidation: "false");
            _request.Verify(x => x.Url, Times.Exactly(4));
            _request.Verify(x => x.Cookies, Times.Exactly(2));
            _request.Verify(x => x.ServerVariables, Times.Exactly(2));
            _context.Verify(x => x.Items, Times.Exactly(3));
            _context.VerifySet(x => x.User = It.Is<GenericPrincipal>(u => u.Identity.Name == "11111111111" && u.Identity.IsAuthenticated), Times.Once());
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

            VerifyAgent(enableRedirect, redirectHost, redirectPrefix, overrideProtocol, overridePort);
            _request.Verify(x => x.Url, Times.Exactly(2));
            _response.Verify(x => x.Redirect(redirectUrl), Times.Once());
        }

        private void SetupAgent(
            string enableRedirect = DefaultStringValue,
            string redirectHost = DefaultStringValue,
            string redirectPrefix = DefaultStringValue,
            string overrideProtocol = DefaultStringValue,
            string overridePort = DefaultStringValue,
            string[] logoutUrls = null,
            string[] logoutResetCookies = null,
            string logoutRedirectUrl = DefaultStringValue,
            string loginUrl = DefaultStringValue,
            string[] notEnforcedUrls = null,
            string enableNotEnforced = DefaultStringValue,
            string userIdParamName = DefaultStringValue,
            string enableAnonymous = DefaultStringValue,
            string[] resetCookies = null,
            string redirectParam = DefaultStringValue,
            string enableIpValidation = DefaultStringValue,
            string ssoOnly = DefaultStringValue,
            string[] mappingProps = null,
            string fetchMode = DefaultStringValue)
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

            if (logoutResetCookies != null)
            {
                _agent.Setup(x => x.GetOrderedArray("com.sun.identity.agents.config.logout.cookie.reset")).Returns(logoutResetCookies);
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

            if (enableNotEnforced != DefaultStringValue)
            {
                _agent.Setup(x => x.GetSingle("com.sun.identity.agents.config.notenforced.url.attributes.enable")).Returns(enableNotEnforced);
            }

            if (userIdParamName != DefaultStringValue)
            {
                _agent.Setup(x => x.GetSingle("com.sun.identity.agents.config.userid.param")).Returns(userIdParamName);
            }

            if (enableAnonymous != DefaultStringValue)
            {
                _agent.Setup(x => x.GetSingle("com.sun.identity.agents.config.anonymous.user.enable")).Returns(enableAnonymous);
            }

            if (resetCookies != null)
            {
                _agent.Setup(x => x.GetOrderedArray("com.sun.identity.agents.config.cookie.reset")).Returns(resetCookies);
            }

            if (redirectParam != DefaultStringValue)
            {
                _agent.Setup(x => x.GetSingle("com.sun.identity.agents.config.redirect.param")).Returns(redirectParam);
            }

            if (enableIpValidation != DefaultStringValue)
            {
                _agent.Setup(x => x.GetSingle("com.sun.identity.agents.config.client.ip.validation.enable")).Returns(enableIpValidation);
            }

            if (ssoOnly != DefaultStringValue)
            {
                _agent.Setup(x => x.GetSingle("com.sun.identity.agents.config.sso.only")).Returns(ssoOnly);
            }

            if (mappingProps != null)
            {
                _agent.Setup(x => x.GetArray("com.sun.identity.agents.config.session.attribute.mapping")).Returns(mappingProps);
            }

            if (fetchMode != DefaultStringValue)
            {
                _agent.Setup(x => x.GetSingle("com.sun.identity.agents.config.session.attribute.fetch.mode")).Returns(fetchMode);
            }

            _agent.Setup(x => x.GetSingle("com.sun.identity.agents.config.auth.connection.timeout")).Returns("1");
        }

        private void VerifyAgent(
            string enableRedirect = DefaultStringValue,
            string redirectHost = DefaultStringValue,
            string redirectPrefix = DefaultStringValue,
            string overrideProtocol = DefaultStringValue,
            string overridePort = DefaultStringValue,
            string[] logoutUrls = null,
            string[] logoutResetCookies = null,
            string logoutRedirectUrl = DefaultStringValue,
            string loginUrl = DefaultStringValue,
            string[] notEnforcedUrls = null,
            string enableNotEnforced = DefaultStringValue,
            string userIdParamName = DefaultStringValue,
            string enableAnonymous = DefaultStringValue,
            string[] resetCookies = null,
            string redirectParam = DefaultStringValue,
            string enableIpValidation = DefaultStringValue,
            string ssoOnly = DefaultStringValue,
            string[] mappingProps = null,
            string fetchMode = DefaultStringValue)
        {
            if (enableRedirect != DefaultStringValue)
            {
                _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.override.host"), Times.Once());
            }

            if (redirectHost != DefaultStringValue)
            {
                _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.fqdn.default"), Times.Once());
            }

            if (redirectPrefix != DefaultStringValue)
            {
                _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.agenturi.prefix"), Times.Once());
            }

            if (overrideProtocol != DefaultStringValue)
            {
                _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.override.protocol"), Times.Once());
            }

            if (overridePort != DefaultStringValue)
            {
                _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.override.port"), Times.Once());
            }

            if (logoutUrls != null)
            {
                _agent.Verify(x => x.GetOrderedArray("com.sun.identity.agents.config.agent.logout.url"), Times.Once());
            }

            if (logoutResetCookies != null)
            {
                _agent.Verify(x => x.GetOrderedArray("com.sun.identity.agents.config.logout.cookie.reset"), Times.Once());
            }

            if (logoutRedirectUrl != DefaultStringValue)
            {
                _agent.Verify(x => x.GetFirst("com.sun.identity.agents.config.logout.redirect.url"), Times.Once());
            }

            if (loginUrl != DefaultStringValue)
            {
                _agent.Verify(x => x.GetFirst("com.sun.identity.agents.config.login.url"), Times.Once());
            }

            if (notEnforcedUrls != null)
            {
                _agent.Verify(x => x.GetOrderedArray("com.sun.identity.agents.config.notenforced.url"), Times.Once());
            }

            if (enableNotEnforced != DefaultStringValue)
            {
                _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.notenforced.url.attributes.enable"), Times.Once());
            }

            if (userIdParamName != DefaultStringValue)
            {
                _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.userid.param"), Times.Once());
            }

            if (enableAnonymous != DefaultStringValue)
            {
                _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.anonymous.user.enable"), Times.Once());
            }

            if (resetCookies != null)
            {
                _agent.Verify(x => x.GetOrderedArray("com.sun.identity.agents.config.cookie.reset"), Times.Once());
            }

            if (redirectParam != DefaultStringValue)
            {
                _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.redirect.param"), Times.Once());
            }

            if (enableIpValidation != DefaultStringValue)
            {
                _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.client.ip.validation.enable"), Times.Once());
            }

            if (ssoOnly != DefaultStringValue)
            {
                _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.sso.only"), Times.Once());
            }

            if (mappingProps != null)
            {
                _agent.Verify(x => x.GetArray("com.sun.identity.agents.config.session.attribute.mapping"), Times.Once());
            }

            if (fetchMode != DefaultStringValue)
            {
                _agent.Verify(x => x.GetSingle("com.sun.identity.agents.config.session.attribute.fetch.mode"), Times.Once());
            }
        }

        private string GetAuthCookie()
        {
            var sid = Auth.login("/clients", indexType.service, "ldap", new Callback[] { new NameCallback("11111111111"), new PasswordCallback("1111111111") }).sessionId;

            return sid;
        }
    }
}