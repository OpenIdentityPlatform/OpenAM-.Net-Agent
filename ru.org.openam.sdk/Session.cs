using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ru.org.openam.sdk
{
    class Session
    {
        public String sessionId;
        public session.Response token;

        public Session(String sessionId)
        {
            this.sessionId = sessionId;
            Validate();
        }

        public Session(System.Web.HttpRequest request)
            :this((request.Cookies[Config.getCookieName()]!=null)?request.Cookies[Config.getCookieName()].Value:null)
        {
        }

        public void Validate() 
        {
            token = Get(new session.Request(sessionId));
        }

        public bool isValid()
        {
            try
            {
                Validate();
            }
            catch (session.SessionException)
            {
                return false;
            }
            return true;
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
    }
}
