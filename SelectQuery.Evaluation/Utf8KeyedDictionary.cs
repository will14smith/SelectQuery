using System;
using System.Collections;
using System.Text;

namespace SelectQuery.Evaluation;

public class Utf8KeyedDictionary<TValue> : IEnumerable
{
    private struct Bucket
    {
        public string? Key;
        public byte[] KeyUtf8;
        public TValue Value;
        public int HashCode;
        public bool HasCollision;
    }
    
    private Bucket[] _buckets;
    private int _count;
    private int _expandThreshold;

    public Utf8KeyedDictionary()
    {
        _buckets = new Bucket[3];
        _expandThreshold = (int)(_buckets.Length * 0.72f);
    }

    public Utf8KeyedDictionary(Utf8KeyedDictionary<ValueIndex> other)
    {
        _buckets = new Bucket[other._buckets.Length];
        Array.Copy(other._buckets, 0, _buckets, 0, _buckets.Length);
        _count = other._count;
        _expandThreshold = (int)(_buckets.Length * 0.72f);
    }

    public TValue this[string key]
    {
        get => throw new NotImplementedException();
        set => AddOrUpdate(key, value);
    }
    
    public bool TryGetValue(ReadOnlySpan<byte> key, out TValue? value)
    {
        var hash = Hash(key);
        
        var index = hash % _buckets.Length;
        do
        {
            var bucket = _buckets[index];
            if(bucket.HashCode == hash && bucket.KeyUtf8.AsSpan().SequenceEqual(key))
            {
                value = bucket.Value;
                return true;
            }

            if (!bucket.HasCollision)
            {
                value = default;
                return false;
            }

            index = (index + 1) % _buckets.Length;
        } while (true);
    }
    public bool TryGetValue(string key, out TValue? value)
    {
        var keyUtf8 = Encoding.UTF8.GetBytes(key);
        var hash = Hash(keyUtf8);
        
        var index = hash % _buckets.Length;
        do
        {
            var bucket = _buckets[index];
            if(bucket.HashCode == hash && bucket.Key == key)
            {
                value = bucket.Value;
                return true;
            }

            if (!bucket.HasCollision)
            {
                value = default;
                return false;
            }

            index = (index + 1) % _buckets.Length;
        } while (true);
    }
    
    public void Add(string key, TValue value)
    {
        var keyUtf8 = Encoding.UTF8.GetBytes(key);
        var hash = Hash(keyUtf8);
        
        if(_count >= _expandThreshold)
        {
            Expand();
        }
        
        var index = hash % _buckets.Length;
        do
        {
            if (_buckets[index].Key == null)
            {
                _buckets[index] = new Bucket
                {
                    Key = key,
                    KeyUtf8 = keyUtf8,
                    Value = value,
                    HashCode = hash,
                    HasCollision = false
                };
                _count++;
                return;
            }

            if (_buckets[index].Key == key)
            {
                throw new InvalidOperationException("Key already exists");
            }

            _buckets[index].HasCollision = true;
            index = (index + 1) % _buckets.Length;
        } while (true);
    }
    
    private void AddOrUpdate(string key, TValue value)
    {
        var keyUtf8 = Encoding.UTF8.GetBytes(key);
        var hash = Hash(keyUtf8);
        
        if(_count >= _expandThreshold)
        {
            Expand();
        }
        
        var index = hash % _buckets.Length;
        do
        {
            if (_buckets[index].Key == null)
            {
                _buckets[index] = new Bucket
                {
                    Key = key,
                    KeyUtf8 = keyUtf8,
                    Value = value,
                    HashCode = hash,
                    HasCollision = false
                };
                _count++;
                return;
            }

            if (_buckets[index].Key == key)
            {
                _buckets[index].Value = value;
                return;
            }

            _buckets[index].HasCollision = true;
            index = (index + 1) % _buckets.Length;
        } while (true);
    }


    private void Expand()
    {
        var newBuckets = new Bucket[_buckets.Length * 2];

        foreach (var bucket in _buckets)
        {
            var index = bucket.HashCode % newBuckets.Length;
            do
            {
                if (newBuckets[index].Key == null)
                {
                    newBuckets[index] = bucket;
                    newBuckets[index].HasCollision = false;
                    break;
                }
                
                newBuckets[index].HasCollision = true;
                index = (index + 1) % newBuckets.Length;
            } while (true);
        }

        _buckets = newBuckets;
        _expandThreshold = (int)(_buckets.Length * 0.72f);
    }

    public IEnumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }

    // private static int Hash(string key) => Hash(Encoding.UTF8.GetBytes(key));
    private static int Hash(ReadOnlySpan<byte> key)
    {
#if !NETSTANDARD
        var hashCode = new HashCode();
        hashCode.AddBytes(key);
        return hashCode.ToHashCode() & 0x7FFFFFFF;
#else 
        unchecked
        {
            const int p = 16777619;
            var hash = (int)2166136261;

            foreach (var b in key)
            {
                hash = (hash ^ b) * p;
            }

            return hash & 0x7FFFFFFF;
        }

#endif
    }
}