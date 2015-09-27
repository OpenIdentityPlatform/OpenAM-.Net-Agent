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
namespace ru.org.openam.iis
{
	public class iis7AgentModule : BaseHttpModule
	{
		public iis7AgentModule()
		{
			_agent = new Agent();
		}

		// конструктор для тестов
		public iis7AgentModule(Agent agent)
		{
			_agent = agent;
		}

		private readonly Agent _agent;

		public override void OnEndRequest(HttpContextBase context)
		{
			Log.Trace("End request");
		}

		public override void OnBeginRequest(HttpContextBase context)
		{	
			try
			{
				if(context == null || context.Request == null || context.Request.Url == null)
					throw new ArgumentException("context or context.Request or context.Request.Url is null");
				Log.Trace(string.Format("Begin request url: {0} ip: {1}", context.Request.Url.AbsoluteUri, GetUserIp(context.Request)));
			}
			catch (Exception ex)
			{
				Log.Fatal(ex);
				if(context == null || context.Request == null || context.Request.IsLocal)
					throw;
				else
				{
					context.Response.StatusCode = 500;
					CompleteRequest(context);	
				}
			}
		}

		public override void OnAuthentication(HttpContextBase context)
		{	  
			try
			{
				if(context == null || context.Request == null || context.Request.Url == null)
					throw new ArgumentException("context or context.Request or context.Request.Url is null");

				HttpRequestBase request = context.Request;
				HttpResponseBase response= context.Response;

				Uri url = context.Request.Url;
				String Host=url.Host; //save original Host

				//com.sun.identity.agents.config.override.*
				//com.sun.identity.agents.config.agenturi.prefix
				Uri agentURI=null;
				try{
					agentURI=new Uri(_agent.GetSingle("com.sun.identity.agents.config.agenturi.prefix"));
				}catch(Exception){}

				if (agentURI!=null){
					if (_agent.GetSingle("com.sun.identity.agents.config.override.protocol") == "true")
						url=new Uri(url.ToString().Replace(url.Scheme+"://",agentURI.Scheme+"://"));
					
					if (_agent.GetSingle("com.sun.identity.agents.config.override.host") == "true")
						url=new Uri(url.ToString().Replace("://"+url.Host,"://"+agentURI.Host));

					if (_agent.GetSingle("com.sun.identity.agents.config.override.port") == "true"){
						url=new Uri(url.ToString().Replace(url.Host+"/",				url.Host+":"+agentURI.Port+"/"));
						url=new Uri(url.ToString().Replace(url.Host+":"+url.Port+"/",	url.Host+":"+agentURI.Port+"/"));
					}
				}

				//com.sun.identity.agents.config.fqdn.check.enable
				//com.sun.identity.agents.config.fqdn.default
				String fqdnDefault=_agent.GetSingle("com.sun.identity.agents.config.fqdn.default");
				if(	!string.IsNullOrWhiteSpace(fqdnDefault)
					&& !Host.Equals(fqdnDefault)
					&& _agent.GetSingle("com.sun.identity.agents.config.fqdn.check.enable") == "true" 
				)
				{
					Uri nUrl=new Uri(url.ToString().Replace("://"+url.Host,"://"+fqdnDefault));
					Log.AuditTrace(string.Format("Request {0} was redirected to {1}",  url.AbsoluteUri, nUrl));
					Redirect(nUrl.AbsoluteUri, context);
					return;
				}

				if(IsLogOff(url))
				{
					Log.AuditTrace(string.Format("Logoff {0}", url.AbsoluteUri));

					ResetCookie("com.sun.identity.agents.config.logout.cookie.reset", response);

					var logoutUrl = _agent.GetFirst("com.sun.identity.agents.config.logout.redirect.url");
					if(logoutUrl == null)
						logoutUrl = _agent.GetFirst("com.sun.identity.agents.config.login.url");
					if(!string.IsNullOrWhiteSpace(logoutUrl))
						Redirect(logoutUrl, context);
					else
						throw new InvalidOperationException("com.sun.identity.agents.config.logout.redirect.url and com.sun.identity.agents.config.login.url cannot be empty");
					return;
				}

				if (IsFree(url))
				{
					if(_agent.GetSingle("com.sun.identity.agents.config.notenforced.url.attributes.enable") == "true")
					{
						var us = GetUserSession(request);
						context.User = GetUser(us);
					}
					else
						context.User = GetAnonymous();
					Log.AuditTrace(string.Format("Free access allowed to {0}", url.AbsoluteUri));
					return;
				}	 
				
				var session = GetUserSession(request);
				var user = GetUser(session);
				var autorized = false;
				if(user != null)
				{
					if(_agent.GetSingle("com.sun.identity.agents.config.sso.only") != "true")
					{
						var policy = Policy.Get(_agent, session, url, null, GetAttrsNames());
						if(policy != null && policy.result != null && policy.result.isAllow(context.Request.HttpMethod))
						{
							MapPolicyProps(policy.result.attributes, context);
							autorized = true;
							Log.AuditTrace(string.Format("User {0} was autorized to {1}", user.Identity.Name, url.AbsoluteUri));
						}
					}
					else
					{
						Log.AuditTrace(string.Format("User {0} was not autorized to {1}", user.Identity.Name, url.AbsoluteUri));
						autorized = true;
					}
				}

			    if (session != null && IsInvalidIp(session, request))
			        autorized = false;

			    if (user != null && autorized)
				{	
					context.User = user;
					MapArrtsProps(session, context);
					Log.Audit(string.Format("User {0} was allowed access to {1}", user.Identity.Name, url.AbsoluteUri));
				}
				else if(_agent.GetSingle("com.sun.identity.agents.config.anonymous.user.enable") == "true")
				{
					context.User = GetAnonymous();
					Log.AuditTrace(string.Format("Anonymous access allowed to {0}", url.AbsoluteUri));
				}
				else
				{
					ResetCookie("com.sun.identity.agents.config.cookie.reset", response);

					string userId = null;
					if(user != null)
						userId = user.Identity.Name;
					var status = user == null ? 401 : 403;
					Log.Audit(string.Format("User {0} was denied access to {1} ({2})", userId, url.AbsoluteUri, status));
					LogOff(user == null, url, context);
				}
			}
			catch (Exception ex)
			{
				Log.Fatal(ex);
				if(context == null || context.Request == null || context.Request.IsLocal)
					throw;
				else
				{
					context.Response.StatusCode = 500;
					CompleteRequest(context);	
				}				
			}
		}

