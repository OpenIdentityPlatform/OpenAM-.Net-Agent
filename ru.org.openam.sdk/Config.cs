using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Net;

namespace ru.org.openam.sdk
{
    class Config
    {
        public static Uri getUrl()
        {
            Uri res=new Uri(ConfigurationManager.AppSettings["com.iplanet.am.server.protocol"] + "://" + ConfigurationManager.AppSettings["com.iplanet.am.server.host"] + ":" + ConfigurationManager.AppSettings["com.iplanet.am.server.port"] + ConfigurationManager.AppSettings["com.iplanet.am.services.deploymentDescriptor"]);
            return res;
        }
        public static String getAppUser()
        {
            return ConfigurationManager.AppSettings["com.sun.identity.agents.app.username"];
        }
        public static String getAppPassword()
        {
            return ConfigurationManager.AppSettings["com.iplanet.am.service.password"];
        }
        public static String getCookieName()
        {
            return ConfigurationManager.AppSettings["com.iplanet.am.cookie.name"];
        }

        public static HttpWebRequest getHttpWebRequest(pll.type type)
        {
            var request = (HttpWebRequest)WebRequest.Create(getUrl(type));
            request.KeepAlive = true;
            request.UserAgent = "OpenAM .Net SDK (Version 1.0)";
            request.Method = "POST";
            request.ContentType = "text/xml; encoding='utf-8'";
            return request;
        }

        public static Uri getUrl(pll.type type)
        {
            switch (type)
            {
               case pll.type.auth:
                  return new Uri(getUrl()+"/authservice");
               case pll.type.session:
                  return new Uri(getUrl() + "/sessionservice");
               default:
                  throw new Exception("unknown type="+type);
            }
        }

    }
}
