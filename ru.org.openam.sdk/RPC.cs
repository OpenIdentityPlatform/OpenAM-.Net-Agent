using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;
using System.IO;

namespace ru.org.openam.sdk
{
    class RPC
    {
        public static pll.ResponseSet Get(pll.RequestSet requests)
        {
            var httpRequest = Bootstrap.getHttpWebRequest(requests.svcid);
            System.Diagnostics.Trace.TraceInformation("{0}",httpRequest.RequestUri);

            String req = requests.ToString();
            System.Diagnostics.Trace.TraceInformation("request:\r\n {0}", req);
                        
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] postBytes = (new UTF8Encoding()).GetBytes(req);
            httpRequest.ContentLength = postBytes.Length;

            var postStream = httpRequest.GetRequestStream();
            postStream.Write(postBytes, 0, postBytes.Length);
            postStream.Close();

            var XMLDocument = new XmlDocument();
            using (var response = (HttpWebResponse)httpRequest.GetResponse())
            {
                var myXMLReader = new XmlTextReader(response.GetResponseStream());
                XMLDocument.Load(myXMLReader);
                myXMLReader.Close();
                response.Close();
            }
            System.Diagnostics.Trace.TraceInformation("response:\r\n {0}", XMLDocument.InnerXml);
            return new pll.ResponseSet(XMLDocument.DocumentElement);
        }
    }
}
