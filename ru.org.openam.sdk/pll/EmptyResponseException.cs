using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.pll
{
    public class EmptyResponseException :Exception
    {
        public EmptyResponseException(String message)
            : base(message)
        {
        }
    }
}
