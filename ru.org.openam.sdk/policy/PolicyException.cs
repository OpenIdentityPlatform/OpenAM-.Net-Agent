using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.policy
{
    public class PolicyException : Exception
    {
        public PolicyException(String message)
            : base(message)
        {
        }
    }
}
