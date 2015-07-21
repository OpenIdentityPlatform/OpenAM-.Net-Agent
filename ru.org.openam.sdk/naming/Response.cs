using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;

namespace ru.org.openam.sdk.naming
{
    //xml version="1.0" encoding="UTF-8" standalone="yes"?><ResponseSet vers="1.0" svcid="com.iplanet.am.naming" reqid="1"><Response><![CDATA[<NamingResponse vers="1.0" reqid="1">
    //<GetNamingProfile>
    //<Attribute name="iplanet-am-naming-session-class" value="com.iplanet.dpro.session.service.SessionRequestHandler"></Attribute>
    //<Attribute name="iplanet-am-naming-samlsoapreceiver-url" value="%protocol://%host:%port%uri/SAMLSOAPReceiver"></Attribute>
    //<Attribute name="02" value="http://login.staging.rapidsoft.ru:80/auth"></Attribute>
    //<Attribute name="iplanet-am-naming-auth-url" value="%protocol://%host:%port%uri/authservice"></Attribute>
    //<Attribute name="sun-naming-idsvcs-rest-url" value="%protocol://%host:%port%uri/identity/"></Attribute>
    //</GetNamingProfile>
    //</NamingResponse>]]></Response></ResponseSet>

    public class Response:pll.Response
    {
        public Response()
            : base()
        {
        }
        public Dictionary<string, string> property=new Dictionary<string,string>();

		public Response(CookieContainer cookieContainer,XmlNode element)
			: base(cookieContainer,element)
        {
            foreach (XmlNode node in element.ChildNodes)
            {
                if (node.LocalName.Equals("Attribute"))
                    property.Add(node.Attributes["name"].Value, node.Attributes["value"].Value);
                else if (node.LocalName.Equals("Exception"))
                    throw new NamingException(node.InnerText);
                else
                    throw new Exception("unknown node type=" + node.LocalName);
            }
        }
    }
}
