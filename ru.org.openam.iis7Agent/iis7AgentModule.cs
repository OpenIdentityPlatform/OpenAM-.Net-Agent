using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using ru.org.openam.sdk;

// todo аудит и анхендлед в логи
// todo проверить поступ к статику
// todo доимплиментировать настройки
using ru.org.openam.sdk.session;

namespace ru.org.openam.iis7Agent
{
	public class iis7AgentModule : IHttpModule
	{
		private HttpApplication _app;

		private readonly Agent agent = new Agent();

		public void Init(HttpApplication context)
		{
			this._app = context;
			this._app.AuthenticateRequest += OnAuthentication;
		}

		void OnAuthentication(object sender, EventArgs a)
		{
			var nUrl = CheckUrl();
			if(nUrl != null)
			{
				_app.Response.Redirect(nUrl);
				return;
			}

			Session session;
			try
			{
				session = GetUserSession();
			}
			catch(SessionException)
			{
				_app.Response.Redirect(agent.GetLoginUrl());	
				return;
			}

			var userId = GetUserId(session);
			var freeUrls = agent.GetConfig()["com.sun.identity.agents.config.notenforced.url"] as HashSet<string>;
			if (session != null && userId != null)
			{
				var identity = new GenericIdentity(userId);
				var principal = new GenericPrincipal(identity, new string[0]);
				_app.Context.User = principal;

				// todo замапить из маппинга
				foreach (var prop in session.token.property)
				{
					_app.Context.Items["am_" + prop.Key] = prop.Value;
				}
				_app.Context.Items["am_auth_cookie"] = GetAuthCookie();
			}
			else if (freeUrls == null || freeUrls.All(u => !_app.Context.Request.Url.LocalPath.StartsWith(u, StringComparison.InvariantCultureIgnoreCase)))
			{
				_app.Response.Redirect(agent.GetLoginUrl());
			}
//			else if((string)agent.GetConfig()["com.sun.identity.agents.config.notenforced.url.attributes.enable"] == "true")
//			{
//
//			}
		}

		private string CheckUrl()
		{
			var enabled = (string)agent.GetConfig()["com.sun.identity.agents.config.notenforced.url.attributes.enable"] == "true";
			var url = (string)agent.GetConfig()["com.sun.identity.agents.config.fqdn.default"];
			if(enabled && !string.IsNullOrWhiteSpace(url) 
				&& !_app.Context.Request.Url.Host.Equals(url, StringComparison.InvariantCultureIgnoreCase))
			{
				var protocol = (string)agent.GetConfig()["com.sun.identity.agents.config.override.protocol"];
				var port = (string)agent.GetConfig()["com.sun.identity.agents.config.override.port"];
				var res = "";

//			todo	if(!string.IsNullOrWhiteSpace(protocol))
//				{
//					res += protocol +"://";
//				}
//				else
				{
					res += "//";
				}

				res += url;

//	todo			if(!string.IsNullOrWhiteSpace(port))
//				{
//					res += ":" + port;
//				}

				res += _app.Context.Request.Url.PathAndQuery;

				return res;
			}

			return null;
		}

		private string GetUserId(Session session)
		{
			if (session == null || session.token == null)
			{
				return null;
			}

			var uid = session.token.property[(string)agent.GetConfig()["com.sun.identity.agents.config.userid.param"]];
			return uid;
		}

		private Session GetUserSession()
		{
			var auth = GetAuthCookie();
			if (auth == null)
			{
				return null;
			}

			var userSession = Cache.GetOrDefault
			(
				"am_" + auth,
				() => Session.getSession(agent, _app.Context.Request)
				, r =>
				{
					if (r != null && r.token != null)
					{
						return r.token.maxcaching;
					}

					return 0;
				}
			);
			return userSession;
		}


		private string GetAuthCookie()
		{
			var cookie = _app.Context.Request.Cookies[agent.GetCookieName()];
			if (cookie == null || string.IsNullOrWhiteSpace(cookie.Value))
			{
				return null;
			}

			return cookie.Value;
		}

		public void Dispose() { }
	}
}
