using System;
using System.Net;
using ru.org.openam.sdk.session;
using System.Web;

namespace ru.org.openam.sdk
{
	public class Session
    {
		private readonly Agent agent=null;

		private static readonly Cache _cache = new Cache();

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
		private Session(session.Response token){
			this.token = token;
		}

		public Session(auth.Response authResponse)
		{
			token = (session.Response)new session.Request(authResponse).getResponse();
		}

		public static void invalidate(String sid)
		{
			_cache.Remove(sid);
		}

		private static Session getSessionFromCache(String sid){
			return _cache.Get<Session> (sid);
		}

		public static Session getSession(Agent agent, Session oldSession){
			if (oldSession == null || oldSession.token==null)
				return null;
			Session session=getSessionFromCache(oldSession.token.sid);
			if (session != null)
				return session;
			try {
				session.Response token =(session.Response)new session.Request(oldSession).getResponse() ;
				if (token == null)
					return null;
				return new Session (token);
			} catch (SessionException) {
				return null;
			}
		}

		public static Session getSession(Agent agent, HttpCookieCollection cookies)
		{
			if (cookies == null)
				return null;
			
			String sid = agent.GetAuthCookieValue (cookies);
			if (String.IsNullOrEmpty(sid))
				return null;
			
			Session userSession=getSessionFromCache(sid);
			if (userSession == null) 
				lock (sid){
					userSession=getSessionFromCache(sid);
					try {
						session.Response token = (session.Response)new session.Request (sid, cookies).getResponse ();
						if (token != null)
							userSession = new Session (token);
					} catch (SessionException) {
						return null;
					} catch (Exception) {
						return null;
					}
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

        override public String ToString()
        {
			return "Session: " + token;
        }

        public String GetProperty(String key, String defaultValue)
        {
            String res = GetProperty(key);
			return res ?? defaultValue;
        }
        public String GetProperty(String key)
        {
			if (token == null)
				return null;
            return token.property[key];
        }
    }
}
