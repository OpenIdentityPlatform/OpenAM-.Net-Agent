using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.policy
{
	public class Response:pll.Response
    {
        public Response()
            : base()
        {
        }

//<PolicyResponse requestId="5" issueInstant="1412458936797" >
//<ResourceResult name="http://ssss:80/">
//<PolicyDecision>
//<ResponseAttributes>
//<AttributeValuePair>
//<Attribute name="uid"/>
//<Value>9175955155</Value>
//</AttributeValuePair>
//<AttributeValuePair>
//<Attribute name="employeeType"/>
//<Value>Person</Value>
//</AttributeValuePair>
//<AttributeValuePair>
//<Attribute name="balance"/>
//<Value>72.048533</Value>
//</AttributeValuePair>
//<AttributeValuePair>
//<Attribute name="sn"/>
//</AttributeValuePair>
//<AttributeValuePair>
//<Attribute name="bonus"/>
//<Value>7661</Value>
//</AttributeValuePair>
//<AttributeValuePair>
//<Attribute name="displayName"/>
//<Value>Харсеко Валерий Валерьевич</Value>
//</AttributeValuePair>
//<AttributeValuePair>
//<Attribute name="businessCategory"/>
//<Value>ПРОСТО для своих</Value>
//</AttributeValuePair>
//<AttributeValuePair>
//<Attribute name="time-balance-actual"/>
//<Value>20141005014213+0400</Value>
//</AttributeValuePair>
//</ResponseAttributes>
//<ActionDecision timeToLive="9223372036854775807">
//<AttributeValuePair>
//<Attribute name="POST"/>
//<Value>allow</Value>
//</AttributeValuePair>
//<Advices>
//</Advices>
//</ActionDecision>
//<ActionDecision timeToLive="9223372036854775807">
//<AttributeValuePair>
//<Attribute name="GET"/>
//<Value>allow</Value>
//</AttributeValuePair>
//<Advices>
//</Advices>
//</ActionDecision>
//</PolicyDecision>
//</ResourceResult>
//</PolicyResponse>
//</PolicyService>

		Uri url=null;
		Dictionary<string, object> ResponseAttributes=new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, object> ActionDecision=new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, object> Advices=new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);

		public bool isAllow(String method){
			Object Decision;
			return 
				ActionDecision.TryGetValue (method != null && "GET".Equals (method.ToUpper()) ? "GET" : "POST", out Decision)
				&& Decision != null 
				&& (	(Decision is String && "allow".Equals (Decision)) 
					|| 	(Decision is HashSet<String> && ((HashSet<String>)Decision).Contains("allow") && !((HashSet<String>)Decision).Contains("deny"))
				); 
		}

		public Dictionary<string, object> attributes
		{
			get { return ResponseAttributes; }
		}

        public Response(XmlNode element)
            : base()
        {
            //authIdentifier = element.Attributes["authIdentifier"].Value;
            foreach (XmlNode node in element.ChildNodes)
            {
				if (node.LocalName.Equals("ResourceResult"))
                {
                    foreach (XmlAttribute attr in node.Attributes)
                        if (attr.LocalName.Equals("name"))
							url = new Uri(attr.Value);
                        else
                            throw new Exception("unknown node type=" + attr.LocalName);
					foreach (XmlNode node2 in node.ChildNodes)
						if (node2.LocalName.Equals ("PolicyDecision"))
							foreach (XmlNode node3 in node2.ChildNodes)
							{
								if (node3.LocalName.Equals ("ResponseAttributes")) {
									bool hasAttributeValuePair = false;
									foreach (XmlNode node4 in node3.ChildNodes)
										if (node4.LocalName.Equals ("AttributeValuePair"))
											hasAttributeValuePair = true;
										else
											throw new Exception("unknown node type=" + node4.LocalName);
									if (hasAttributeValuePair)
										processAttributeValuePair(ResponseAttributes,node3);
								}
								else if (node3.LocalName.Equals ("ResponseDecisions")) {
									bool hasAttributeValuePair = false;
									foreach (XmlNode node4 in node3.ChildNodes)
										if (node4.LocalName.Equals ("AttributeValuePair"))
											hasAttributeValuePair = true;
										else
											throw new Exception("unknown node type=" + node4.LocalName);
									if (hasAttributeValuePair)
										processAttributeValuePair(ResponseAttributes,node3);
								}
								else if (node3.LocalName.Equals ("ActionDecision")) 
								{
									foreach (XmlAttribute attr in node3.Attributes)
										if (attr.LocalName.Equals("timeToLive"))
											continue;
										else
											throw new Exception("unknown node type=" + attr.LocalName);
									bool hasAttributeValuePair = false;
									foreach (XmlNode node4 in node3.ChildNodes){
										bool hasAttributeValuePair2 = false;
										if (node4.LocalName.Equals ("AttributeValuePair"))
											hasAttributeValuePair = true;
										else if (node4.LocalName.Equals ("Advices"))
										{
											foreach (XmlNode node5 in node4.ChildNodes)
												if (node5.LocalName.Equals ("AttributeValuePair"))
													hasAttributeValuePair2 = true;
												else
													throw new Exception("unknown node type=" + node5.LocalName);
											if (hasAttributeValuePair2)
												processAttributeValuePair(Advices,node4);
										}
										else
											throw new Exception("unknown node type=" + node4.LocalName);
									}
									if (hasAttributeValuePair)
										processAttributeValuePair(ActionDecision,node3);
								}
								else
									throw new Exception("unknown node type=" + node3.LocalName);
							}
						else
                            throw new Exception("unknown node type=" + node2.LocalName);
                }
                else if (node.LocalName.Equals("Exception"))
                    throw new PolicyException(node.InnerText);
                else
                    throw new Exception("unknown node type=" + node.LocalName);
            }
        }

		void processAttributeValuePair(Dictionary<string, object> res,XmlNode root)
		{
			foreach (XmlNode node in root.ChildNodes)
				if (node.LocalName.Equals ("AttributeValuePair"))
				{
					String name = null;
					Object value = null;
					foreach (XmlNode node2 in node.ChildNodes)
					{
						if (node2.LocalName.Equals ("Attribute"))
							foreach (XmlAttribute attr in node2.Attributes)
								if (attr.LocalName.Equals ("name"))
									name = attr.Value;
								else
									throw new Exception ("unknown node type=" + attr.LocalName);
						else if (node2.LocalName.Equals ("Value")) {
							if (value == null)
								value = node2.InnerXml;
							else if (value is String)
								value = new HashSet<String> (new []{ (string)value, node2.InnerXml });
							else
								((HashSet<String>)value).Add(node.InnerXml);
						}else
							throw new Exception ("unknown node type=" + node2.LocalName);
					}
					if (name != null && value != null && !res.ContainsKey(name)) 
						res.Add (name, value);
				}
		}
    }
}
