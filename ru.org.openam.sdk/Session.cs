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
            token = Get(new session.Request(sessionId));
            this.sessionId = sessionId;
        }

        Agent agent;
        Session(Agent agent, System.Web.HttpRequest request)
            : this((request.Cookies[agent.GetCookieName()] != null) ? request.Cookies[agent.GetCookieName()].Value : null)
        {
            this.agent = agent;
        }

        //static Dictionary<String, Session> sessions;
        public static Session getSession(Agent agent, System.Web.HttpRequest request)
        {
            return new Session(agent,request);
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