		private void LogOff(bool isNotAuth, Uri url, HttpContextBase context)
		{
			var logoffUrl = GetLogoffUrl(isNotAuth ? "com.sun.identity.agents.config.login.url" : "com.sun.identity.agents.config.access.denied.url", url);
			if(logoffUrl != null)
				Redirect(logoffUrl, context);
			else
			{
				context.Response.StatusCode = isNotAuth ? 401 : 403;
				CompleteRequest(context);
			}
		}

		private bool IsInvalidIp(Session session, HttpRequestBase request)
		{
			if(_agent.GetSingle("com.sun.identity.agents.config.client.ip.validation.enable") != "true")
				return false;

			var props = session.token.property;
			if(!props.ContainsKey("Host"))
				return false;
			var host = props["Host"];
			var userIp = GetUserIp(request);
			if(host == userIp)
				return false;
									   
			Log.AuditTrace(string.Format("User {0} ip change detected, last: {1} current: {2}", GetUserId(session), host, userIp));
			return true;
		}

		private string GetUserIp(HttpRequestBase request){
			var headerName = _agent.GetSingle("com.sun.identity.agents.config.client.ip.header");
			var userIp = request.UserHostAddress;
			if(!string.IsNullOrWhiteSpace(headerName))
			{
				if(headerName.StartsWith("HTTP_"))
				{
					// без каунта не вернет
					var c = request.ServerVariables.Count;
					userIp = request.ServerVariables[headerName];
				}
				else
					userIp = request.Headers[headerName];
			}

			if(userIp != null && userIp.Contains(","))
				userIp = userIp.Substring(0, userIp.IndexOf(",", StringComparison.Ordinal));

			return userIp;
		}

		private ICollection<string> GetAttrsNames()
		{
			var attrs = _agent.GetArray("com.sun.identity.agents.config.profile.attribute.mapping");
			var list = new List<string>();
			foreach (var attr in attrs)
			{
				var vals = attr.Split('=');
				if(vals.Length != 2)
					continue;

				var key = vals[0].Substring(1);
				key = key.Substring(0, key.Length-1);
				list.Add(key);
			}

			return list;
		}

		private void ResetCookie(string cfg, HttpResponseBase response)
		{
			var resetCookie = _agent.GetOrderedArray(cfg);
			foreach (var cookie in resetCookie)
				response.AddHeader("Set-Cookie", cookie);
		}

