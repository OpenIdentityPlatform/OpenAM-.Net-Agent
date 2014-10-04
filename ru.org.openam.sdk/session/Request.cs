using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

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

        public Boolean reset = false;
        public String SessionID;

        public Request()
            : base()
        {
            svcid = pll.type.session;
        }

        public Request(String SessionID)
            : this()
        {
            this.SessionID = SessionID;
        }

        public Request(Session session)
            : this(session.sessionId)
        {
            //need cookie
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
    }
}
