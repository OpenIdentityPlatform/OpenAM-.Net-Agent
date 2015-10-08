using System;
using System.Linq;
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
		static Log()
		{
			Init();
		}

		private static object _sync = new Object();

		private static Logger _debugLogger;

		private static Logger _auditLogger;
		
		private const string DEBUG_LOGGER = "PolicyAgentDebug";

		private const string AUDIT_LOGGER = "PolicyAgentAudit";

		private static bool _noConfig;

		public static String auditLevel=null;
		public static void Init()
		{
			var config = LogManager.Configuration ?? new LoggingConfiguration();

			var debugRule = config.LoggingRules.FirstOrDefault(l => l.LoggerNamePattern == DEBUG_LOGGER);
			var auditRule = config.LoggingRules.FirstOrDefault(l => l.LoggerNamePattern == AUDIT_LOGGER);
			if(debugRule != null && auditRule != null && !_noConfig)
			{
				_debugLogger = LogManager.GetLogger(DEBUG_LOGGER);
				_auditLogger = LogManager.GetLogger(AUDIT_LOGGER);
				return;
			}
			
			_noConfig = true;
			var fileTarget = new FileTarget();
			var retryTargetWrapper = new RetryingTargetWrapper(fileTarget, 3, 100);
			var asyncTargetWrapper = new AsyncTargetWrapper(retryTargetWrapper);
			config.AddTarget("async", asyncTargetWrapper);

			fileTarget.Layout = @"${longdate} ${level} ${message}";
			fileTarget.FileName = "${basedir}/App_Data/Logs/${logger}/${date:format=yyyy-MM-dd}.log";
			fileTarget.Encoding = Encoding.UTF8;

			LogLevel nlogLevel = LogLevel.Info; //default level 

			// todo проверить
			//TODO failover FATAL log to WINDOWS SYSTEM LOG
			if (Agent.Instance.HasConfig())
			{
				//logAudit = Agent.Instance.GetSingle("com.sun.identity.agents.config.audit.accesstype") == "LOG_ALLOW";
				auditLevel=Agent.Instance.GetSingle("com.sun.identity.agents.config.audit.accesstype");

				if (Agent.Instance.GetSingle("com.sun.identity.agents.config.local.log.rotate") == "true")
				{
					long temp;
					fileTarget.ArchiveAboveSize = 104857600;
					fileTarget.MaxArchiveFiles = 9999;
					fileTarget.ArchiveFileName = "${basedir}/App_Data/Logs/${logger}/${date:format=yyyy-MM-dd}_{#}.log";
					fileTarget.ArchiveNumbering = ArchiveNumberingMode.Sequence;
					fileTarget.ArchiveEvery = FileArchivePeriod.None;
					if (long.TryParse(Agent.Instance.GetSingle("com.sun.identity.agents.config.local.log.size"), out temp))
						fileTarget.ArchiveAboveSize = temp;
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

			lock(_sync){
				var oldRule = config.LoggingRules.FirstOrDefault(l => l.LoggerNamePattern == DEBUG_LOGGER);
				if(oldRule != null){
					config.LoggingRules.Remove(oldRule);
				}
				var rule = new LoggingRule(DEBUG_LOGGER, nlogLevel, fileTarget);
				config.LoggingRules.Add(rule);

				oldRule = config.LoggingRules.FirstOrDefault(l => l.LoggerNamePattern == AUDIT_LOGGER);
				if(oldRule != null){
					config.LoggingRules.Remove(oldRule);
				}
				var rule2 = new LoggingRule(AUDIT_LOGGER, LogLevel.Info, fileTarget);
				config.LoggingRules.Add(rule2);

				LogManager.Configuration = config;

				_debugLogger = LogManager.GetLogger(DEBUG_LOGGER);
				_auditLogger = LogManager.GetLogger(AUDIT_LOGGER);
			}
		}


		public static void Fatal(Exception e)
		{
			if (e == null)
				return;

			if(_debugLogger != null)
				_debugLogger.Fatal("(web request id: {0}) {1}", GetRequestId(), e);
		}

		/// <summary>
		/// Залогировать фатальную ошибку.
		/// </summary>
		/// <param name="message">Строка сообщения.</param>
		public static void Fatal(string message)
		{
			if(_debugLogger != null)
				_debugLogger.Fatal("(web request id: {0}) {1}", GetRequestId(), message);
		}

		/// <summary>
		/// Залогировать предупреждение.
		/// </summary>
		/// <param name="message">Строка сообщения.</param>
		public static void Warning(string message)
		{
			if(_debugLogger != null)
				_debugLogger.Warn("(web request id: {0}) {1}", GetRequestId(), message);
		}

		/// <summary>
		/// Залогировать информацию.
		/// </summary>
		/// <param name="message">Строка сообщения.</param>
		public static void Info(string message)
		{
			if(_debugLogger != null)
				_debugLogger.Info("(web request id: {0}) {1}", GetRequestId(), message);
		}

		public static void Trace(string message)
		{
			if(_debugLogger != null)
				_debugLogger.Trace("(web request id: {0}) {1}", GetRequestId(), message);
		}

		public static void Audit(Boolean allow,string message)
		{
			if (_auditLogger != null && ("LOG_BOTH".Equals(auditLevel) || (allow && "LOG_ALLOW".Equals(auditLevel)) || (!allow && "LOG_DENY".Equals(auditLevel))) )
				_auditLogger.Info("(web request id: {0}) {1}", GetRequestId(), message);
		}

		public static void AuditTrace(string message)
		{
			if (_auditLogger != null)
				_auditLogger.Trace("(web request id: {0}) {1}", GetRequestId(), message);
		}
		 
		private static Guid? GetRequestId()
		{
			if (HttpContext.Current == null)
				return null;
	
			const string key = "requestId";
			if (HttpContext.Current.Items[key] == null)
				HttpContext.Current.Items[key] = Guid.NewGuid();
	
			return HttpContext.Current.Items[key] as Guid?;
		}

	}
}