using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using ru.org.openam.sdk.auth.callback;
using System.Reflection;
using System.Runtime.Caching;
using System.Security.Principal;

namespace ru.org.openam.sdk
{
	public class Agent
	{
		private static Agent _instance;
		public static Agent Instance 
		{ 
			get
			{
				if(_instance == null)
				{
					_instance = new Agent();
				}

				return _instance;
			}
		}

		public Agent()
		{
			_instance = this;
		}

		Session session = null;

		public Session getSession()
		{
			session=Session.getSession(this,session);	
			if (session == null || !session.isValid())
				lock (this)
				{
					session=Session.getSession(this,session);	
					if (session == null || !session.isValid()) //need re-auth ?
					{
						session = Auth.login(
							Bootstrap.getAppRealm(),
							auth.indexType.moduleInstance, "Application",
							new Callback[] { 
                                new NameCallback(Bootstrap.getAppUser()), 
                                new PasswordCallback(Bootstrap.getAppPassword()) 
                            }
						);
						naming = Naming.Get(new naming.Request(session));
						reread_config = true;//clear config
						config=GetConfig();
					}
				}
			return session;
		}

		naming.Response naming;
		public naming.Response GetNaming() //for personal session naming (need agent only)
		{
			if (naming == null)
				naming = Naming.Get(new naming.Request(getSession()));
			return naming;
		}

		public bool HasConfig()
		{
			return config != null;
		}

