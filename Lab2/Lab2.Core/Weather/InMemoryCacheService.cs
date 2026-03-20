namespace Lab2.Core;

public class InMemoryCacheService : ICacheService
{
    private readonly Dictionary<string, CacheItem> _cache = new();
    private readonly object _lockObject = new();

    public T Get<T>(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        lock (_lockObject)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (item.ExpiresAt > DateTime.UtcNow)
                {
                    return (T)item.Value;
                }
                else
                {
                    _cache.Remove(key);
                }
            }

            return default!;
        }
    }

    public void Set<T>(string key, T value, TimeSpan expiration)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        if (expiration.TotalSeconds <= 0)
            throw new ArgumentException("Expiration must be greater than 0", nameof(expiration));

        lock (_lockObject)
        {
            _cache[key] = new CacheItem
            {
                Value = value,
                ExpiresAt = DateTime.UtcNow.Add(expiration)
            };
        }
    }

    public bool Exists(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        lock (_lockObject)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (item.ExpiresAt > DateTime.UtcNow)
                {
                    return true;
                }
                else
                {
                    _cache.Remove(key);
                }
            }

            return false;
        }
    }

    public void Clear()
    {
        lock (_lockObject)
        {
            _cache.Clear();
        }
    }

    private class CacheItem
    {
        public object Value { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}
