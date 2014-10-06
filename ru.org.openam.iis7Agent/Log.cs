using System;
using System.Linq;
using System.Web;
using NLog;

namespace ru.org.openam.iis7Agent
{
	public static class Log
	{
		#region Properties

		// todo брать присылаемые настройки, если нет секции нлога
		private static readonly Logger _logger = LogManager.GetLogger("iis7AgentLog");

		#endregion
		
		#region Log methods

		public static void Fatal(Exception e)
		{
			if(e == null)
			{
				return;
			}

			Fatal(e.ToString());
		}

		/// <summary>
		/// Залогировать фатальную ошибку.
		/// </summary>
		/// <param name="message">Строка сообщения.</param>
		public static void Fatal(string message)
		{
			_logger.Fatal(message);
		}

		/// <summary>
		/// Залогировать предупреждение.
		/// </summary>
		/// <param name="message">Строка сообщения.</param>
		public static void Warning(string message)
		{
			_logger.Warn(message);
		}

		/// <summary>
		/// Залогировать информацию.
		/// </summary>
		/// <param name="message">Строка сообщения.</param>
		public static void Info(string message)
		{
			_logger.Info(message);
		}
		
		public static void Trace(string message)
		{
			_logger.Trace(message);
		}

		#region Private Methods

		#endregion

		#endregion
	}
}