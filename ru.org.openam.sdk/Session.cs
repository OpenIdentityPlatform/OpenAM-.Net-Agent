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
        
        private Session(Agent agent, System.Web.HttpRequest request)
            : this(agent.GetAuthCookie(request))
        {
            this.agent = agent;
        }

        public static Session getSession(Agent agent, System.Web.HttpRequest request)
        {
			var auth = agent.GetAuthCookie(request);
			if (auth == null)
			{
				return null;
			}

			var userSession = _cache.GetOrDefault
			(
				"am_" + auth,
				() => new Session(agent,request)
				, r =>
				{
					if (r != null && r.token != null)
					{
						return r.token.maxcaching;
					}

					return 0;
				}
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
            pll.ResponseSet responses = RPC.GetXML(GetNaming(), new pll.RequestSet(new session.Request[] { request }));
            if (responses.Count > 0)
                return (session.Response)responses[0];
            return new session.Response();
        }

        override public String ToString()
        {
            return sessionId;
        }

        public String GetProperty(String key, String value)
        {
            String res = GetProperty(key);
            return res == null ? value : res;
        }
        public String GetProperty(String key)
        {
            Validate();
            return token.property[key];
        }
    }
}
