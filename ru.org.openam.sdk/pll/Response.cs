using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;

namespace ru.org.openam.sdk.pll
{
    public class Response
    {
		public CookieContainer cookieContainer;

        public Response() { }
		public Response(CookieContainer cookieContainer,XmlNode element) {
			this.cookieContainer = cookieContainer;
		}
    }
}
