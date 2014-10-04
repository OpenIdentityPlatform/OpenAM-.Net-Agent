using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ru.org.openam.sdk
{
    public class Naming
    {
        public static naming.Response Get(naming.Request request)
        {
            pll.ResponseSet responses = RPC.GetXML(Bootstrap.GetNaming(), new pll.RequestSet(new naming.Request[] { request }));
            if (responses.Count > 0)
                return (naming.Response)responses[0];
            return new naming.Response();
        }
    }
}
