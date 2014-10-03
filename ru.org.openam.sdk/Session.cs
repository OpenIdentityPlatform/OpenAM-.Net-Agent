using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ru.org.openam.sdk
{
	public class Session
    {
        public String sessionId;
        public session.Response token;

        public Session(String sessionId)
        {
            this.sessionId = sessionId;
            Validate();
        }

        public Session(Agent agent, System.Web.HttpRequest request)
            : this((request.Cookies[agent.GetCookieName()] != null) ? request.Cookies[agent.GetCookieName()].Value : null)
        {
        }

        public void Validate() 
        {
            //TODO max cache time 
            token = Get(new session.Request(sessionId));
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

        public static session.Response Get(session.Request request)
        {
            pll.ResponseSet responses = RPC.Get(new pll.RequestSet(new session.Request[] { request }));
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
