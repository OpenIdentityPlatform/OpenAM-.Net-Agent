using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.naming
{
    class NamingException:Exception
    {
        public NamingException(String message)
            : base(message)
        {
        }
    }
}
