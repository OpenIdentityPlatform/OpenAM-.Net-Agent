using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using System.Web;

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
        }

		private Request(String SessionID)
            : this()
        {
            this.SessionID = SessionID;
        }

        public Request(String SessionID, HttpCookieCollection cookies) : this(SessionID)
        {
            CookieContainer cookieContainer = getCookieContainer();
            for (int i = 0; i < cookies.Count; i++)
                cookieContainer.Add(new System.Net.Cookie(cookies[i].Name,cookies[i].Value) { Domain = getUrl().Host });
        }

		public Request(Session session): this(session.token.sid)
        {
			Log.Trace(string.Format("session {0} revalidate", SessionID));
			cookieContainer = session.token.cookieContainer;
            //replace LB cookie from old token
			if (session.token.property ["amlbcookie"] != null) {
				CookieCollection cc = cookieContainer.GetCookies (getUrl());
				foreach (Cookie co in cc)
					if (co.Name.Equals (Agent.Instance.GetLBCookieName ()) && !co.Value.Equals (session.token.property ["amlbcookie"])) {
						Log.Warning(string.Format("replace session {0} server {1}->{2}", SessionID,co.Value,session.token.property ["amlbcookie"]));
						co.Expired = true;
						cookieContainer.Add (new Cookie (Agent.Instance.GetLBCookieName (), session.token.property ["amlbcookie"]) { Domain = uri.Host });
					}
			}
        }

		public Request(auth.Response authResponse)
			: this(authResponse.ssoToken)
		{
			cookieContainer = authResponse.cookieContainer;
		}

		Uri uri=null;
		override public Uri getUrl()
		{
			if (uri == null)
				uri = new Uri (GetNaming ().property ["iplanet-am-naming-session-url"].Replace ("%protocol://%host:%port%uri", Bootstrap.getUrl ().ToString ().Replace ("/namingservice", "")));
			return uri;
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
