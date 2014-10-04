using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ru.org.openam.sdk.pll
{
    public enum type
    {
        unknown,
        auth,
        session,
        naming,
        identity
    };

    public abstract class  Request
    {
        public type svcid=type.unknown;
        abstract override public String ToString();
    }
}
