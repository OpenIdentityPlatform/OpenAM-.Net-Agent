using System;
using System.Text;
using System.Web;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace ru.org.openam.sdk
{
	public static class Log
	{
		private static readonly bool _dafaultConfig;

		static Log()
		{
			if (LogManager.Configuration == null)
			{
				_dafaultConfig = true;
			}
			
			IntInitLog();  
		}

		public static void InitLog()
		{
			if(_dafaultConfig)
			{
				IntInitLog();
			}
		}


		private static void IntInitLog()
		{
			if(_dafaultConfig)
			{	
				var config = new LoggingConfiguration();

				var fileTarget = new FileTarget();
				var retryTargetWrapper = new RetryingTargetWrapper(fileTarget, 3, 100);
				var asyncTargetWrapper = new AsyncTargetWrapper(retryTargetWrapper);
				config.AddTarget("async", asyncTargetWrapper);

				fileTarget.Layout = @"${longdate} ${level} ${message}";
				fileTarget.FileName = "${basedir}/App_Data/Logs/${logger}/${date:format=yyyy-MM-dd}.txt";
				fileTarget.Encoding = Encoding.UTF8;

				LogLevel nlogLevel = LogLevel.Debug;
				// todo проверить
				if (Agent.Instance.HasConfig())
				{
					if (Agent.Instance.GetSingle("com.sun.identity.agents.config.local.log.rotate") == "true")
					{
						long temp;
						fileTarget.ArchiveAboveSize = 104857600;
						fileTarget.MaxArchiveFiles = 9999;
						fileTarget.ArchiveFileName = "${basedir}/App_Data/Logs/${logger}/${date:format=yyyy-MM-dd}_{#}.txt";
						fileTarget.ArchiveNumbering = ArchiveNumberingMode.Sequence;
						fileTarget.ArchiveEvery = FileArchivePeriod.None;
						if (long.TryParse(Agent.Instance.GetSingle("com.sun.identity.agents.config.local.log.size"), out temp))
						{
							fileTarget.ArchiveAboveSize = temp;
						}
					}

					var configLevel = Agent.Instance.GetSingle("com.sun.identity.agents.config.debug.level");
					switch (configLevel)
					{
						case "Error":
							nlogLevel = LogLevel.Error;
							break;
						case "Warning":
							nlogLevel = LogLevel.Warn;
							break;
						case "Info":
							nlogLevel = LogLevel.Info;
							break;
						default:
							nlogLevel = LogLevel.Trace;
							break;
					}
				}

				var rule = new LoggingRule("PolicyAgentDebug", nlogLevel, fileTarget);
				config.LoggingRules.Add(rule);
				
				var rule2 = new LoggingRule("PolicyAgentAudit", LogLevel.Info, fileTarget);
				config.LoggingRules.Add(rule2);

				LogManager.Configuration = config;
			}

			_debugLogger = LogManager.GetLogger("PolicyAgentDebug");

			if (!_dafaultConfig || Agent.Instance.HasConfig() && Agent.Instance.GetSingle("com.sun.identity.agents.config.audit.accesstype") == "LOG_ALLOW")
			{
				_auditLogger = LogManager.GetLogger("PolicyAgentAudit");
			}
		}

		private static Logger _debugLogger;

		private static Logger _auditLogger;
		
		public static void Fatal(Exception e)
		{
			if (e == null)
			{
				return;
			}

			if(_debugLogger != null)
			{
				_debugLogger.Fatal("(web request id: {0}) {1}", GetRequestId(), e);
			}
		}

		/// <summary>
		/// Залогировать фатальную ошибку.
		/// </summary>
		/// <param name="message">Строка сообщения.</param>
		public static void Fatal(string message)
		{
			if(_debugLogger != null)
			{
				_debugLogger.Fatal("(web request id: {0}) {1}", GetRequestId(), message);
			}
		}

		/// <summary>
		/// Залогировать предупреждение.
		/// </summary>
		/// <param name="message">Строка сообщения.</param>
		public static void Warning(string message)
		{
			if(_debugLogger != null)
			{
				_debugLogger.Warn("(web request id: {0}) {1}", GetRequestId(), message);
			}
		}

		/// <summary>
		/// Залогировать информацию.
		/// </summary>
		/// <param name="message">Строка сообщения.</param>
		public static void Info(string message)
		{
			if(_debugLogger != null)
			{
				_debugLogger.Info("(web request id: {0}) {1}", GetRequestId(), message);
			}
		}

		public static void Trace(string message)
		{
			if(_debugLogger != null)
			{
				_debugLogger.Trace("(web request id: {0}) {1}", GetRequestId(), message);
			}
		}

		public static void Audit(string message)
		{
			if (_auditLogger != null)
			{
				_auditLogger.Info("(web request id: {0}) {1}", GetRequestId(), message);
			}
		}

		public static void AuditTrace(string message)
		{
			if (_auditLogger != null)
			{
				_auditLogger.Info("(web request id: {0}) {1}", GetRequestId(), message);
			}
		}
		 
		private static Guid? GetRequestId()
		{
			if (HttpContext.Current == null)
			{
				return null;
			}

			const string key = "requestId";
			if (HttpContext.Current.Items[key] == null)
			{
				HttpContext.Current.Items[key] = Guid.NewGuid();
			}

			return HttpContext.Current.Items[key] as Guid?;
		}

	}
}