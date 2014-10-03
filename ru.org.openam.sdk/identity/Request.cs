using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.identity
{
//String attributeServiceURL = url + ATTRIBUTE_SERVICE 
//                + "?name=" + URLEncoder.encode(profileName, "UTF-8") 
//                + "&attributes_names=realm"
//                + "&attributes_values_realm=" 
//                + URLEncoder.encode(realm, "UTF-8")
//                + "&attributes_names=objecttype"
//                + "&attributes_values_objecttype=Agent"
//                + "&admin=" + URLEncoder.encode(tokenId, "UTF-8"); 
 //GET /auth/identity/xml/read?name=mbank2&attributes_names=realm&attributes_values_realm=%2F&attributes_names=objecttype&attributes_values_objecttype=Agent&admin=AQIC5wM2LY4SfczFMrAkVFsERiXXXq2vD8rj6iMRBvr7QHM.*AAJTSQACMDIAAlNLAAotMjQwMjY3NjA2AAJTMQACMDQ.* HTTP/1.0
    public class Request: pll.Request
    {
        static int reqid = 1;

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

        override public String ToString()
        {
            return "";
        }
    }
}
