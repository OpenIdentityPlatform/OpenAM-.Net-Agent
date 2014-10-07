using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Web;
using System.Web.Caching;
using ru.org.openam.sdk;

namespace ru.org.openam.iis7Agent
{
	internal static class Cache
	{
		private static readonly ConcurrentDictionary<string, DateTime> _locks = new ConcurrentDictionary<string, DateTime>();
	   
		public static void Set(string key, object value, int mins)
		{
			var expiration = DateTime.Now.AddMinutes(mins);
			HttpContext.Current.Cache.Insert(key, value ?? new NullCacheItem(), null, expiration, TimeSpan.Zero, CacheItemPriority.High, null);
			Log.Trace(string.Format("{0} inserted to cache expire date: {1}", key, expiration)); 
		}
		
		public static T Get<T>(string key)
		{  
			Log.Trace(string.Format("{0} retrieved from cache by key", key));
			var value = HttpContext.Current.Cache[key];
			
			if (value != null && value is T)
			{
				return (T)value;
			}

			return default(T);
		}

		public static void Remove(string key)
		{
			Remove(HttpContext.Current, key);
		}

		public static void Remove(HttpContext context, string key)
		{  
			context.Cache.Remove(key);
			Log.Trace(string.Format("{0} removed from cache", key));
		}

		public static bool Has(string key)
		{
			return Has(HttpContext.Current, key);
		}

		public static bool Has(HttpContext context, string key)
		{
			return context.Cache[key] != null;
		}	  
		
		public static T GetOrDefault<T>(string key, Func<T> defaultValueFunc, Func<T, int> minsFunc)
		{
			if (Has(key))
			{
				return Get<T>(key);
			}
			else
			{
				
				Lock(key);
				T val;
				try
				{
					if (!Has(key))
					{
						val = defaultValueFunc();
						var mins = minsFunc(val);
						Set(key, val, mins);
					}
					else
					{
						val = Get<T>(key);
					}
				}
				finally 
				{ 
					Unlock(key);
				}
				return val;
			}
		}

		public static T GetOrDefault<T>(string key, Func<T> defaultValueFunc, int mins)
		{	
			return GetOrDefault(key, defaultValueFunc, i => mins);
		}  

		private static void Lock(string key)
		{	 
			while (IsLocked(key))
			{
				Thread.Sleep(10);
			}

			_locks.TryAdd(key, DateTime.Now);			
		}

		private static void Unlock(string key)
		{	 
			DateTime temp;
			_locks.TryRemove(key, out temp);
		}

		private static bool IsLocked(string key)
		{
			DateTime date;
			return _locks.TryGetValue(key, out date) && date < DateTime.Now.AddMinutes(10);
		}
	}

	internal class NullCacheItem
	{
		 
	}
}