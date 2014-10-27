using System;
using NUnit.Framework;
using ru.org.openam.sdk.auth;
using ru.org.openam.sdk.auth.callback;
using ru.org.openam.sdk.session;
using ru.org.openam.sdk.pll;

namespace ru.org.openam.sdk.nunit
{
	[TestFixture ()]
	public class TestCase
	{
		[Test ()]
		public void Agent_getSession()
		{
			new Agent().getSession();
		}

		[Test ()]
		public void login_good()
		{
			Session actual = Auth.login(
				"/", 
				auth.indexType.moduleInstance, 
				"Application", 
				new Callback[] { 
					new NameCallback(Bootstrap.getAppUser()), 
					new PasswordCallback(Bootstrap.getAppPassword())
				}
			);
			Assert.AreNotEqual(null, actual);
			Auth.login("/clients", auth.indexType.service, "ldap", new Callback[] { new NameCallback("11111111111"), new PasswordCallback("1111111111") });
		}

		[Test ()]
		public void login_bad()
		{
			try
			{
				Session actual = Auth.login("/", auth.indexType.moduleInstance, "Application", new Callback[] { new NameCallback(Bootstrap.getAppUser()), new PasswordCallback("xxxxxxxxxxxxx") });
			}
			catch (AuthException)
			{ 
			}
		}

		[Test ()]
		public void identity_GetConfig()
		{
			Assert.AreNotEqual(0,new Agent().GetConfig().Count);
		}

		[Test ()]
		public void naming_Get()
		{
			naming.Response global = Bootstrap.GetNaming();
			Assert.AreNotEqual(null, global);

			naming.Response personal = new Agent().GetNaming();
		}

		[Test ()]
		public void pll_GetTest()
		{
			ResponseSet actual = RPC.GetXML(
				Bootstrap.GetNaming(), 
				new RequestSet(new ru.org.openam.sdk.pll.Request[]{new auth.Request("/",auth.indexType.moduleInstance,"Application")}));
		}

		[Test ()]
		public void session_GetTest()
		{
			Session session= Auth.login("/", auth.indexType.moduleInstance, "Application", new Callback[] { new NameCallback(Bootstrap.getAppUser()), new PasswordCallback(Bootstrap.getAppPassword()) });
			Assert.AreNotEqual(null, session);
			Assert.IsTrue(session.isValid());
		}

		[Test ()]
		public void session_bad_GetTest()
		{
			try
			{
				Session Session = new Session("000000000000000000000000000000");
			}
			catch (SessionException)
			{
				return;
			}
		}

		[Test ()]
		public void policy_404()
		{
			Assert.IsFalse(
				Policy.Get(
					new Agent(),
					Auth.login("/clients", auth.indexType.service, "ldap", new Callback[] { new NameCallback("11111111111"), new PasswordCallback("1111111111") }),
					new Uri("http://sssss:80/sdsd?sdsdsd"),
					null
				).result.isAllow("GET")
			);
		}

		[Test ()]
		public void policy_403()
		{
			Assert.IsFalse(
				Policy.Get(
					new Agent(),
					Auth.login("/clients", auth.indexType.service, "ldap", new Callback[] { new NameCallback("11111111111"), new PasswordCallback("1111111111") }),
					new Uri("http://deny.rapidsoft.ru:80/sdsd?sss"),
					null
				).result.isAllow("GET")
			);
		}

		[Test ()]
		public void policy_302()
		{
			Assert.IsFalse(
				Policy.Get(
					new Agent(),
					Auth.login("/clients", auth.indexType.service, "ldap", new Callback[] { new NameCallback("11111111111"), new PasswordCallback("1111111111") }),
					new Uri("http://advice.rapidsoft.ru:80/sdsd?sss"),
					null
				).result.isAllow("GET")
			);
		}

		[Test ()]
		public void policy_200()
		{
			Assert.IsTrue(
				Policy.Get(
					new Agent(),
					Auth.login("/clients", auth.indexType.service, "ldap", new Callback[] { new NameCallback("11111111111"), new PasswordCallback("1111111111") }),
					new Uri("http://localhost.rapidsoft.ru:80/sdsd?sss"),
					null
				).result.isAllow("post")
			);
		}
	}
}

