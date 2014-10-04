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
        Policy(Agent agent, Session session, Uri uri, Dictionary<String, HashSet<String>> extra)
            : this(agent)
        {
            result=Get(new policy.Request(agent,session,uri,extra)); //TODO cache
        }

        policy.Response Get(policy.Request request)
        {
            pll.ResponseSet responses = RPC.GetXML(agent.GetNaming(), new pll.RequestSet(new policy.Request[] { request }));
            if (responses.Count > 0)
                return (policy.Response)responses[0];
            return new policy.Response();
        }

        public static Policy Get(Agent agent, Session session, Uri uri, Dictionary<String, HashSet<String>> extra)
        {
            return new Policy(agent,  session,  uri,  extra);
        }
    }
}
