using System;
using System.Collections.Specialized;

namespace ru.org.openam.sdk
{
	public class ConfigurationManager 
	{
		static NameValueCollection settings=System.Configuration.ConfigurationManager.AppSettings;
		public static NameValueCollection AppSettings
		{
			get
			{
				return settings;
			}
		}
	}
}