		//<identitydetails>
		//   <name value="test.domain.com" />
		//   <type value="agent" />
		//   <realm value="/" />
		//   <attribute name="com.sun.identity.agents.config.cdsso.enable"> 
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.cdsso.cookie.domain">
		//      <value>[0]=</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.get.client.host.name">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.profile.attribute.fetch.mode">
		//      <value>HTTP_HEADER</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.notenforced.ip">
		//      <value>[0]=</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.fqdn.check.enable">
		//      <value>true</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.cleanup.interval">
		//      <value>30</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.notenforced.url.attributes.enable">
		//      <value>true</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.ignore.preferred.naming.url">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.client.ip.header" />
		//   <attribute name="com.sun.identity.agents.config.session.attribute.mapping">
		//      <value>[MaxIdleTime]=profile-maxidletime</value>
		//      <value>[password.expired]=profile-password-expire-notify</value>
		//      <value>[ignoreOTP]=profile-ignore-otp-login</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.audit.accesstype">
		//      <value>LOG_DENY</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.proxy.override.host.port">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.load.balancer.enable">
		//      <value>true</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.encode.url.special.chars.enable">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.convert.mbyte.enable">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.domino.check.name.database">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.iis.owa.enable">
		//      <value>true</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.override.port">
		//      <value>true</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.policy.clock.skew">
		//      <value>600</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.iis.owa.enable.session.timeout.url">
		//      <value>https://ibank.staging.rapidsoft.ru/timeout.aspx</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.sso.only">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.fqdn.default">
		//      <value>ibank.staging.rapidsoft.ru</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.cookie.reset">
		//      <value>[0]=SvyaznoyBank.InternetBank.Site</value>
		//      <value>[2]=SvyaznoyBank.InternetBank.Site=;Domain=.ibank.staging.rapidsoft.ru</value>
		//      <value>[1]=SvyaznoyBank.InternetBank.Site=;Domain=.staging.rapidsoft.ru</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.domino.ltpa.config.name">
		//      <value>LtpaToken</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.domino.ltpa.cookie.name">
		//      <value>LtpaToken</value>
		//   </attribute>
		//   <attribute name="sunIdentityServerDeviceKeyValue">
		//      <value>agentRootURL=https://login.staging.rapidsoft.ru:443/</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.response.attribute.mapping">
		//      <value>[]=</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.userid.param.type">
		//      <value>session</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.url.comparison.case.ignore">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.profile.attribute.cookie.maxage">
		//      <value>300</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.domino.ltpa.enable">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.remote.logfile">
		//      <value>amAgent_login_staging_rapidsoft_ru_443.log</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.notenforced.url">
		//      <value>[4]=https://ibank.staging.rapidsoft.ru:443/favicon.ico?*</value>
		//      <value>[5]=https://ibank.staging.rapidsoft.ru/favicon.ico</value>
		//      <value>[3]=https://ibank.staging.rapidsoft.ru:443/favicon.ico</value>
		//      <value>[0]=https://ibank.staging.rapidsoft.ru:443/Scripts/*</value>
		//      <value>[6]=https://ibank.staging.rapidsoft.ru/favicon.ico?*</value>
		//      <value>[1]=https://ibank.staging.rapidsoft.ru:443/Content/*</value>
		//      <value>[7]=/favicon.ico?*</value>
		//      <value>[2]=https://ibank.stagind.rapidsoft.ru:443/Static/*</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.notification.enable">
		//      <value>true</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.logout.cookie.reset">
		//      <value>[0]=SvyaznoyBank.InternetBank.Site</value>
		//      <value>[2]=SvyaznoyBank.InternetBank.Site=;Domain=.ibank.staging.rapidsoft.ru</value>
		//      <value>[1]=SvyaznoyBank.InternetBank.Site=;Domain=.staging.rapidsoft.ru</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.profile.attribute.cookie.prefix">
		//      <value>HTTP_</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.polling.interval">
		//      <value>60</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.attribute.multi.value.separator">
		//      <value>|</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.debug.file.rotate">
		//      <value>true</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.debug.level">
		//      <value>All</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.local.log.rotate">
		//      <value>true</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.repository.location">
		//      <value>centralized</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.client.ip.validation.enable">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.override.protocol">
		//      <value>true</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.ignore.path.info">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.logout.redirect.url" />
		//   <attribute name="AgentType">
		//      <value>WebAgent</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.override.notification.url">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.session.attribute.fetch.mode">
		//      <value>HTTP_HEADER</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.policy.cache.polling.interval">
		//      <value>1</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.cdsso.cdcservlet.url">
		//      <value>[0]=http://login.staging.rapidsoft.ru:80/auth/cdcservlet</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.cookie.name">
		//      <value>svbid</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.profile.attribute.mapping">
		//      <value>[employeeNumber]=profile-clientid</value>
		//      <value>[telephoneNumber]=profile-phone</value>
		//      <value>[sn]=profile-type</value>
		//      <value>[sunIdentityServerPPLegalIdentityDOB]=profile-birthday</value>
		//      <value>[sunIdentityServerPPLegalIdentityGender]=profile-gender</value>
		//      <value>[inetUserStatus]=profile-status</value>
		//      <value>[uid]=profile-pkn</value>
		//      <value>[sunIdentityServerPPCommonNameFN]=profile-name-first</value>
		//      <value>[sunIdentityServerPPCommonNameSN]=profile-name-last</value>
		//      <value>[sunIdentityServerPPCommonNameMN]=profile-name-middle</value>
		//      <value>[mail]=profile-mail</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.iis.filter.priority">
		//      <value>HIGH</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.client.notification.url">
		//      <value>https://ibank.staging.rapidsoft.ru:443/UpdateAgentCacheServlet?shortcircuit=false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.iis.auth.type" />
		//   <attribute name="com.sun.identity.agents.config.cookie.secure">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.ignore.path.info.for.not.enforced.list">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.remote.log.interval">
		//      <value>5</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.notenforced.url.invert">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="universalid">
		//      <value>id=test.domain.com,ou=agent,dc=rapidsoft,dc=ru</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.replaypasswd.key" />
		//   <attribute name="com.sun.identity.agents.config.iis.owa.enable.change.protocol">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.userid.param">
		//      <value>UserToken</value>
		//   </attribute>
		//   <attribute name="userpassword">
		//      <value>{SHA-1}0kt0k7qfilEjDz9Qfq8VciZ61zw=</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.response.attribute.fetch.mode">
		//      <value>NONE</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.log.disposition">
		//      <value>LOCAL</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.freeformproperties" />
		//   <attribute name="com.sun.identity.agents.config.postdata.preserve.enable">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.agenturi.prefix">
		//      <value>https://ibank.staging.rapidsoft.ru:443/amagent</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.override.host">
		//      <value>true</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.cookie.reset.enable">
		//      <value>true</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.local.log.size">
		//      <value>52428800</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.access.denied.url" />
		//   <attribute name="com.sun.identity.agents.config.debug.file.size">
		//      <value>10000000</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.change.notification.enable">
		//      <value>true</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.anonymous.user.enable">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.agent.logout.url">
		//      <value>[0]=https://ibank.staging.rapidsoft.ru:443/LogOn/LogOff</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.domino.ltpa.org.name" />
		//   <attribute name="com.sun.identity.agents.config.poll.primary.server">
		//      <value>1</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.fqdn.mapping">
		//      <value>[]=</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.auth.connection.timeout">
		//      <value>5</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.client.hostname.header" />
		//   <attribute name="com.sun.identity.agents.config.ignore.server.check">
		//      <value>true</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.fetch.from.root.resource">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.login.url">
		//      <value>[0]=http://localhost.rapidsoft.ru:8080/auth/UI/Login?service=qbank</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.redirect.param">
		//      <value>goto</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.logout.url">
		//      <value>[0]=https://login.staging.rapidsoft.ru:444/auth/UI/Logout</value>
		//   </attribute>
		//   <attribute name="sunIdentityServerDeviceStatus">
		//      <value>Active</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.sso.cache.polling.interval">
		//      <value>1</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.anonymous.user.id">
		//      <value>anonymous</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.encode.cookie.special.chars.enable">
		//      <value>false</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.locale">
		//      <value>en_US</value>
		//   </attribute>
		//   <attribute name="com.sun.identity.agents.config.postcache.entry.lifetime">
		//      <value>10</value>
		//   </attribute>
		//</identitydetails>

