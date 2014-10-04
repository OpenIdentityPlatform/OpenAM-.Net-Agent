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
        public Response(XmlNode element)
            : base()
        {
            //authIdentifier = element.Attributes["authIdentifier"].Value;
            foreach (XmlNode node in element.ChildNodes)
            {
                if (node.LocalName.Equals("PolicyResponse"))
                {
                    foreach (XmlAttribute attr in node.Attributes)
                        //if (attr.LocalName.Equals("sid"))
                        //    sid = attr.Value;
                        //else if (attr.LocalName.Equals("stype"))
                        //    stype = attr.Value;
                        //else if (attr.LocalName.Equals("cid"))
                        //    cid = attr.Value;
                        //else if (attr.LocalName.Equals("cdomain"))
                        //    cdomain = attr.Value;
                        //else if (attr.LocalName.Equals("maxtime"))
                        //    maxtime = long.Parse(attr.Value);
                        //else if (attr.LocalName.Equals("maxidle"))
                        //    maxidle = long.Parse(attr.Value);
                        //else if (attr.LocalName.Equals("maxcaching"))
                        //    maxcaching = long.Parse(attr.Value);
                        //else if (attr.LocalName.Equals("timeidle"))
                        //    timeidle = long.Parse(attr.Value);
                        //else if (attr.LocalName.Equals("timeleft"))
                        //    timeleft = long.Parse(attr.Value);
                        //else if (attr.LocalName.Equals("state"))
                        //    state = (state)Enum.Parse(typeof(state), attr.Value);
                        //else
                            throw new Exception("unknown node type=" + attr.LocalName);
                    foreach (XmlNode node2 in node.ChildNodes)
                        //if (node2.LocalName.Equals("Property"))
                        //    property.Add(node2.Attributes["name"].Value, node2.Attributes["value"].Value);
                        //else
                            throw new Exception("unknown node type=" + node2.LocalName);
                }
                else if (node.LocalName.Equals("Exception"))
                    throw new PolicyException(node.InnerText);
                else
                    throw new Exception("unknown node type=" + node.LocalName);
            }
        }
    }
}
