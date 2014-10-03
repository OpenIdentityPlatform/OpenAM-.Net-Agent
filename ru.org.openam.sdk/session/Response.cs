using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.session
{
    enum state
    {
        valid,
        destroyed
    }
    class Response:pll.Response
    {
        public Response()
            : base()
        {
        }

        //<GetSession>
        //<Session sid="AQIC5wM2LY4SfcykzPvJaL_XIcF8gIZuF7PY5z7thlEfxDg.*AAJTSQACMDM.*" stype="user" cid="ws.api" cdomain="dc=rapidsoft,dc=ru" maxtime="153722867280912930" maxidle="153722867280912930" maxcaching="153722867280912930" timeidle="0" timeleft="153722867280912930" state="valid">
        //<Property name="CharSet" value="UTF-8"></Property>
        //<Property name="UserId" value="ws.api"></Property>
        //<Property name="successURL" value="/auth/console"></Property>
        //<Property name="cookieSupport" value="true"></Property>
        //<Property name="AuthLevel" value="0"></Property>
        //<Property name="SessionHandle" value="shandle:AQIC5wM2LY4SfczB45FyL5jLga4-OZfpm40RHPjWFLY__TQ.*AAJTSQACMDM.*"></Property>
        //<Property name="UserToken" value="ws.api"></Property>
        //<Property name="IndexType" value="module_instance"></Property>
        //<Property name="Principals" value="ws.api"></Property>
        //<Property name="sun.am.UniversalIdentifier" value="id=ws.api,ou=agent,dc=rapidsoft,dc=ru"></Property>
        //<Property name="amlbcookie" value="03"></Property>
        //<Property name="Organization" value="dc=rapidsoft,dc=ru"></Property>
        //<Property name="Locale" value="en_US"></Property>
        //<Property name="HostName" value="192.168.1.206"></Property>
        //<Property name="AuthType" value="Application"></Property>
        //<Property name="Host" value="192.168.1.206"></Property>
        //<Property name="UserProfile" value="Required"></Property>
        //<Property name="AMCtxId" value="9df3e4dc5d4d349f03"></Property>
        //<Property name="clientType" value="genericHTML"></Property>
        //<Property name="authInstant" value="2012-04-05T15:46:50Z"></Property>
        //<Property name="Principal" value="id=ws.api,ou=agent,dc=rapidsoft,dc=ru"></Property>
        //</Session></GetSession>

        public String sid;
        public String stype;
        public String cid;
        public String cdomain;
        public long maxtime;
        public long maxidle;
        public long maxcaching;
        public long timeidle;
        public long timeleft;
        public state state;
        public Dictionary<string, string> property=new Dictionary<string,string>();

        public Response(XmlNode element)
            : base()
        {
            //authIdentifier = element.Attributes["authIdentifier"].Value;
            foreach (XmlNode node in element.ChildNodes)
            {
                if (node.LocalName.Equals("Session"))
                {
                    foreach (XmlAttribute attr in node.Attributes)
                        if (attr.LocalName.Equals("sid"))
                            sid = attr.Value;
                        else if (attr.LocalName.Equals("stype"))
                            stype = attr.Value;
                        else if (attr.LocalName.Equals("cid"))
                            cid = attr.Value;
                        else if (attr.LocalName.Equals("cdomain"))
                            cdomain = attr.Value;
                        else if (attr.LocalName.Equals("maxtime"))
                            maxtime = long.Parse(attr.Value);
                        else if (attr.LocalName.Equals("maxidle"))
                            maxidle = long.Parse(attr.Value);
                        else if (attr.LocalName.Equals("maxcaching"))
                            maxcaching = long.Parse(attr.Value);
                        else if (attr.LocalName.Equals("timeidle"))
                            timeidle = long.Parse(attr.Value);
                        else if (attr.LocalName.Equals("timeleft"))
                            timeleft = long.Parse(attr.Value);
                        else if (attr.LocalName.Equals("state"))
                            state = (state)Enum.Parse(typeof(state), attr.Value);
                        else
                            throw new Exception("unknown node type=" + attr.LocalName);
                    foreach (XmlNode node2 in node.ChildNodes)
                        if (node2.LocalName.Equals("Property"))
                            property.Add(node2.Attributes["name"].Value, node2.Attributes["value"].Value);
                        else
                            throw new Exception("unknown node type=" + node2.LocalName);
                }
                else if (node.LocalName.Equals("Exception"))
                    throw new SessionException(node.InnerText);
                else
                    throw new Exception("unknown node type=" + node.LocalName);
            }
        }
    }
}