		Dictionary<String, Object> config;
		Boolean reread_config=true;
		public Dictionary<String, Object> GetConfig()
		{
			if (config == null || reread_config){
				config =
					((identity.Response)new identity.Request(
						getSession().GetProperty("UserId"),
						new String[] {
							"realm",
							"objecttype"
						},
						new KeyValuePair<string, string>[]{
							new KeyValuePair<string,string>("realm",Bootstrap.getAppRealm()),
							new KeyValuePair<string,string>("objecttype","Agent")
						},
						getSession()
					).getResponse()).property;
				reread_config = false;
				Log.Init();
			}
			return config;
		}

		// виртуальный для тестов
		public virtual string GetSingle(string name) 
		{
			if(!GetConfig().ContainsKey(name))
				return ConfigurationManager.AppSettings[name] as string; //try local

			var opt = GetConfig()[name];
			
			return opt as string;
		}

		// виртуальный для тестов
		public virtual string GetFirst(string name) 
		{
			if(!GetConfig().ContainsKey(name))
				return ConfigurationManager.AppSettings[name] as string; //try local

			var hs = GetOrderedArray(name);
			return hs.FirstOrDefault();	
		}
		
		private readonly Regex _numRegex = new Regex(@"\[(\d+)\]=", RegexOptions.Compiled);
		// виртуальный для тестов
		public virtual string[] GetOrderedArray(string name) 
		{
			if(!GetConfig().ContainsKey(name))
			{
				return new string[0];
			}
			var opt = GetConfig()[name];
			if(opt is string)
			{
				return new []{_numRegex.Replace((string)opt, "")};
			}
			else if(opt is HashSet<string>)
			{
				var hs = (HashSet<string>)opt;
				var res = hs.OrderBy(o => {
					var m = _numRegex.Match(o);
					if(m.Success)
					{
						return int.Parse(m.Groups[1].Value);
					}
					return 0;
				}).Select(o => _numRegex.Replace(o, "")).ToArray();
				return res;
			}

			return new string[0];
		}

		// виртуальный для тестов
		public virtual string[] GetArray(string name) 
		{
			if(!GetConfig().ContainsKey(name))
			{
				return new string[0];
			}
			var opt = GetConfig()[name];
			if(opt is string)
			{
				return new []{(string)opt};
			}
			else if(opt is HashSet<string>)
			{
				return ((HashSet<string>)opt).ToArray();
			}

			return new string[0];
		}

		public static string GetCookieName()
		{
			String res=null;
			if (_instance!=null && _instance.HasConfig())
				res=(String)Instance.config["com.sun.identity.agents.config.cookie.name"];
			return res == null?"null":res;
		}


		const string AM_LB_COOKIE_NAME="com.iplanet.am.lbcookie.name";

