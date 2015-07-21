using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Xml;

namespace ru.org.openam.sdk.pll
{
    public enum type
    {
        unknown,
        auth,
        session,
        naming,
        identity,
        policy
    };

    public abstract class  Request
    {
		static Request(){
			ServicePointManager.DefaultConnectionLimit = 1024;
			ServicePointManager.Expect100Continue = false;
		}

        public type svcid=type.unknown;
        
		abstract override public String ToString();

		abstract public Uri getUrl();
		
		CookieContainer cookieContainer=null;
		public CookieContainer getCookieContainer(){
			if (cookieContainer==null)
				cookieContainer=new CookieContainer();
			return cookieContainer;
		}

		virtual protected naming.Response GetNaming(){
			return Bootstrap.GetNaming ();
		}

		HttpWebRequest getHttpWebRequest()
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(getUrl());
			request.KeepAlive = true;
			request.UserAgent = "openam.org.ru/1.0 (.Net)";
			request.CookieContainer = getCookieContainer();
			int connect_timeout=7000,receive_timeout=15000;
			if (Agent.Instance.HasConfig()) {
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

		virtual protected String getMethod(){
			return "POST";
		}

		virtual protected String getContentType(){
			return "text/xml; encoding='utf-8'";
		}

		protected Guid uuid = Guid.NewGuid(); //tracking

		virtual protected String getRequestString(){
			return new pll.RequestSet(new pll.Request[]{this}).ToString();
		}

		virtual protected Response getResponse(String data){
			ResponseSet responses = new ResponseSet(getCookieContainer(),data);
			if (responses.Count > 0)
				return (Response)responses[0];
			throw new Exception(data);
		}

		public Response getResponse()
		{
			HttpWebRequest request = getHttpWebRequest();
			request.Method = getMethod();
			request.ContentType = getContentType();
			request.CookieContainer.Add(new Cookie("track", uuid.ToString()) { Domain = getUrl().Host });

			String body = getRequestString();
			byte[] postBytes = (new UTF8Encoding()).GetBytes(body);
			request.ContentLength = postBytes.Length;
			using (Stream requestStream = request.GetRequestStream()){ 
				try{
					Log.Info (string.Format("{1} {2}{0}{3}{4}",Environment.NewLine,request.Method,request.RequestUri,request.Headers,body));
					requestStream.Write(postBytes, 0, postBytes.Length);
				}finally{
					requestStream.Close();
				}
			}

			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
			{
				try{
					using (StreamReader streamReader = new StreamReader(response.GetResponseStream ())) {
						try{
							String data=streamReader.ReadToEnd();
							Log.Info(string.Format("Message received (uuid: {1}){0}{3}{2}{0}", Environment.NewLine, uuid, data,response.Headers));
							return getResponse(data);
						}finally{
							streamReader.Close();
						}
					}
				}finally{
					response.Close ();
				}
			}
		}
    }
}
