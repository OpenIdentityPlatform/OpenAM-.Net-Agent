using System;
using System.Linq;
using System.Security.Principal;
using System.Web;
using ru.org.openam.sdk;

namespace ru.org.openam.iis7Agent
{
	public class iis7AgentModule : IHttpModule
	{
		private HttpApplication _app;

		private static readonly string[] _notAuthorizedUrls = {"/Home/Login"};//todo ConfigurationManager.AppSettings["FreeUrls"].Split(';');


		public void Init(HttpApplication context)
		{
			this._app = context;
			this._app.AuthenticateRequest += OnAuthentication;
		}

		void OnAuthentication(object sender, EventArgs a)
		{
			var session = GetUserSession();
			var userId = GetUserId(session);
			if (session != null && userId != null)
            {
				var identity = new GenericIdentity(userId);
                var principal = new GenericPrincipal(identity, new string[0]);
                _app.Context.User = principal;

				// копируем пропертесы в айтемы чтобы к ним был доступ из приложения для этого запроса
	            foreach (var prop in session.token.property)
	            {
		            _app.Context.Items[prop.Key] = prop.Value;
	            }
            }
            else if (_notAuthorizedUrls.All(u => !_app.Context.Request.Url.LocalPath.StartsWith(u, StringComparison.InvariantCultureIgnoreCase)))
			{
				_app.Response.Redirect("/Home/Login"); // todo брать из конфига
				//throw new AuthenticationException("Request is not authenticated");
			}
		}

		private string GetUserId(Session session)
		{
			if(session == null || session.token == null)
			{
				return null;
			}

			// todo название свойства в конфиг
			return session.token.property["UserId"];
		}

		private Session GetUserSession()
		{
			var cookie = _app.Context.Request.Cookies[Config.getCookieName()];
			if(cookie == null || string.IsNullOrWhiteSpace(cookie.Value))
			{
				return null;
			}

			var userSession = new Session(_app.Context.Request);
			return userSession;
		}

		public void Dispose() { }
	}
}
