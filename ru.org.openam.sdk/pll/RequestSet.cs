using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.pll
{
    class RequestSet: List<Request>
    {
        static int reqid = 1;

        public int id = reqid++;
        
        public RequestSet()
            : base()
        {
        }

        public RequestSet(IEnumerable<Request> requests)
            : this()
        {
            AddRange(requests);
        }

        public type svcid
        {
            get{
                if (Count > 0)
                    return this[0].svcid;
                return type.unknown;
            }
        }

        override public String ToString()
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = new UTF8Encoding();
            XmlWriter writer = XmlWriter.Create(sb, settings);
            writer.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"");
            writer.WriteStartElement("RequestSet");
            writer.WriteAttributeString("vers", "1.0");
            writer.WriteAttributeString("svcid", svcid.ToString());
            writer.WriteAttributeString("reqid", id.ToString());
            foreach (Request req in this)
            {
                writer.WriteStartElement("Request");
                writer.WriteCData(req.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.Close();
            return sb.ToString();
        }
    }
}
