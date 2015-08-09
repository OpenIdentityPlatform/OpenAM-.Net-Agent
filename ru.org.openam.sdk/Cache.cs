using System;
using System.Collections.Concurrent;
using System.Runtime.Caching;
using System.Threading;

namespace ru.org.openam.sdk
{
	internal class Cache
	{	
		public Cache()
		{
			_cache = new MemoryCache(Guid.NewGuid().ToString());
		}

		private readonly MemoryCache _cache;

		private readonly ConcurrentDictionary<string, DateTime> _locks = new ConcurrentDictionary<string, DateTime>();
	   
		public void Set(string key, object value, int mins)
		{
			var expiration = DateTime.Now.AddMinutes(mins);
			_cache.Set(key, value ?? new NullCacheItem(), expiration);
			Log.Trace(string.Format("{0} inserted to cache expire date: {1}", key, expiration)); 
		}
		
		public T Get<T>(string key)
		{  
			Log.Trace(string.Format("{0} retrieved from cache", key));
			var value = _cache[key];
			
			if (value != null && value is T)
			{
				return (T)value;
			}

			return default(T);
		}

		public void Remove(string key)
		{  
			_cache.Remove(key);
			Log.Trace(string.Format("{0} removed from cache", key));
		}

		public bool Has(string key)
		{
			return _cache[key] != null;
		}	  
		
		public T GetOrDefault<T>(string key, Func<T> defaultValueFunc, Func<T, int> minsFunc)
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

		public T GetOrDefault<T>(string key, Func<T> defaultValueFunc, int mins)
		{	
			return GetOrDefault(key, defaultValueFunc, i => mins);
		}  

		private void Lock(string key)
		{	 
			while (IsLocked(key))
			{
				Thread.Sleep(10);
			}

			_locks.TryAdd(key, DateTime.Now);			
		}

		private void Unlock(string key)
		{	 
			DateTime temp;
			_locks.TryRemove(key, out temp);
		}

		private bool IsLocked(string key)
		{
			DateTime date;
			return _locks.TryGetValue(key, out date) && date < DateTime.Now.AddMinutes(10);
		}
	}

	internal class NullCacheItem
	{
		 
	}
}