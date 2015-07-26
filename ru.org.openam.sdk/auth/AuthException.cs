using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.auth
{
    public class AuthException:Exception
    {
        public String messageException;
        public String errorCode;
        public String templateName;

        //<Exception message="invalid password"  errorCode="103" templateName="login_failed_template.jsp"></Exception>
        public AuthException(XmlNode element)
            : base()
        {
            foreach (XmlAttribute attr in element.Attributes)
                if (attr.LocalName.Equals("message"))
                    messageException = attr.Value;
                else if (attr.LocalName.Equals("errorCode"))
                    errorCode = attr.Value;
                else if (attr.LocalName.Equals("templateName"))
                    templateName = attr.Value;
                else
                    throw new Exception("unknown attribute type=" + attr.LocalName);
        }
    }
}
