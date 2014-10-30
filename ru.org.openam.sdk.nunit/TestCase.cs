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
				Assert.IsNotNull(actual);
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
			Assert.IsTrue(personal.property.Count>0);
		}

		[Test ()]
		public void pll_GetTest()
		{
			ResponseSet actual = RPC.GetXML(
				Bootstrap.GetNaming(), 
				new RequestSet(new ru.org.openam.sdk.pll.Request[]{new auth.Request("/",auth.indexType.moduleInstance,"Application")}));
			Assert.IsNotNull(actual);
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
				new Session("000000000000000000000000000000");
				Assert.IsTrue(true);
			}
			catch (SessionException)
			{
				return;
			}
		}

		[Test ()]
		public void policy_404()
		{
			Policy policy=Policy.Get(
					new Agent(),
					Auth.login("/clients", auth.indexType.service, "ldap", new Callback[] { new NameCallback("11111111111"), new PasswordCallback("1111111111") }),
					new Uri("http://sssss:80/sdsd?sdsdsd"),
					null,
					null
				);
			Assert.IsFalse(policy.result.isAllow("GET"));
		}

		[Test ()]
		public void policy_403()
		{
			Policy policy=Policy.Get(
					new Agent(),
					Auth.login("/clients", auth.indexType.service, "ldap", new Callback[] { new NameCallback("11111111111"), new PasswordCallback("1111111111") }),
					new Uri("http://deny.rapidsoft.ru:80/sdsd?sss"),
					null,
					null
				);
			Assert.IsFalse(policy.result.isAllow("GET"));

			policy=Policy.Get(
				new Agent(),
				Auth.login("/clients", auth.indexType.service, "ldap", new Callback[] { new NameCallback("11111111111"), new PasswordCallback("1111111111") }),
				new Uri("http://localhost.rapidsoft.ru:80/403"),
				null,
				null
			);
			Assert.IsFalse(policy.result.isAllow("GET"));
		}

		[Test ()]
		public void policy_302()
		{
			Policy policy=Policy.Get(
					new Agent(),
					Auth.login("/clients", auth.indexType.service, "ldap", new Callback[] { new NameCallback("11111111111"), new PasswordCallback("1111111111") }),
					new Uri("http://advice.rapidsoft.ru:80/sdsd?sss"),
					null,
					null
				);
			Assert.IsFalse(policy.result.isAllow("GET"));
		}

		[Test ()]
		public void policy_200()
		{
			Policy policy=Policy.Get(
				new Agent (),
				Auth.login ("/clients", auth.indexType.service, "ldap", new Callback[] {
					new NameCallback ("11111111111"),
					new PasswordCallback ("1111111111")
				}),
				new Uri ("http://localhost.rapidsoft.ru:80/sdsd?sss"),
				null,
				new String[]{"uid","inetuserStatus","unknown","cn"});
			Assert.IsTrue(policy.result.isAllow("post"));
			Assert.IsTrue(policy.result.isAllow("head"));
			Assert.IsTrue (policy.result.attributes.Count > 0);

			policy=Policy.Get(
				new Agent (),
				Auth.login ("/clients", auth.indexType.service, "ldap", new Callback[] {
					new NameCallback ("11111111111"),
					new PasswordCallback ("1111111111")
				}),
				new Uri ("http://localhost.rapidsoft.ru:80/"),
				null,
				new String[]{"uid","inetuserStatus","unknown","cn"});
			Assert.IsTrue(policy.result.isAllow("post"));
		}
	}
}