		public string GetLBCookieName()
		{
			String res = null;
			if (HasConfig()&&config.ContainsKey(AM_LB_COOKIE_NAME))
				res=(string)config[AM_LB_COOKIE_NAME];
			if (res==null)
				res=ConfigurationManager.AppSettings[AM_LB_COOKIE_NAME];
			return (res==null)?"amlbcookie":res;
		}

		public string GetAuthCookieValue(HttpCookieCollection cookies)
		{
			if (!HasConfig ())
				GetConfig ();
			return GetCookie(cookies,GetCookieName());
		}	

		public string GetLBCookieValue(HttpCookieCollection cookies)
		{
			if (!HasConfig ())
				GetConfig ();
			return GetCookie(cookies,GetLBCookieName());
		}	

		public string GetCookie(HttpCookieCollection cookies,String name)
		{
			var cookie = cookies.Get(name);
			if (cookie == null || string.IsNullOrWhiteSpace(cookie.Value))
				return null;
			return cookie.Value;
		}	

		static String Version=((AssemblyInformationalVersionAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).FirstOrDefault()).InformationalVersion;
		public static String getVersion(){
			return Version;
		}

		// com.sun.identity.agents.config.polling.interval
		// com.sun.identity.agents.config.cleanup.interval
		public static int getConfigPoolingInterval(){
			int res = 10;
			int.TryParse (Instance.GetSingle ("com.sun.identity.agents.config.polling.interval"), out res);
			return Math.Min (60, Math.Max(1,res));
		}

		private readonly MemoryCache freeCache=new MemoryCache("com.sun.identity.agents.config.notenforced.url");
		public bool isNotenforced(Uri url)
		{
			var result = freeCache.Get(url.ToString());
			if (result == null) 
				lock(url){
					result = freeCache.Get(url.ToString());
					if (result == null) {
						result = false;
						var freeUrls = GetOrderedArray ("com.sun.identity.agents.config.notenforced.url");
						foreach (var u in freeUrls) 
							try{
								if (!string.IsNullOrWhiteSpace (u) && new Regex (Regex.Escape (u).Replace (@"\*", ".*").Replace (@"\?", "."), RegexOptions.IgnoreCase).IsMatch (url.ToString ())) {
									result = true;
									break;
								}
							}catch(Exception e){
								Log.Fatal(string.Format(" {0} regexp error: {1}", u,e));
							}
						if ("true".Equals (GetSingle ("com.sun.identity.agents.config.notenforced.url.invert")))
							result = !(bool)result;
						freeCache.Set(url.ToString(),result,DateTime.Now.AddMinutes(getConfigPoolingInterval()));
					}
				}
			Log.Trace(string.Format(" {0} isNotenforced: {1}", url,result));
			return (bool)result;
		}

		//com.sun.identity.agents.config.iis.auth.type
		//User ID can be fetched from either SESSION and LDAP attributes. (property name: com.sun.identity.agents.config.userid.param.type) 
		//Agent sets value of User Id to REMOTE_USER server variable. (property name: com.sun.identity.agents.config.userid.param) 
		//User id of unauthenticated users. (property name: com.sun.identity.agents.config.anonymous.user.id) 
		//Enable/Disable REMOTE_USER processing for anonymous users. (property name: com.sun.identity.agents.config.anonymous.user.enable) 
		public GenericPrincipal GetPrincipal(Session session,Policy policy){
			string type = GetSingle("com.sun.identity.agents.config.userid.param.type");
			string param = GetSingle("com.sun.identity.agents.config.userid.param");
			string authtype = GetSingle("com.sun.identity.agents.config.iis.auth.type");
			string userid = "LDAP".Equals (type) ? (param==null||policy==null||policy.result==null)?null:Convert.ToString(policy.result.attributes [param]) : (param==null||session==null)?null:session.token.property[param];
			if (userid == null && "true".Equals (GetSingle ("com.sun.identity.agents.config.anonymous.user.enable"))) {
				userid = GetSingle ("com.sun.identity.agents.config.anonymous.user.id");
				if (userid == null)
					userid = "";
			}
			return (userid==null) ? null : new GenericPrincipal(new GenericIdentity(userid,string.IsNullOrWhiteSpace(authtype)?"OpenAM":authtype), new string[0]);
		}
	}
}
