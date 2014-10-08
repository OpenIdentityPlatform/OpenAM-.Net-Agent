using System;
using System.Security.Principal;
using System.Web;
using ru.org.openam.sdk;
using ru.org.openam.sdk.session;

// todo доимплиментировать настройки
// todo проверить все кейсы авторизации
// todo авторизация

namespace ru.org.openam.iis7Agent
{
	public class iis7AgentModule : IHttpModule
	{
		private HttpApplication _app;

		private readonly Agent agent = new Agent();

		public void Init(HttpApplication context)
		{
			this._app = context;
			this._app.BeginRequest += OnBeginRequest;
			this._app.AuthenticateRequest += OnAuthentication;
			this._app.EndRequest += OnEndRequest;
		}

		private void OnEndRequest(object sender, EventArgs e)
		{
			Log.Trace("End request");
		}

		private void OnBeginRequest(object sender, EventArgs e)
		{	
			Log.Trace(string.Format("Begin request url: {0} ip: {1}",  _app.Context.Request.Url.AbsoluteUri, _app.Context.Request.UserHostAddress));
		}

		void OnAuthentication(object sender, EventArgs a)
		{
			try
			{
				var nUrl = CheckUrl();
				if(nUrl != null)
				{
					Log.AuditTrace(string.Format("Request {0} was redirected to {1}",  _app.Context.Request.Url.AbsoluteUri, nUrl));
					_app.Response.Redirect(nUrl);
					return;
				}

				if(IsLogOff())
				{
					Log.AuditTrace(string.Format("Logoff {0}", _app.Context.Request.Url.AbsoluteUri));

					ResetCookie("com.sun.identity.agents.config.logout.cookie.reset");

					var url = agent.GetFirst("com.sun.identity.agents.config.logout.redirect.url");
					if(url == null)
					{
						url = agent.GetFirst("com.sun.identity.agents.config.login.url");
					}
					_app.Response.Redirect(url);
					return;
				}

				if (IsFree())
				{
					if(agent.GetSingle("com.sun.identity.agents.config.notenforced.url.attributes.enable") == "true")
					{
						_app.Context.User = GetUser();
					}
					else
					{
						_app.Context.User = GetAnonymous();
					}

					Log.AuditTrace(string.Format("Free access allowed to {0}", _app.Context.Request.Url.AbsoluteUri));
					return;
				}

				
				var user = GetUser();
				if (user != null)
				{	
					_app.Context.User = user;
					MapArrtsProps();
					Log.Audit(string.Format("User {0} was allowed access to {1}", user.Identity.Name, _app.Context.Request.Url.AbsoluteUri));
				}
				else if(agent.GetSingle("com.sun.identity.agents.config.anonymous.user.enable") == "true")
				{
					_app.Context.User = GetAnonymous();
					Log.AuditTrace(string.Format("Anonymous access allowed to {0}", _app.Context.Request.Url.AbsoluteUri));
				}
				else
				{
					ResetCookie("com.sun.identity.agents.config.cookie.reset");

					// todo юзер ид
					Log.Audit(string.Format("User {0} was denied access to {1}", null, _app.Context.Request.Url.AbsoluteUri));
					_app.Response.Redirect(GetLogoffUrl());
				}
			}
			catch (Exception ex)
			{
				Log.Fatal(ex);
				throw;
			}
		}

		private void ResetCookie(string cfg)
		{
			var resetCookie = agent.GetHashSet(cfg);
			foreach (var c in resetCookie)
			{
				var cookie = c.Substring(c.IndexOf("=") + 1);
				_app.Context.Response.AddHeader("Set-Cookie", cookie);
			}
		}

		private bool IsLogOff()
		{
			var logOffUrls = agent.GetHashSet("com.sun.identity.agents.config.agent.logout.url");
			foreach (var u in logOffUrls)
			{
				var url = u.Substring(u.IndexOf("=")+1);
				if(new Uri(url).AbsoluteUri == _app.Context.Request.Url.AbsoluteUri)
				{
					return true;
				}
			}
			
			return false;
		}


