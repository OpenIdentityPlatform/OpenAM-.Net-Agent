using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;

namespace ru.org.openam.sdk.pll
{
    public class ResponseSet:List<Response>
    {
        public String vers;
        public String svcid;
        public int reqid;

		public ResponseSet(CookieContainer cookieContainer, String data) 
            : base()
        {
			XmlDocument xml = new XmlDocument ();
			xml.LoadXml(data);
			XmlElement element = xml.DocumentElement;
            vers=element.Attributes["vers"].Value;
            svcid=element.Attributes["svcid"].Value;
            reqid=int.Parse(element.Attributes["reqid"].Value);
            foreach (XmlNode result in element.ChildNodes)
            {
				XmlDocument response=new XmlDocument();
                response.LoadXml(result.FirstChild.Value);
                
                if (response.DocumentElement.Name.Equals("AuthContext"))
					Add(new auth.Response(cookieContainer, response.DocumentElement.FirstChild));
                else if (response.DocumentElement.Name.Equals("SessionResponse"))
					Add(new session.Response(cookieContainer, response.DocumentElement.FirstChild));
                else if (response.DocumentElement.Name.Equals("NamingResponse"))
					Add(new naming.Response(cookieContainer, response.DocumentElement.FirstChild));
                else if (response.DocumentElement.Name.Equals("PolicyService"))
					Add(new policy.Response(cookieContainer, response.DocumentElement.FirstChild));
                else
                    throw new Exception("unknown svcid=" + svcid);
            }
        }
    }
}
