using System;

namespace ru.org.openam.sdk
{
	public class Session
    {
		private readonly Agent agent;

		private static readonly Cache _cache = new Cache();

		internal Cache PolicyCache {get; private set;}

        public String sessionId;

        public session.Response token;
		
        public Session(string sessionId)
        {
			PolicyCache = new Cache();
            token = Get(new session.Request(sessionId));
            this.sessionId = sessionId;
        }	 
        
        private Session(Agent agent, string authCookie)
            : this(authCookie)
        {
            this.agent = agent;
        }

        public static Session getSession(Agent agent, string authCookie)
        {
			if (authCookie == null)
			{
				return null;
			}

			var minsStr = agent.GetSingle("com.sun.identity.agents.config.policy.cache.polling.interval");
			int mins;
			if (!int.TryParse(minsStr, out mins))
			{
				mins = 1;
			}

			var userSession = _cache.GetOrDefault
			(
				"am_" + authCookie,
				() => new Session(agent,authCookie),
				mins
				// всегда приходит 0
				//, r =>
				//{
				//	if (r != null && r.token != null)
				//	{
				//		return r.token.maxcaching;
				//	}

				//	return 3;
				//}
			);
            return userSession;
        }

        public void Validate() 
        {
            token = Get(new session.Request(this));
        }

        public bool isValid()
        {
            try
            {
                Validate();
                return true;
            }
            catch (session.SessionException)
            {
                return false;
            }
        }

        public naming.Response GetNaming() //for personal session naming (need agent only)
        {
            return agent==null?Bootstrap.GetNaming():agent.GetNaming();
        }

        public session.Response Get(session.Request request)
        {
            pll.ResponseSet responses = RPC.GetXML(GetNaming(), new pll.RequestSet(new [] { request }));
            if (responses.Count > 0)
                return (session.Response)responses[0];
            return new session.Response();
        }

        override public String ToString()
        {
            return "Session: " + sessionId;
        }

        public String GetProperty(String key, String value)
        {
            String res = GetProperty(key);
            return res ?? value;
        }
        public String GetProperty(String key)
        {
            Validate();
            return token.property[key];
        }
    }
}
