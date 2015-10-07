using System;
using System.Net;
using ru.org.openam.sdk.session;

namespace ru.org.openam.sdk
{
	public class Session
    {
		private readonly Agent agent;

		private static readonly Cache _cache = new Cache();

		internal Cache PolicyCache {get; private set;}=new Cache();

        public String sessionId;

        session.Response _token;
		public session.Response token
		{
			get { return _token; }
			set { 
				this._token = value; 
				//allow cache token maxcaching 1..3 minutes
				_cache.Set (value.sid,this,Math.Max(1,Math.Min((int)value.maxcaching,3)));
			}
		}

        public Session(string sessionId)
        {
			try{
            	token = Get(new session.Request(sessionId));
			}catch(SessionException){
				//cache session not found 1 min
				_token=null;
				_cache.Set(sessionId,this,1);
			}
            this.sessionId = sessionId;
        }

		public Session(auth.Response authResponse)
		{
			session.Request request = new session.Request(authResponse.ssoToken);
			request.cookieContainer = authResponse.cookieContainer;
			//remove AMAuthCookie after auth
			CookieCollection cc = request.cookieContainer.GetCookies (request.getUrl());
			foreach (Cookie co in cc)
				if (co.Name.Equals ("AMAuthCookie"))
					co.Expired = true;
			token = Get(request);
			this.sessionId = token.sid;
		}

        private Session(Agent agent, string authCookie)
            : this(authCookie)
        {
            this.agent = agent;
        }

        public static Session getSession(Agent agent, string authCookie)
        {
			if (authCookie == null)
				return null;

			Session userSession=_cache.Get<Session>(authCookie);
			if (userSession == null) 
				lock (authCookie){
					userSession=_cache.Get<Session>(authCookie);
					if (userSession == null) 
						userSession = new Session (agent, authCookie);
				}
            return userSession;
        }

        public bool isValid()
        {
			return token != null && state.valid.Equals (token.state);
        }

        public naming.Response GetNaming() //for personal session naming (need agent only)
        {
            return agent==null?Bootstrap.GetNaming():agent.GetNaming();
        }

        public session.Response Get(session.Request request)
        {
			return (session.Response)request.getResponse();
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
            //Validate();
            return token.property[key];
        }
    }
}
