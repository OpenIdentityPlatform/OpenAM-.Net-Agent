using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ru.org.openam.sdk
{
    public class Naming
    {
        public static naming.Response Get(naming.Request request)
        {
			return (naming.Response)request.getResponse(); 
        }
    }
}
