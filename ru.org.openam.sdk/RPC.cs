using System;
using System.Text;
using System.Net;
using System.Xml;

namespace ru.org.openam.sdk
{
	public class RPC
	{
		static RPC(){
			ServicePointManager.DefaultConnectionLimit = 1024;
		}

		public static HttpWebRequest getHttpWebRequest(Uri uri)
		{
			var request = (HttpWebRequest)WebRequest.Create(uri);
			request.KeepAlive = true;
			request.UserAgent = "openam.org.ru/1.0 (.Net)";

			//<add key="com.sun.identity.agents.config.receive.timeout" value="0"/>
			//<add key="com.sun.identity.agents.config.connect.timeout" value="0"/>
			int connect_timeout=7000;
			int receive_timeout=15000;
			if (Agent.Instance.HasConfig ()) {
				int.TryParse (Agent.Instance.GetSingle ("com.sun.identity.agents.config.connect.timeout"), out connect_timeout);
				int.TryParse (Agent.Instance.GetSingle ("com.sun.identity.agents.config.receive.timeout"), out receive_timeout);
			} else {
				int.TryParse (ConfigurationManager.AppSettings["com.sun.identity.agents.config.connect.timeout"], out connect_timeout);
				int.TryParse (ConfigurationManager.AppSettings["com.sun.identity.agents.config.receive.timeout"], out receive_timeout);
			}
			if (connect_timeout>0)
				request.Timeout = Math.Max(connect_timeout,3000);
			if (receive_timeout>0)
				request.ReadWriteTimeout = Math.Max(receive_timeout,5000);
			return request;
		}

		public static Uri getUrl(naming.Response naming, pll.type type)
		{
			switch (type)
			{
				case pll.type.auth:
					return new Uri(naming.property["iplanet-am-naming-auth-url"].Replace("%protocol://%host:%port%uri", Bootstrap.getUrl().ToString().Replace("/namingservice", "")));
				case pll.type.session:
					return new Uri(naming.property["iplanet-am-naming-session-url"].Replace("%protocol://%host:%port%uri", Bootstrap.getUrl().ToString().Replace("/namingservice", "")));
				case pll.type.identity:
					return new Uri(naming.property["sun-naming-idsvcs-rest-url"].Replace("%protocol://%host:%port%uri", Bootstrap.getUrl().ToString().Replace("/namingservice", "")));
				case pll.type.policy:
					return new Uri(naming.property["iplanet-am-naming-policy-url"].Replace("%protocol://%host:%port%uri", Bootstrap.getUrl().ToString().Replace("/namingservice", "")));
				case pll.type.naming:
					return Bootstrap.getUrl();
				default:
					throw new Exception("unknown type=" + type);
			}
		}

		public static pll.Response GetXML(naming.Response naming, pll.Request request)
		{
			if (pll.type.identity.Equals(request.svcid))
				return new identity.Response(
				   GetXML(
					   new Uri(getUrl(naming, request.svcid).ToString() + "xml/read"),
					   "POST",
					   "application/x-www-form-urlencoded",
					   request.ToString()
				   ).DocumentElement);
			throw new Exception("unknown type=" + request.svcid);
		}
		public static pll.ResponseSet GetXML(Uri uri, pll.RequestSet requests)
		{
			return new pll.ResponseSet(
				GetXML(
					uri,
					"POST",
					"text/xml; encoding='utf-8'",
					requests.ToString()
				).DocumentElement);
		}

		public static pll.ResponseSet GetXML(naming.Response naming, pll.RequestSet requests)
		{
			return GetXML(getUrl((naming.Response)naming, requests.svcid),requests);
		}

		public static XmlDocument GetXML(Uri uri, String method, String ContentType, String body)
		{
			var http = getHttpWebRequest(uri);
			http.Method = "POST";
			http.ContentType = ContentType;
			var uuid = Guid.NewGuid();
			Log.Info(string.Format
			(
				"Message sent (uuid: {1}) {2} {3}{0}User-Agent: {4}{0}Content-Type: {5}{0}{6}{0}",
				"\n",
				uuid,
				http.Method,
				http.RequestUri,
				http.UserAgent,
				http.ContentType,
				body
			));
			byte[] postBytes = (new UTF8Encoding()).GetBytes(body);
			http.ContentLength = postBytes.Length;

			var postStream = http.GetRequestStream();
			postStream.Write(postBytes, 0, postBytes.Length);
			postStream.Close();

			var XMLDocument = new XmlDocument();
			using (var response = (HttpWebResponse)http.GetResponse())
			{
				var myXMLReader = new XmlTextReader(response.GetResponseStream());
				XMLDocument.Load(myXMLReader);
				myXMLReader.Close();
				response.Close();
			}

			Log.Info(string.Format("Message received (uuid: {1}) {2}{0}", Environment.NewLine, uuid, XMLDocument.InnerXml));

			return XMLDocument;
		}
	}
}
