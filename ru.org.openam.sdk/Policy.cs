using System;
using System.Collections.Generic;

namespace ru.org.openam.sdk
{
	public class Policy
	{
		internal static Cache PolicyCache {get; private set;}=new Cache();


		public policy.Response result;
		Policy(Agent agent, Session session, Uri uri, Dictionary<string, ISet<String>> extra, ICollection<string> attributes)
		{
			var url = GetUrl(agent, uri);
			result = (policy.Response)new policy.Request (agent, session, url, extra, attributes).getResponse ();
		}


		public static Policy Get(Agent agent, Session session, Uri uri, Dictionary<string, ISet<string>> extra, ICollection<string> attributes)
		{
			var minsStr = agent.GetSingle("com.sun.identity.agents.config.policy.cache.polling.interval");
			int mins;
			if (!int.TryParse(minsStr, out mins))
				mins = 1;

			var policy = Policy.PolicyCache.GetOrDefault
			(
				GetUrl(agent, uri)+session.token.sid,
				() => new Policy(agent, session, uri, extra, attributes),
				mins
			);
			return policy;
		}

		private static string GetUrl(Agent agent, Uri uri)
		{
			string url;
			if (agent.GetSingle("com.sun.identity.agents.config.fetch.from.root.resource") == "true")
				url = uri.Scheme + "://" + uri.Host + ":" + uri.Port+"/";
			else if (agent.GetSingle("com.sun.identity.agents.config.ignore.path.info") == "true")
				url = uri.Scheme + "://" + uri.Host + ":" + uri.Port + (String.IsNullOrEmpty(uri.AbsolutePath)?"/": uri.AbsolutePath);
			else
				url = uri.OriginalString;
			return url;
		}
	}
}
