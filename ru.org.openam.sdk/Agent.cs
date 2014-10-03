using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ru.org.openam.sdk.auth.callback;

namespace ru.org.openam.sdk
{
    public class Agent
    {
        
        public Agent()
        {
            
        }

        Session session;
        public Session getSession()
        {
            if (session==null||!session.isValid())
                lock (this)
                {
                    if (session == null || !session.isValid()) //need re-auth ?
                    {
                        session = Auth.login(
                            Bootstrap.getAppRealm(),
                            auth.indexType.moduleInstance, "Application",
                            new Callback[] { 
                                new NameCallback(Bootstrap.getAppUser()), 
                                new PasswordCallback(Bootstrap.getAppPassword()) 
                            }
                        );
                        naming = GetNaming(); //clear naming
                        config = GetConfig();//clear config
                    }
                }
            return session;
        }

        naming.Response naming;
        public naming.Response GetNaming() //for personal session naming (need agent only)
        {
            if (naming == null)
                naming = Naming.Get(new naming.Request(getSession().sessionId));
            return naming;
        }

        Dictionary<String, String> config;
        public Dictionary<String,String> GetConfig() 
        {
            //if (config == null)
            //    config = Naming.Get(new naming.Request(getSession().sessionId));
            return config;
        }

        public String GetCookieName()
        {
            return GetConfig()["com.iplanet.am.cookie.name"];
        }
    }
}