		private bool IsLogOff(Uri url)
		{
			var logOffUrls = _agent.GetOrderedArray("com.sun.identity.agents.config.agent.logout.url");
			foreach (var u in logOffUrls)
				if(new Uri(u).AbsoluteUri == url.AbsoluteUri)
					return true;
			return false;
		}	

		private string GetLogoffUrl(string urlProp, Uri url)
		{	 
			var u = _agent.GetFirst(urlProp);
			if (u != null)
			{ 
				var gotoName = _agent.GetSingle("com.sun.identity.agents.config.redirect.param");
				if(!string.IsNullOrWhiteSpace(gotoName))
				{
					if(u.Contains("?"))
						u += "&";
					else
						u += "?";
					u += gotoName + "=" + HttpUtility.UrlPathEncode(url.AbsoluteUri);
				}

				return u;
			}
			return null;
		}

		private GenericPrincipal GetUser(Session session)
		{
			var userId = GetUserId(session);
			if(session == null || userId == null)
				return null;

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

		private bool IsFree(Uri url)
		{
			var freeUrls = _agent.GetOrderedArray("com.sun.identity.agents.config.notenforced.url");
			foreach (var u in freeUrls)
			{
				//TODO regexp !!!!!
				if(u.EndsWith("*") && url.OriginalString.StartsWith(u.Substring(0, u.Length-1), StringComparison.InvariantCultureIgnoreCase))
					return true;
				else if(url.OriginalString.Equals(u, StringComparison.InvariantCultureIgnoreCase))
					return true;
			}

			return false;
		}
		
		private void MapArrtsProps(Session session, HttpContextBase context)
		{
			var props = session.token.property;
			var mapStrs = _agent.GetArray("com.sun.identity.agents.config.session.attribute.mapping");
			var fetchMode = _agent.GetSingle("com.sun.identity.agents.config.session.attribute.fetch.mode");
			if(mapStrs == null)
				return;

			foreach (var mapStr in mapStrs)
			{
				var vals = mapStr.Split('=');
				if(vals.Length != 2)
					continue;

				var key = vals[0].Substring(1);
				key = key.Substring(0, key.Length-1);
				if(props.ContainsKey(key))
				{
					context.Items[vals[1]] = props[key];
					if(fetchMode == "HTTP_HEADER")
					{
						context.Request.ServerVariables[vals[1]] = props[key];
						var compName = "HTTP_" + vals[1].ToUpper().Replace("-", "_");
						context.Request.ServerVariables[compName] = Convert.ToString(props[key]);
					}
					else if(fetchMode == "HTTP_COOKIE")
						context.Request.Cookies.Set(new HttpCookie(vals[1], props[key]));
				}
			}

			context.Items["am_auth_cookie"] = _agent.GetAuthCookie(context.Request.Cookies);
		}

		private void MapPolicyProps(Dictionary<string, object> attributes, HttpContextBase context)
		{
			var props = attributes;
			var fetchMode = _agent.GetSingle("com.sun.identity.agents.config.profile.attribute.fetch.mode");
			var mapStrs = _agent.GetArray("com.sun.identity.agents.config.profile.attribute.mapping");
			if(mapStrs == null)
				return;

			foreach (var mapStr in mapStrs)
			{
				var vals = mapStr.Split('=');
				if(vals.Length != 2)
					continue;

				var key = vals[0].Substring(1);
				key = key.Substring(0, key.Length-1);
				if(props.ContainsKey(key))
				{
					context.Items[vals[1]] = props[key];
					if(fetchMode == "HTTP_HEADER")
					{
						context.Request.ServerVariables[vals[1]] = Convert.ToString(props[key]);
						var compName = "HTTP_" + vals[1].ToUpper().Replace("-", "_");
						context.Request.ServerVariables[compName] = Convert.ToString(props[key]);
					}
					else if(fetchMode == "HTTP_COOKIE")
						context.Request.Cookies.Set(new HttpCookie(vals[1], Convert.ToString(props[key])));
				}
			} 
		}

		private string GetUserId(Session session)
		{
			if (session == null || session.token == null)
				return null;

			var userIdParam = _agent.GetSingle("com.sun.identity.agents.config.userid.param");
			if(string.IsNullOrWhiteSpace(userIdParam))
				return null;

			var uid = session.token.property[userIdParam];
			return uid;
		}

		private Session GetUserSession(HttpRequestBase request)
		{
			try
			{
				return Session.getSession(_agent, _agent.GetAuthCookie(request.Cookies));
			}
			catch(SessionException ex)
			{
				Log.Warning(string.Format("SessionException was thrown {0}{1}", Environment.NewLine, ex));
				return null;
			}
		}

	}
}
