using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.auth.callback
{
    public class NameCallback:Callback
    {
        public String Prompt = "";
        public String Value = "";

        public NameCallback()
        {
        }

        public NameCallback(String value)
            :this()
        {
            Value = value;
        }

        public NameCallback(XmlNode element)
            : base(element)
        {
            foreach (XmlNode node in element.ChildNodes)
                if (node.LocalName.Equals("Prompt"))
                    Prompt = node.InnerText;
                else if (node.LocalName.Equals("Value"))
                    Value = node.InnerText;
                else
                    throw new Exception("unknown element=" + node.LocalName);
        }

        override public String ToString()
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = new UTF8Encoding();
            settings.OmitXmlDeclaration = true;
            XmlWriter writer = XmlWriter.Create(sb, settings);
            writer.WriteStartElement("NameCallback");
                writer.WriteStartElement("Prompt");
                writer.WriteValue(Prompt);
                writer.WriteEndElement();
                writer.WriteStartElement("Value");
                writer.WriteValue(Value);
                writer.WriteEndElement();
            writer.WriteEndElement();
            writer.Close();
            return sb.ToString();
        }
    }
}
