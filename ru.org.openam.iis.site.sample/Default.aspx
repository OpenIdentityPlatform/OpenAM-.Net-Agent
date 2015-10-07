<%@ Page Language="C#" Inherits="ru.org.openam.iis.site.sample.Default" %>
<!DOCTYPE html>
<html>
<head runat="server">
	<title>Default</title>
</head>
<body>
	<%
		NameValueCollection headers = base.Request.Headers;
		for (int i = 0; i < headers.Count; i++)
		    Response.Write(headers.GetKey(i) + " = " + headers.Get(i) + "<br/>");
    %>

    <hr/>

    <%
		NameValueCollection sv = base.Request.ServerVariables;
		for (int i = 0; i < sv.Count; i++)
		    Response.Write(sv.GetKey(i) + " = " + sv.Get(i) + "<br/>");
    %>
</body>
</html>

