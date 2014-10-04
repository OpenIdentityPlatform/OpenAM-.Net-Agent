using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.auth.callback
{
	class PagePropertiesCallback : Callback
	{
		public Boolean isErrorState = false;
		public String ModuleName;
		public String HeaderValue;
		public String ImageName;
		public int PageTimeOutValue;
		public String TemplateName;
		public int PageState;
        public String AttributeList;
        public String RequiredList;
        public String InfoTextList;
		//<PagePropertiesCallback isErrorState="false"><ModuleName>LDAP</ModuleName><HeaderValue>This server uses LDAP Authentication</HeaderValue><ImageName></ImageName><PageTimeOutValue>1200</PageTimeOutValue><TemplateName>Login.jsp</TemplateName><PageState>1</PageState></PagePropertiesCallback>
		public PagePropertiesCallback(XmlNode element)
			: base(element)
		{
			isErrorState = Boolean.Parse(element.Attributes["isErrorState"].Value);
			foreach (XmlNode node in element.ChildNodes)
				if (node.LocalName.Equals("ModuleName"))
					ModuleName = node.InnerText;
				else if (node.LocalName.Equals("HeaderValue"))
					HeaderValue = node.InnerText;
				else if (node.LocalName.Equals("ImageName"))
					ImageName = node.InnerText;
				else if (node.LocalName.Equals("PageTimeOutValue"))
					PageTimeOutValue = int.Parse(node.InnerText);
				else if (node.LocalName.Equals("TemplateName"))
					TemplateName = node.InnerText;
				else if (node.LocalName.Equals("PageState"))
					PageState = int.Parse(node.InnerText);
                else if (node.LocalName.Equals("AttributeList"))
                    AttributeList = node.InnerText;
                else if (node.LocalName.Equals("RequiredList"))
                    RequiredList = node.InnerText;
                else if (node.LocalName.Equals("InfoTextList"))
                    InfoTextList = node.InnerText;
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
			writer.WriteStartElement("PagePropertiesCallback");
			writer.WriteAttributeString("isErrorState", isErrorState.ToString());
			writer.WriteElementString("ModuleName", ModuleName);
			writer.WriteElementString("HeaderValue", HeaderValue);
			writer.WriteElementString("ImageName", ImageName);
			writer.WriteElementString("PageTimeOutValue", PageTimeOutValue.ToString());
			writer.WriteElementString("TemplateName", TemplateName);
			writer.WriteElementString("PageState", PageState.ToString());
			writer.WriteEndElement();
			writer.Close();
			return sb.ToString();
		}
	}
}
