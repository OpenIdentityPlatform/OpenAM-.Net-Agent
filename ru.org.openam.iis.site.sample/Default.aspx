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
</body>
</html>

