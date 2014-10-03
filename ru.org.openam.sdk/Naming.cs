using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ru.org.openam.sdk
{
    class Naming
    {
        public String sessionId;
        
        public Naming(String sessionId)
        {
            this.sessionId = sessionId;
        }

        public static naming.Response Get(naming.Request request)
        {
            pll.ResponseSet responses = RPC.Get(new pll.RequestSet(new naming.Request[] { request }));
            if (responses.Count > 0)
                return (naming.Response)responses[0];
            return new naming.Response();
        }

        override public String ToString()
        {
            return sessionId;
        }

        static naming.Response global = Get(new naming.Request());
        public static naming.Response Get()
        {
            return global;
        }
    }
}
