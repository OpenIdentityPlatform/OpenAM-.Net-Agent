using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.policy
{
//<PolicyRequest appSSOToken="AQIC5wM2LY4SfcxWElMOnHagBSKhRW9qUOOs3FisthRVZBw.*AAJTSQACMDQAAlNLAAk4NjcyNjI2NTMAAlMxAAIwMw..*" requestId="5">
//<GetResourceResults userSSOToken="AQIC5wM2LY4SfczdkH45R4CBR9MBxPBxrDxd_ozBZJ6ZTHE.*AAJTSQACMDQAAlNLAAotNTM1NDI1MDE4AAJTMQACMDM.*" serviceName="iPlanetAMWebAgentService" resourceName="http://sss.sss.ss:80/" resourceScope="self">
//<EnvParameters>
//<AttributeValuePair>
//<Attribute name="a"/>
//<Value>dddd</Value>
//</AttributeValuePair>
//</EnvParameters>
//</GetResourceResults>
//</PolicyRequest>
//</PolicyService>
    public enum type
    {
        iPlanetAMWebAgentService
    };

    public class Request: pll.Request
    {
        static int reqid = 1;

        public Request()
            : base()
        {
            svcid = pll.type.policy;
        }

        Agent agent;
        Session session;
        String resourceName;
        Dictionary<String, HashSet<String>> extra=new Dictionary<string,HashSet<string>>();

		public Request(Agent agent, Session session, Uri uri, Dictionary<String, HashSet<String>> EnvParameters)
            : this()
        {
            this.agent = agent;
            this.session = session;
            this.resourceName = uri.ToString(); //TODO 
            if (extra!=null)
                this.extra = extra;
        }

        override public String ToString()
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = new UTF8Encoding();
            XmlWriter writer = XmlWriter.Create(sb, settings);
            writer.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"");
            writer.WriteStartElement("PolicyService");
            writer.WriteAttributeString("version", "1.0");
            writer.WriteStartElement("PolicyRequest");
            writer.WriteAttributeString("requestId", (reqid++).ToString());
            writer.WriteAttributeString("appSSOToken", agent.getSession().sessionId);
                writer.WriteStartElement("GetResourceResults");
                writer.WriteAttributeString("userSSOToken", session.sessionId);
                writer.WriteAttributeString("serviceName", type.iPlanetAMWebAgentService.ToString());
                writer.WriteAttributeString("resourceName", resourceName);
                writer.WriteAttributeString("resourceScope", "self");
                writer.WriteValue("");
                if (extra != null && extra.Count > 0)
                {
                    writer.WriteStartElement("EnvParameters");
                    writer.WriteStartElement("AttributeValuePair");
                    foreach(String paramName in extra.Keys)
                    {
                        writer.WriteStartElement("Attribute");
                        writer.WriteAttributeString("name", paramName);
                        foreach (String value in extra[paramName])
                        {
                            writer.WriteStartElement("Value");
                                writer.WriteValue(value);
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.Close();
            return sb.ToString();
        }
    }
}
