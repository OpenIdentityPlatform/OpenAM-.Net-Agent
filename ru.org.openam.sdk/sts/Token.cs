using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.sts
{
    class Token
    {
        Session appSession;
        Session userSession;
        public Token(Session appSession, Session userSession)
        {
            this.appSession=appSession;
            this.userSession = userSession;
        }

        //<fam:FAMToken xmlns:fam=\"http://www.sun.com/identity/famtoken\">
        //<fam:TokenValue>AQIC5wM2LY4SfczoW-L3juzUnAmsMYJvuXHvZuMvL3jZvoo.*AAJTSQACMDM.*</fam:TokenValue>
        //<fam:AppTokenValue>AQIC5wM2LY4SfczRz6I2t8HRAmjQen9K-als1NU9rDwX8Iw.*AAJTSQACMDM.*</fam:AppTokenValue>
        //<fam:TokenType>urn:sun:wss:ssotoken</fam:TokenType></fam:FAMToken>
        
        //<fam:FAMToken p1:fam="AQIC5wM2LY4SfcwGmeXEv-njDZoqtHJ9y_SssmNU83h8JPM.*AAJTSQACMDM.*" p2:fam="AQIC5wM2LY4Sfcz_2q68PrPaj7bN54ZIso2qtLnPssUsAPw.*AAJTSQACMDM.*" p3:fam="urn:sun:wss:ssotoken" xmlns:fam=
        //"http://www.sun.com/identity/famtoken" xmlns:p1="TokenValue" xmlns:p2="AppTokenValue" xmlns:p3="TokenTypee"/>

        override public String ToString()
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = new UTF8Encoding();
            XmlWriter writer = XmlWriter.Create(sb, settings);
            writer.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"");
            writer.WriteStartElement("fam", "FAMToken", "http://www.sun.com/identity/famtoken");
                //writer.WriteAttributeString("xmlns","fam", "http://www.sun.com/identity/famtoken");
            writer.WriteElementString("fam", "TokenValue", "http://www.sun.com/identity/famtoken", userSession.ToString());
            writer.WriteElementString("fam", "AppTokenValue", "http://www.sun.com/identity/famtoken", appSession.ToString());
            writer.WriteElementString("fam", "TokenType", "http://www.sun.com/identity/famtoken", "urn:sun:wss:ssotoken");
            writer.WriteEndElement();
            writer.Close();
            return sb.ToString();
        }

        public static implicit operator XmlElement(Token d)
        {
            var doc=new XmlDocument();
            doc.LoadXml(d.ToString());
            return doc.DocumentElement;
        }
    }
}
