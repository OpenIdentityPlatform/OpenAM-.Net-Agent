using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.session
{
    class SessionException:Exception
    {
        public SessionException(String message)
            : base(message)
        {
        }
    }
}
