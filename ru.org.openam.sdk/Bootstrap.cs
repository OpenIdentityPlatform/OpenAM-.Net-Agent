using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace ru.org.openam.sdk
{
    public class Bootstrap
    {
        public static Uri getUrl()
        {
            Uri res = new Uri(ConfigurationManager.AppSettings["com.sun.identity.agents.config.naming.url"]);
            return res;
        }
        public static String getAppRealm()
        {
            String res=ConfigurationManager.AppSettings["com.sun.identity.agents.config.organization.name"];
            return res==null?"/":res;
        }
        public static String getAppUser()
        {
            return ConfigurationManager.AppSettings["com.sun.identity.agents.app.username"];
        }
        public static String getAppPassword()
        {
            return ConfigurationManager.AppSettings["com.iplanet.am.service.password"];
        }

        //<Attribute name="iplanet-am-naming-session-class" value="com.iplanet.dpro.session.service.SessionRequestHandler"></Attribute>
        //<Attribute name="iplanet-am-naming-samlsoapreceiver-url" value="%protocol://%host:%port%uri/SAMLSOAPReceiver"></Attribute>
        //<Attribute name="02" value="http://login.staging.rapidsoft.ru:80/auth"></Attribute>
        //<Attribute name="iplanet-am-naming-auth-url" value="%protocol://%host:%port%uri/authservice"></Attribute>
        //<Attribute name="sun-naming-idsvcs-rest-url" value="%protocol://%host:%port%uri/identity/"></Attribute>
        //<Attribute name="04" value="https://login.staging.rapidsoft.ru:444/auth"></Attribute>
        //<Attribute name="iplanet-am-platform-site-id-list" value="04,05,01|02|05|06|04,06,02,03"></Attribute>
        //<Attribute name="iplanet-am-naming-fsassertionmanager-url" value="%protocol://%host:%port%uri/FSAssertionManagerServlet/FSAssertionManagerIF"></Attribute>
        //<Attribute name="openam-am-platform-site-names-list" value="dmz|02"></Attribute>
        //<Attribute name="iplanet-am-naming-auth-class" value="com.sun.identity.authentication.server.AuthXMLHandler"></Attribute>
        //<Attribute name="06" value="http://test.rapidsoft.ru:8080/auth"></Attribute>
        //<Attribute name="iplanet-am-naming-samlawareservlet-url" value="%protocol://%host:%port%uri/SAMLAwareServlet"></Attribute>
        //<Attribute name="iplanet-am-platform-lb-cookie-value-list" value="01|01,03|03"></Attribute>
        //<Attribute name="serviceObjectClasses" value="iplanet-am-naming-service"></Attribute>
        //<Attribute name="iplanet-am-platform-server-list" value="https://login.staging.rapidsoft.ru:444/auth,http://sso.rapidsoft.ru:8080/auth,http://localhost.rapidsoft.ru:8080/auth,http://test.rapidsoft.ru:8080/auth,http://login.staging.rapidsoft.ru:80/auth"></Attribute>
        //<Attribute name="iplanet-am-naming-samlassertionmanager-url" value="%protocol://%host:%port%uri/AssertionManagerServlet/AssertionManagerIF"></Attribute>
        //<Attribute name="03" value="http://localhost.rapidsoft.ru:8080/auth"></Attribute>
        //<Attribute name="sun-naming-idsvcs-jaxws-url" value="%protocol://%host:%port%uri/identityservices/"></Attribute>
        //<Attribute name="iplanet-am-naming-policy-class" value="com.sun.identity.policy.remote.PolicyRequestHandler"></Attribute>
        //<Attribute name="sun-naming-sts-mex-url" value="%protocol://%host:%port%uri/sts/mex"></Attribute>
        //<Attribute name="iplanet-am-naming-profile-url" value="%protocol://%host:%port%uri/profileservice"></Attribute>
        //<Attribute name="iplanet-am-naming-session-url" value="%protocol://%host:%port%uri/sessionservice"></Attribute>
        //<Attribute name="sun-naming-sts-url" value="%protocol://%host:%port%uri/sts"></Attribute>
        //<Attribute name="iplanet-am-naming-logging-url" value="%protocol://%host:%port%uri/loggingservice"></Attribute>
        //<Attribute name="iplanet-am-naming-securitytokenmanager-url" value="%protocol://%host:%port%uri/SecurityTokenManagerServlet/SecurityTokenManagerIF"></Attribute>
        //<Attribute name="iplanet-am-naming-samlpostservlet-url" value="%protocol://%host:%port%uri/SAMLPOSTProfileServlet"></Attribute>
        //<Attribute name="iplanet-am-naming-jaxrpc-url" value="%protocol://%host:%port%uri/jaxrpc/"></Attribute>
        //<Attribute name="iplanet-am-naming-profile-class" value="com.iplanet.dpro.profile.agent.ProfileService"></Attribute>
        //<Attribute name="iplanet-am-naming-logging-class" value="com.sun.identity.log.service.LogService"></Attribute>
        //<Attribute name="iplanet-am-naming-policy-url" value="%protocol://%host:%port%uri/policyservice"></Attribute>
        //<Attribute name="05" value="http://sso.rapidsoft.ru:8080/auth"></Attribute>
        static naming.Response globalNaming = Naming.Get(new naming.Request());
        public static naming.Response GetNaming()
        {
			return globalNaming;
        }
    }
}
