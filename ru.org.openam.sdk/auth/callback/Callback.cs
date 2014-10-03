using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.auth.callback
{
    abstract class Callback
    {
        public Callback()
        {
        }
        public Callback(XmlNode element)
        {
        }

        abstract override public String ToString();
    }
}
