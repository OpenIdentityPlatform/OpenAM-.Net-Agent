using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;

namespace ru.org.openam.sdk.session
{
//    Received RequestSet XML :
//<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
//<RequestSet vers="1.0" svcid="session" reqid="10">
//<Request><![CDATA[<SessionRequest vers="1.0" reqid="6">
//<GetSession reset="false">
//<SessionID>AQIC5wM2LY4SfczOQuxkowQlw48pKNNIWE1brT4UHBlxXVE.*AAJTSQACMDM.*</SessionID>
//</GetSession>
//</SessionRequest>]]></Request>
//</RequestSet>
    public class Request: pll.Request
    {
        static int reqid = 1;

        public Boolean reset = true;
        public String SessionID;

        public Request()
            : base()
        {
            svcid = pll.type.session;
			uri=getUrl();
        }

        public Request(String SessionID)
            : this()
        {
            this.SessionID = SessionID;
        }

		Uri uri=null;

        public Request(Session session)
            : this(session.sessionId)
        {
			cookieContainer = session.token.cookieContainer;
            //save LB cookie
			if (session.token.property ["amlbcookie"] != null) {
				CookieCollection cc = cookieContainer.GetCookies (uri);
				foreach (Cookie co in cc)
					if (co.Name.Equals("amlbcookie"))
						co.Expired = true;
				cookieContainer.Add (new Cookie ("amlbcookie", session.token.property ["amlbcookie"]) { Domain = uri.Host });
			}
        }

		override public Uri getUrl()
		{
			return new Uri(GetNaming().property["iplanet-am-naming-session-url"].Replace("%protocol://%host:%port%uri", Bootstrap.getUrl().ToString().Replace("/namingservice", "")));
		}

        override public String ToString()
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = new UTF8Encoding();
            XmlWriter writer = XmlWriter.Create(sb, settings);
            writer.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"");
            writer.WriteStartElement("SessionRequest");
            writer.WriteAttributeString("version", "1.0");
            writer.WriteAttributeString("reqid", (reqid++).ToString());
                writer.WriteStartElement("GetSession");
                writer.WriteAttributeString("reset", reset.ToString());
                    writer.WriteElementString("SessionID", SessionID);
                writer.WriteEndElement();
            writer.WriteEndElement();
            writer.Close();
            return sb.ToString();
        }

		override public CookieContainer getCookieContainer(){
			CookieContainer cookieContainer= base.getCookieContainer();
			String cn = Agent.GetCookieName ();
			if (!cn.Equals ("null")) {
				CookieCollection cc = cookieContainer.GetCookies (uri);
				foreach (Cookie co in cc)
					if (co.Name.Equals ("null")||co.Name.Equals ("AMAuthCookie"))
						co.Expired = true;
			}
			cookieContainer.Add(new Cookie(cn, SessionID) { Domain = getUrl().Host });
			return cookieContainer;
		}
    }
}
