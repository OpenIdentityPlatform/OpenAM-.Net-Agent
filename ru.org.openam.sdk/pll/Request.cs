using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ru.org.openam.sdk.pll
{
    enum type
    {
        unknown,
        auth,
        session
    };

    abstract class  Request
    {
        public type svcid=type.unknown;
        abstract override public String ToString();
    }
}
