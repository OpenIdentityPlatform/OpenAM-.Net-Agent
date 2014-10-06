using System;
using System.Collections.Generic;
using System.Xml;

namespace ru.org.openam.sdk.identity
{
	public class Response:pll.Response
    {
        public Response()
            : base()
        {
        }

        //<identitydetails><name value="test.domain.com" /><type value="agent" /><realm value="/" />
        //<attribute name="com.sun.identity.agents.config.cdsso.enable"><value>false</value></attribute>
        //<attribute name="com.sun.identity.agents.config.cdsso.cookie.domain"><value>[0]=</value></attribute>
        //<attribute name="com.sun.identity.agents.config.get.client.host.name"><value>false</value></attribute>
        //<attribute name="com.sun.identity.agents.config.profile.attribute.fetch.mode"><value>HTTP_HEADER</value>
        //</attribute><attribute name="com.sun.identity.agents.config.notenforced.ip"><value>[0]=</value></attribute><attribute 

        public String name;
        public String type;
        public String realm;
        public Dictionary<string, object> property = new Dictionary<string, object>();

        public Response(XmlElement element)
            : base(element)
        {
            foreach (XmlNode node in element.ChildNodes)
            {
                if (node.LocalName.Equals("name"))
                {
                    foreach (XmlAttribute attr in node.Attributes)
                        if (attr.LocalName.Equals("value"))
                            name = attr.Value;
                        else
                            throw new Exception("unknown node type=" + attr.LocalName);
                }
                else if (node.LocalName.Equals("type"))
                {
                    foreach (XmlAttribute attr in node.Attributes)
                        if (attr.LocalName.Equals("value"))
                            type = attr.Value;
                        else
                            throw new Exception("unknown node type=" + attr.LocalName);
                }
                else if (node.LocalName.Equals("realm"))
                {
                    foreach (XmlAttribute attr in node.Attributes)
                        if (attr.LocalName.Equals("value"))
                            realm = attr.Value;
                        else
                            throw new Exception("unknown node type=" + attr.LocalName);
                }
                else if (node.LocalName.Equals("attribute"))
                {
                    foreach (XmlAttribute attr in node.Attributes)
                        if (attr.LocalName.Equals("name"))
                        {
                            type = attr.Value;
                            foreach (XmlNode node2 in node.ChildNodes)
                                if (node2.LocalName.Equals("value") && !string.IsNullOrWhiteSpace(node2.InnerXml))
                                {	 
									if(property.ContainsKey(attr.Value))
									{
										var value = property[attr.Value];
										var set = value as HashSet<string>;
										if (set != null)
										{
                                            set.Add(node2.InnerXml);
										}
										else //String to HashSet
                                        {
                                            property.Remove(attr.Value);
											property.Add(attr.Value, new HashSet<String>(new []{(string)value, node2.InnerXml}));
                                        }	
									}
									else
									{
                                        property.Add(attr.Value, node2.InnerXml);
									}
                                }
                                else
                                    throw new Exception("unknown node type=" + node2.LocalName);
                        }
                        else
                            throw new Exception("unknown node type=" + attr.LocalName);
                }
                else if (node.LocalName.Equals("Exception"))
                    throw new IdentityException(node.InnerText);
                else
                    throw new Exception("unknown node type=" + node.LocalName);
            }
        }
    }
}
