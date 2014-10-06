using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.session
{
    public class SessionException:Exception
    {
        public SessionException(String message)
            : base(message)
        {
        }
    }
}
