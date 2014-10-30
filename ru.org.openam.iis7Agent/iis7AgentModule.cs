using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Web;
using ru.org.openam.sdk;
using ru.org.openam.sdk.session;

// todo доимплиментировать настройки
// todo проверить все кейсы авторизации
// todo маппинг policy.result.attributes.Count > 0); проверить
// todo проверить sla timeout
// todo потестить новый кэш в сессии и в полиси
// todo брать сессию 1 раз
namespace ru.org.openam.iis7Agent
{
	public class iis7AgentModule : IHttpModule
	{
		private HttpApplication _app;

		private readonly Agent _agent = new Agent();

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
			Log.Trace(string.Format("Begin request url: {0} ip: {1}",  _app.Context.Request.Url.AbsoluteUri, GetUserIp()));
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

					var url = _agent.GetFirst("com.sun.identity.agents.config.logout.redirect.url");
					if(url == null)
					{
						url = _agent.GetFirst("com.sun.identity.agents.config.login.url");
					}
					_app.Response.Redirect(url);
					return;
				}

				if (IsFree())
				{
					if(_agent.GetSingle("com.sun.identity.agents.config.notenforced.url.attributes.enable") == "true")
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
				var autorized = false;
				if(user != null)
				{
					if(_agent.GetSingle("com.sun.identity.agents.config.sso.only") != "true")
					{
						var policy = Policy.Get(_agent, GetUserSession(), _app.Context.Request.Url, null, GetAttrsNames());
						if(policy.result.isAllow(_app.Context.Request.HttpMethod))
						{
							MapPolicyProps(policy.result.attributes);
							autorized = true;
							Log.AuditTrace(string.Format("User {0} was autorized to {1}", user.Identity.Name, _app.Context.Request.Url.AbsoluteUri));
						}
					}
					else
					{
						Log.AuditTrace(string.Format("User {0} was not autorized to {1}", user.Identity.Name, _app.Context.Request.Url.AbsoluteUri));
						autorized = true;
					}
				}

				if(IsInvalidIp())
				{
					autorized = false;
				}

				if (user != null && autorized)
				{	
					_app.Context.User = user;
					MapArrtsProps();
					Log.Audit(string.Format("User {0} was allowed access to {1}", user.Identity.Name, _app.Context.Request.Url.AbsoluteUri));
				}
				else if(_agent.GetSingle("com.sun.identity.agents.config.anonymous.user.enable") == "true")
				{
					_app.Context.User = GetAnonymous();
					Log.AuditTrace(string.Format("Anonymous access allowed to {0}", _app.Context.Request.Url.AbsoluteUri));
				}
				else
				{
					ResetCookie("com.sun.identity.agents.config.cookie.reset");

					string userId = null;
					if(user != null)
					{
						userId = user.Identity.Name;
					}
					var status = user == null ? 401 : 403;
					Log.Audit(string.Format("User {0} was denied access to {1} ({2})", userId, _app.Context.Request.Url.AbsoluteUri, status));
					var logoffUrl = GetLogoffUrl(user == null ? "com.sun.identity.agents.config.login.url" : "com.sun.identity.agents.config.access.denied.url");
					if(logoffUrl != null)
					{
						_app.Response.Redirect(logoffUrl);
					}
					else
					{
						_app.Response.StatusCode = user == null ? 401 : 403;
						_app.Response.End();
					}
				}
			}
			catch (Exception ex)
			{
				Log.Fatal(ex);
				throw;
			}
		}

		private bool IsInvalidIp()
		{
			if(_agent.GetSingle("com.sun.identity.agents.config.client.ip.validation.enable") != "true")
			{
				return false;
			}

			var session = GetUserSession();
			var props = session.token.property;
			if(!props.ContainsKey("Host"))
			{
				return false;
			}
			var host = props["Host"];
			var userIp = GetUserIp();
			if(host == userIp)
			{
				return false;
			}
									   
			Log.AuditTrace(string.Format("User {0} ip change detected, last: {1} current: {2}", GetUserId(session), host, userIp));
			return true;
		}

		private string GetUserIp(){
			var headerName = _agent.GetSingle("com.sun.identity.agents.config.client.ip.header");
			var userIp = _app.Request.UserHostAddress;
			if(!string.IsNullOrWhiteSpace(headerName))
			{
				if(headerName.StartsWith("HTTP_"))
				{
					var c = _app.Context.Request.ServerVariables.Count;
					userIp = _app.Context.Request.ServerVariables[headerName];
				}
				else
				{
					userIp = _app.Context.Request.Headers[headerName];
				}
			}

			if(userIp != null && userIp.Contains(","))
			{
				userIp = userIp.Substring(0, userIp.IndexOf(",", StringComparison.Ordinal) + 1);
			}

			return userIp;
		}

		private ICollection<string> GetAttrsNames()
		{
			var attrs = _agent.GetHashSet("com.sun.identity.agents.config.profile.attribute.mapping");
			var list = new List<string>();
			foreach (var attr in attrs)
			{
				var vals = attr.Split('=');
				if(vals.Length != 2)
				{
					continue;
				}

				var key = vals[0].Substring(1);
				key = key.Substring(0, key.Length-1);
				list.Add(key);
			}

			return list;
		}

		private void ResetCookie(string cfg)
		{
			var resetCookie = _agent.GetOrderedHashSet(cfg);
			foreach (var cookie in resetCookie)
			{
				_app.Context.Response.AddHeader("Set-Cookie", cookie);
			}
		}

		private bool IsLogOff()
		{
			var logOffUrls = _agent.GetOrderedHashSet("com.sun.identity.agents.config.agent.logout.url");
			foreach (var url in logOffUrls)
			{
				if(new Uri(url).AbsoluteUri == _app.Context.Request.Url.AbsoluteUri)
				{
					return true;
				}
			}
			
			return false;
		}


		private string GetLogoffUrl(string urlProp)
		{	 
			var url = _agent.GetFirst(urlProp);
			if (url != null)
			{ 
				var gotoName = _agent.GetSingle("com.sun.identity.agents.config.redirect.param");
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
			var freeUrls = _agent.GetOrderedHashSet("com.sun.identity.agents.config.notenforced.url");
			foreach (var url in freeUrls)
			{
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
			var mapStrs = _agent.GetHashSet("com.sun.identity.agents.config.session.attribute.mapping");
			var fetchMode = _agent.GetSingle("com.sun.identity.agents.config.session.attribute.fetch.mode");
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

			_app.Context.Items["am_auth_cookie"] = _agent.GetAuthCookie(_app.Context.Request);
		}

		private void MapPolicyProps(Dictionary<string, object> attributes)
		{
			var props = attributes;
			var fetchMode = _agent.GetSingle("com.sun.identity.agents.config.profile.attribute.fetch.mode");
			var mapStrs = _agent.GetConfig()["com.sun.identity.agents.config.profile.attribute.mapping"] as HashSet<string>;
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
						_app.Context.Request.ServerVariables[vals[1]] = Convert.ToString(props[key]);
					}
					else if(fetchMode == "HTTP_COOKIE")
					{
						_app.Context.Request.Cookies.Set(new HttpCookie(vals[1], Convert.ToString(props[key])));
					}
				}
			}

			_app.Context.Items["am_auth_cookie"] = _agent.GetAuthCookie(_app.Context.Request);
		}

		private string CheckUrl()
		{
			var enabled = _agent.GetSingle("com.sun.identity.agents.config.override.host") == "true";
			var url = _agent.GetSingle("com.sun.identity.agents.config.fqdn.default");
			if(enabled && !string.IsNullOrWhiteSpace(url) 
				&& !_app.Context.Request.Url.Host.Equals(url, StringComparison.InvariantCultureIgnoreCase))
			{
				var agentUrlStr = _agent.GetSingle("com.sun.identity.agents.config.agenturi.prefix"); 
				var rewriteProtocol = _agent.GetSingle("com.sun.identity.agents.config.override.protocol") == "true";
				var rewritePort = _agent.GetSingle("com.sun.identity.agents.config.override.port") == "true";
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

			var uid = session.token.property[_agent.GetSingle("com.sun.identity.agents.config.userid.param")];
			return uid;
		}

		private Session GetUserSession()
		{
			return Session.getSession(_agent, _app.Context.Request);
		}

		public void Dispose() { }
	}
}
