using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Web;
using System.Net;

namespace ru.org.openam.sdk.identity
{
   public class Request: pll.Request
    {
        public Request() : base()
        {
            svcid = pll.type.identity;
        }

        //name - Name of identity
        //attributes_names - LDAP attributes to be searched against
        //attributes_values_<value_from_attribute_names> - Values for LDAP attributes
        //admin - tokenid for a user with permissions to perform search
        String query = "";
		Session session;
        public Request(String name, String[] attributes_names, KeyValuePair<String,String>[] values_from_attribute_names, Session admin)
            : this()
        {
			session = admin;
            query = query+String.Format("admin={0}&",  HttpUtility.UrlEncode(admin.sessionId));
            if (!String.IsNullOrEmpty(name))
                query = query + String.Format("name={0}&", HttpUtility.UrlEncode(name));
            if (attributes_names!=null)
                foreach (String attributes_name in attributes_names)
                    query = query + String.Format("attributes_names={0}&", HttpUtility.UrlEncode(attributes_name));
            foreach (KeyValuePair<String, String> value_from_attribute_names in values_from_attribute_names)
                query = query + String.Format("attributes_values_{0}={1}&", HttpUtility.UrlEncode(value_from_attribute_names.Key),HttpUtility.UrlEncode(value_from_attribute_names.Value));
        }

		override public Uri getUrl()
		{
			return new Uri(GetNaming().property["sun-naming-idsvcs-rest-url"].Replace("%protocol://%host:%port%uri", Bootstrap.getUrl().ToString().Replace("/namingservice", ""))+ "xml/read");
		}

		override protected String getContentType(){
			return "application/x-www-form-urlencoded";
		}

		override public CookieContainer getCookieContainer(){
			return new session.Request(session).getCookieContainer();
		}

        override public String ToString()
        {
            return query;
        }

		override protected String getRequestString(){
			return ToString();
		}

		override protected pll.Response getResponse(String data){
			XmlDocument xml = new XmlDocument ();
			xml.LoadXml(data);
			XmlElement element = xml.DocumentElement;
			return new Response(getCookieContainer(),element);
		}
    }
}
