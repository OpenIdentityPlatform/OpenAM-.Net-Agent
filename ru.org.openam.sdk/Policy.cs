using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ru.org.openam.sdk
{
    public class Policy
    {
        Agent agent;
        Policy(Agent agent)
        {
            this.agent = agent;
        }

        public policy.Response result;
		Policy(Agent agent, Session session, Uri uri, Dictionary<string, ISet<String>> extra,ICollection<string> attributes)
            : this(agent)
        {
			result=Get(new policy.Request(agent,session,uri,extra,attributes)); 
        }

        policy.Response Get(policy.Request request)
        {
            pll.ResponseSet responses = RPC.GetXML(agent.GetNaming(), new pll.RequestSet(new policy.Request[] { request }));
            if (responses.Count > 0)
                return (policy.Response)responses[0];
            return new policy.Response();
        }

		//TODO memorycache
		public static Policy Get(Agent agent, Session session, Uri uri, Dictionary<string, ISet<string>> extra,ICollection<string> attributes)
        {
			return new Policy(agent,  session,  uri,  extra, attributes);
        }
    }
}
