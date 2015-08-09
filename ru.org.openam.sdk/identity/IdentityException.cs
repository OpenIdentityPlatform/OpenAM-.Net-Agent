using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.identity
{
    class IdentityException:Exception
    {
        public IdentityException(String message)
            : base(message)
        {
        }
    }
}
