using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.pll
{
    class ResponseSet:List<Response>
    {
        public String vers;
        public String svcid;
        public int reqid;

        public ResponseSet(XmlElement element)
            : base()
        {
            vers=element.Attributes["vers"].Value;
            svcid=element.Attributes["svcid"].Value;
            reqid=int.Parse(element.Attributes["reqid"].Value);
            foreach (XmlNode result in element.ChildNodes)
            {
                var response=new XmlDocument();
                response.LoadXml(result.FirstChild.Value);
                
                if (response.DocumentElement.Name.Equals("AuthContext"))
                    Add(new auth.Response(response.DocumentElement.FirstChild));
                else if (response.DocumentElement.Name.Equals("SessionResponse"))
                    Add(new session.Response(response.DocumentElement.FirstChild));
                else if (response.DocumentElement.Name.Equals("NamingResponse"))
                    Add(new naming.Response(response.DocumentElement.FirstChild));
                else
                    throw new Exception("unknown svcid=" + svcid);
            }
        }
    }
}
