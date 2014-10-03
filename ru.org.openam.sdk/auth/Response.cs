using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ru.org.openam.sdk.auth
{
    public enum status
    {
        success,
        failed,
        in_progress
    }
    public class Response:pll.Response
    {
        public String authIdentifier=null;
        public List<callback.Callback> callbacks = new List<callback.Callback>();
        public status status = status.in_progress;
        public String ssoToken = null;
        public String successURL = null;
        public String Subject = null;
        public AuthException exception = null;
        public Response()
            : base()
        {
        }

        //<Response authIdentifier="AQIC5wM2LY4SfczMPYDuFRvknKw4U5cpDc4oFB3yuEbLBRY.*AAJTSQACMDM.*">
        //<LoginStatus status="success" ssoToken="AQIC5wM2LY4SfczMPYDuFRvknKw4U5cpDc4oFB3yuEbLBRY.*AAJTSQACMDM.*" successURL="/auth/console">
        //<Subject>AQICNmQzEZ7tWnyC3DMcEuWT730ItKORL0zMFo9mbh6PHFv9Csnsspkp/xdBr1mGxb+XVUKf+hDYjAwltmtTJdZeGHq+s+3rYVixXlRXuOM3b6kT/YhVN0HAtiFI7WAK46+eNWli6sYQKUT5JrRWYV0CUcAA6vTG+ejpWu+pvA+LAsEmrxIZMwDVNPA/o4Bx25UEvDR78Zht7M/gO2SqvhhC0HVy3b+VTQPYzpHVPH/diAgSnNC5OmWkOAbLlGfyOMgcpbLgwkd8Bzn33qLTQkSpZYErZd9JS+yIZi7PcfK3D6hoi4LzDThAjqsebNhs1Nuhc8lkuXaiVGZYoTnyQgJfFb50h3zrK+quST0v8r7kURsARQf6qKfDKvLZpTXdZOR+zts2j/6k7khfGaR1MnNUf6IwTopnUwWmdW3+ZV4J7mw2dEssL+k++oCXL/tVLewRV3UvXW9ehBmexe+6Zxwu9DBSrvOGku431WVPn+euxC9cqtOmcRoB/hSYysIt9+YkVGFDQhhh9EWsB4Ph7hU/KKjyl7RDLh2b352/2XXIrO3sgCwsF3NZRNasxbB1tS5CWQAkJf8cdaBcDEaWKZHMTfIKfcJzmFUDcpde/QgcDNx2N/dvbaVLE6DwTSE/HqTDBL+dw6YL84oYeDPE8i58B2GlSSwO7km0uh1XE82TgI7gEK90LdM4ofQwVkcO9RN5y4UOUUC1Y7Xavo6gQQfq6+UH5hq20jiDYtPqDIPdPMUu6h6Th14Kl/Fk3ixlnlD65+9GFXehTmvw66RODfAAwY8eEJoyD2jja1LlL+exMiu1Rx4UZeoiKmxXxKexBwiVUHQKBBdKL4CDiRitf30qdVsgLE0LHsZQLokBK/SBWediSaatw5rVcnPkXRfkXQTE2ujefbBYvpJccwNFkho8NsyAlhPl96q5ODTV6sQkZTExXBC41wlX2KZD7W+W3jS/DSTl1bL3LufxDaiDw3st/RpdoNV9MuCMcHnWT/cNNZUjT3NuOnRh0myyTY8DdV4W87jyHYxeItRC7scSPKimmpBQmsuAnVh/BnD3ivrejijk1Cv4nI+yEbWAwIDIOf02D1ueOPKwRt0JcHEjgelBM6YOSwn9oqTHgts/q/sCnEOlfa5SSZTJbVRonRBeTSfsZUM5JHYwg/nqkLjX/3yPxqVNvM4qXn15i2ghJteKT7PEpOG1JhpWgLKcoTLKH4GjX2YMp5Ogqhg1eftTlpXgXERJ7ZfEPuXyUwXuNCtv0AXlhsD6LjRaDSNW5BWE1R9uGGdfj9OtD1ihydrPFDKNNozyhciiR2EwkcVFlWd7kthlLiyeLhD48A==</Subject></LoginStatus></Response>

        //<AuthContext version="1.0"><Response authIdentifier="AQIC5wM2LY4SfcxaFDXHvvdY7D9VxFTevGpvBHuQqXb2o8Q.*AAJTSQACMDM.*">
        //<GetRequirements><Callbacks length="3">
        //<PagePropertiesCallback isErrorState="false"><ModuleName>LDAP</ModuleName><HeaderValue>This server uses LDAP Authentication</HeaderValue><ImageName></ImageName><PageTimeOutValue>1200</PageTimeOutValue><TemplateName>Login.jsp</TemplateName><PageState>1</PageState></PagePropertiesCallback>
        //<NameCallback><Prompt> User Name: </Prompt></NameCallback><PasswordCallback echoPassword="false"><Prompt> Password: </Prompt></PasswordCallback></Callbacks></GetRequirements></Response></AuthContext>]]></Response></ResponseSet>
        public Response(XmlNode element)
            : base()
        {
            authIdentifier = ("null".Equals(element.Attributes["authIdentifier"].Value)) ? null : element.Attributes["authIdentifier"].Value;
            foreach (XmlNode node in element.ChildNodes)
            {
                if (node.LocalName.Equals("GetRequirements"))
                    foreach (XmlNode nodeCallback in node.FirstChild)
                        if (nodeCallback.LocalName.Equals("NameCallback"))
                            callbacks.Add(new callback.NameCallback(nodeCallback));
                        else if (nodeCallback.LocalName.Equals("PasswordCallback"))
                            callbacks.Add(new callback.PasswordCallback(nodeCallback));
                        else if (nodeCallback.LocalName.Equals("PagePropertiesCallback"))
                            callbacks.Add(new callback.PagePropertiesCallback(nodeCallback));
                        else
                            throw new Exception("unknown callback type=" + nodeCallback.LocalName);
                else if (node.LocalName.Equals("LoginStatus"))
                {
                    status = (status)Enum.Parse(typeof(status), node.Attributes["status"].Value);
                    if (node.Attributes["ssoToken"] != null)
                        ssoToken = node.Attributes["ssoToken"].Value;
                    if (node.Attributes["ssoToken"] != null)
                        successURL = node.Attributes["successURL"].Value;
                    foreach (XmlNode node2 in node.ChildNodes)
                        if (node2.LocalName.Equals("Subject"))
                            Subject = node2.InnerText;
                        else
                            throw new Exception("unknown node type=" + node2.LocalName);
                }
                else if (node.LocalName.Equals("Exception"))
                    exception=new AuthException(node);
                else
                    throw new Exception("unknown node type=" + node.LocalName);
            }
        }
    }
}
