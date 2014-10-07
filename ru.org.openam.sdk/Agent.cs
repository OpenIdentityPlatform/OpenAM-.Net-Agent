using System;
using System.Collections.Generic;
using ru.org.openam.sdk.auth.callback;

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

		Session session;
		public Session getSession()
		{
			if (session == null || !session.isValid())
				lock (this)
				{
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
						naming = null; //clear naming
						config = null;//clear config
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
		public Dictionary<String, Object> GetConfig()
		{
			if (config == null){
				config =
					((identity.Response)RPC.GetXML(
						GetNaming(),
						new identity.Request(
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
						)
					 )).property;
				Log.InitLog();
			}
			return config;
		}

		public string GetCookieName()
		{
			return (string)GetConfig()["com.sun.identity.agents.config.cookie.name"];
		}									 
	}
}