		private string GetLogoffUrl()
		{	 
			var url = agent.GetFirst("com.sun.identity.agents.config.login.url");
			if (url != null)
			{ 
				var gotoName = agent.GetSingle("com.sun.identity.agents.config.redirect.param");
				if(!string.IsNullOrWhiteSpace(gotoName))
				{
					if(url.Contains("?"))
					{
						url += "&";
					}
					else
					{
						url += "?";
					}
					url += gotoName + "=" + HttpUtility.UrlPathEncode(_app.Request.Url.AbsoluteUri);
				}

				return url;
			}
			return null;
		}

		private GenericPrincipal GetUser()
		{
			Session session;
			try
			{
				session = GetUserSession();
			}
			catch(SessionException ex)
			{
				Log.Warning(string.Format("SessionException was thrown {0}{1}", Environment.NewLine, ex));
				return null;
			}

			var userId = GetUserId(session);
			if(session == null || userId == null)
			{
				return null;
			}

			var identity = new GenericIdentity(userId);
			var principal = new GenericPrincipal(identity, new string[0]);
			return principal;
		}

		private GenericPrincipal GetAnonymous()
		{
			var identity = new GenericIdentity("");
			var principal = new GenericPrincipal(identity, new string[0]);
			return principal;
		}

		private bool IsFree()
		{
			var freeUrls = agent.GetHashSet("com.sun.identity.agents.config.notenforced.url");
			foreach (var u in freeUrls)
			{
				var url = u.Substring(u.IndexOf("=")+1);
				if(url.EndsWith("*") && _app.Context.Request.Url.AbsoluteUri.StartsWith(url, StringComparison.InvariantCultureIgnoreCase))
				{
					return true;
				}
				else if(_app.Context.Request.Url.AbsoluteUri.Equals(url, StringComparison.InvariantCultureIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}
		
		private void MapArrtsProps()
		{
			var props = GetUserSession().token.property;
			var mapStrs = agent.GetHashSet("com.sun.identity.agents.config.session.attribute.mapping");
			var fetchMode = agent.GetSingle("com.sun.identity.agents.config.session.attribute.fetch.mode");
			// todo var mapStrs = agent.GetConfig()["com.sun.identity.agents.config.profile.attribute.mapping"] as HashSet<string>;
			if(mapStrs == null)
			{
				return;
			}

			foreach (var mapStr in mapStrs)
			{
				var vals = mapStr.Split('=');
				if(vals.Length != 2)
				{
					continue;
				}

				var key = vals[0].Substring(1);
				key = key.Substring(0, key.Length-1);
				if(props.ContainsKey(key))
				{
					_app.Context.Items[vals[1]] = props[key];
					if(fetchMode == "HTTP_HEADER")
					{
						_app.Context.Request.ServerVariables[vals[1]] = props[key];
					}
					else if(fetchMode == "HTTP_COOKIE")
					{
						_app.Context.Request.Cookies.Set(new HttpCookie(vals[1], props[key]));
					}
				}
			}

			_app.Context.Items["am_auth_cookie"] = GetAuthCookie();
		}

		private string CheckUrl()
		{
			var enabled = agent.GetSingle("com.sun.identity.agents.config.override.host") == "true";
			var url = agent.GetSingle("com.sun.identity.agents.config.fqdn.default");
			if(enabled && !string.IsNullOrWhiteSpace(url) 
				&& !_app.Context.Request.Url.Host.Equals(url, StringComparison.InvariantCultureIgnoreCase))
			{
				var agentUrlStr = agent.GetSingle("com.sun.identity.agents.config.agenturi.prefix"); 
				var rewriteProtocol = agent.GetSingle("com.sun.identity.agents.config.override.protocol") == "true";
				var rewritePort = agent.GetSingle("com.sun.identity.agents.config.override.port") == "true";
				var res = "";

				int? port = null;
				string protocol = null;
				if((rewriteProtocol || rewritePort) && !string.IsNullOrWhiteSpace(agentUrlStr))
				{
					var uri = new Uri(agentUrlStr);
					port = uri.Port;
					protocol = uri.Scheme;
				}

				if(!string.IsNullOrWhiteSpace(protocol))
				{
					res += protocol +"://";
				}
				else
				{
					res += _app.Context.Request.Url.Scheme + "://";
				}

				res += url;

				if(port.HasValue)
				{
					res += ":" + port;
				}

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

			var uid = session.token.property[agent.GetSingle("com.sun.identity.agents.config.userid.param")];
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
