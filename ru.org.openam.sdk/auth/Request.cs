using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using ru.org.openam.sdk.auth.callback;

namespace ru.org.openam.sdk.auth
{
    public enum indexType 
    {
        Unknown,
        moduleInstance,
        service
	};

    public class Request: pll.Request
    {
        public String realm = "/";
        public indexType indexType = indexType.Unknown;
        public String IndexName = "";
        public String authIdentifier = "0";
        public List<callback.Callback> callbacks = new List<callback.Callback>();

        public Request()
            :base()
        {
            svcid = pll.type.auth;
        }

        public Request(String realm, indexType indexType, String IndexName)
            :this()
        {
            this.realm = realm;
            this.indexType = indexType;
            this.IndexName = IndexName;
        }

        public Request(Response response)
            :this()
        {
            authIdentifier = response.authIdentifier;
            callbacks = response.callbacks;
        }

		override public Uri getUrl()
		{
			return new Uri(GetNaming().property["iplanet-am-naming-auth-url"].Replace("%protocol://%host:%port%uri", Bootstrap.getUrl().ToString().Replace("/namingservice", "")));
		}

        override public String ToString()
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = new UTF8Encoding();
            XmlWriter writer = XmlWriter.Create(sb, settings);
            writer.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"");
            writer.WriteStartElement("AuthContext");
            writer.WriteAttributeString("version", "1.0");
                writer.WriteStartElement("Request");
                writer.WriteAttributeString("authIdentifier", authIdentifier);
                if (callbacks.Count == 0)
                {
                    writer.WriteStartElement("Login");
                    writer.WriteAttributeString("orgName", realm);
                        writer.WriteStartElement("IndexTypeNamePair");
                        writer.WriteAttributeString("indexType", indexType.ToString());
                        writer.WriteElementString("IndexName", IndexName);
                        writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                else
                {
                    writer.WriteStartElement("SubmitRequirements");
                        writer.WriteStartElement("Callbacks");
                        writer.WriteAttributeString("length", callbacks.Count.ToString());
                        foreach (Callback callback in callbacks)
                            writer.WriteRaw(callback.ToString());
                        writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            writer.WriteEndElement();
            writer.Close();
            return sb.ToString();
        }

    }
}
