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
using System.Reflection;
using System.Collections.Specialized;
using System.Collections;
using System.Runtime.Caching;


namespace ru.org.openam.iis
{
	public class OpenAMHttpModule : BaseHttpModule
	{
		private static Agent _agent= Agent.Instance;

		public OpenAMHttpModule()
		{
		}

		// конструктор для тестов
		public OpenAMHttpModule(Agent agent)
		{
			_agent = agent;
		}

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
				Log.Error(ex);
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
					Log.Trace(string.Format("Request {0} was redirected to {1}",  url, nUrl));
					Redirect(nUrl.AbsoluteUri, context);
					return;
				}

				if(IsLogOff(url))
				{
					Log.Trace(string.Format("Logoff {0}", url.AbsoluteUri));

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

				Session session=null;
				String ip=GetUserIp(request);
				Policy policy=null;
				bool isNotenforced=_agent.isNotenforced(url);

				//read user attr from server
				if (!(isNotenforced && !"true".Equals(_agent.GetSingle("com.sun.identity.agents.config.notenforced.url.attributes.enable")))){
					session=Session.getSession(_agent, request.Cookies);
					if (session!=null)
						MapAttrsProps(session, context);
					if (session!=null && GetAttrsNames().Count>0){ //read policy only for attr
						try {
							policy = Policy.Get (_agent, session, url, null, GetAttrsNames ());
						} catch (sdk.policy.PolicyException ) {
							if (session.token!=null)
								Session.invalidate (session.token.sid);
							session = null;
						}
						if(policy != null && policy.result != null)
							MapPolicyProps(policy.result.attributes, context);
					}
				}

				//get principal from session and policy attrs
				GenericPrincipal user=_agent.GetPrincipal(session,policy);

				//com.sun.identity.agents.config.client.ip.validation.enable
				if(session!=null && !ip.Equals(session.token.property["Host"]) && "true".Equals(_agent.GetSingle("com.sun.identity.agents.config.client.ip.validation.enable"))){
					Log.Audit(false,string.Format("DENY IP CHANGE {0}->{1} {2} {3} {4}",session.token.property["Host"],ip, GetName(user) ,url,HttpCookieCollection2String(request.Cookies)));
					session=null;
					policy=null;
					user=null;

				}

				//set principal 
				if (user!=null)
					context.User=user;
				
				//check need read policy
				if (session!=null && policy==null && !isNotenforced && !"true".Equals(_agent.GetSingle("com.sun.identity.agents.config.sso.only")))
					policy = Policy.Get(_agent, session, url, null, GetAttrsNames());

				//authoried ?
				if (isNotenforced){
					Log.Audit(true,string.Format("ALLOW NOTENFORCED {0} {1} {2} {3}",ip,GetName(user)==null?"anonymouse":GetName(user),url,HttpCookieCollection2String(request.Cookies)));
					return;
				}else if (user!=null && "true".Equals(_agent.GetSingle("com.sun.identity.agents.config.sso.only"))){ 
					Log.Audit(true,string.Format("ALLOW SSO {0} {1} {2} {3}",ip,GetName(user) ,url,HttpCookieCollection2String(request.Cookies)));
					return;
				}else if (policy != null && policy.result != null && policy.result.isAllow(context.Request.HttpMethod)){
					Log.Audit(true,string.Format("ALLOW POLICY {0} {1} {2} {3}",ip,GetName(user) ,url,HttpCookieCollection2String(request.Cookies)));
					return;
				}

				//access denied
				Log.Audit(false,string.Format("DENY {0} {1} {2} ({3}) {4}",ip, (GetName(user)==null?"anonymouse":GetName(user)),url,(user == null ? 401 : 403),HttpCookieCollection2String(request.Cookies)));
				ResetCookie("com.sun.identity.agents.config.cookie.reset", response);
				LogOff(user == null, url, context);
			}
			catch (Exception ex)
			{
				Log.Error(ex);
				if(context == null || context.Request == null || context.Request.IsLocal)
					throw;
				else
				{
					context.Response.StatusCode = 500;
					CompleteRequest(context);	
				}				
			}
		}

		String HttpCookieCollection2String(HttpCookieCollection cookies){
			if (cookies == null)
				return null;
			String res = "";
			foreach (String cookie in cookies) 
				res += string.Format ("{0}={1} ", cookie, cookies.Get(cookie).Value);
			return res;
		}

		string GetName(IPrincipal principal){
			return (principal == null || principal.Identity == null) ? null : principal.Identity.Name;
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

		private string GetUserIp(HttpRequestBase request){
			String headerName = _agent.GetSingle("com.sun.identity.agents.config.client.ip.header");
			String userIp = null;
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
			if(string.IsNullOrWhiteSpace(userIp))
				userIp = request.UserHostAddress;
				
			if(userIp != null && userIp.Contains(","))
				userIp = userIp.Split(',')[0];

			return (userIp==null)?"":userIp;
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
			string[] logOffUrls = _agent.GetOrderedArray("com.sun.identity.agents.config.agent.logout.url");
			foreach (var u in logOffUrls)
				try{
					if(new Uri(u).AbsoluteUri == url.AbsoluteUri)
						return true;
				}catch{
				}
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

		private void MapAttrsProps(Session session, HttpContextBase context)
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
						setROCollection(context.Request.Headers,vals[1],props[key]);
						//setROCollection2(context.Request.ServerVariables,vals[1],props[key]);
						//setROCollection2(context.Request.ServerVariables,"HTTP_" + vals[1].ToUpper().Replace("-", "_"),props[key]);
					}
					else if(fetchMode == "HTTP_COOKIE")
						context.Request.Cookies.Set(new HttpCookie(vals[1], props[key]));
				}
			}

			context.Items["am_auth_cookie"] = _agent.GetAuthCookieValue(context.Request.Cookies);
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
						setROCollection(context.Request.Headers,vals[1],Convert.ToString(props[key]));
						//setROCollection2(context.Request.ServerVariables,vals[1],Convert.ToString(props[key]));
						//setROCollection2(context.Request.ServerVariables,"HTTP_" + vals[1].ToUpper().Replace("-", "_"),Convert.ToString(props[key]));
					}
					else if(fetchMode == "HTTP_COOKIE")
						context.Request.Cookies.Set(new HttpCookie(vals[1], Convert.ToString(props[key])));
				}
			} 
		}

		void setROCollection(NameValueCollection collection,String name,String value){
			if (collection == null)
				return;
			Type hdr = collection.GetType();
			PropertyInfo ro = hdr.GetProperty("IsReadOnly",BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
			// Remove the ReadOnly property
			ro.SetValue(collection, false, null);
			// Invoke the protected InvalidateCachedArrays method 
			hdr.InvokeMember("InvalidateCachedArrays",BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,null, collection, null);
			// Now invoke the protected "BaseAdd" method of the base class to add the
			// headers you need. The header content needs to be an ArrayList or the
			// the web application will choke on it.
			hdr.InvokeMember("BaseAdd",BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,null, collection, new object[] { name, new ArrayList {value}} );
			// repeat BaseAdd invocation for any other headers to be added
			// Then set the collection back to ReadOnly
			//ro.SetValue(collection, true, null);
		}
	}
}
