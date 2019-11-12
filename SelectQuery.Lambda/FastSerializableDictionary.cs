using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Utf8Json;

namespace SelectQuery.Lambda
{
    public class FastSerializableDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, IDisposable where TValue : class where TKey : class
    {
        private static readonly TValue[] EmptyValues = new TValue[0];

        private readonly FastSerializableKeys<TKey> _keys;
        private TValue[] _values;

        public FastSerializableDictionary(FastSerializableKeys<TKey> keys)
        {
            _keys = keys;
            _values = EmptyValues;
        }
        private FastSerializableDictionary(FastSerializableKeys<TKey> keys, TValue[] values)
        {
            _keys = keys;
            _values = values;
        }

        public static FastSerializableDictionary<TKey, TValue> FromDictionary(FastSerializableKeys<TKey> keys, IReadOnlyDictionary<TKey, TValue> source)
        {
            var indices = new Dictionary<TKey, int>();
            var count = 0;
            foreach (var key in source.Keys)
            {
                var index = keys.GetOrAddKey(key);
                indices.Add(key, index);
                count = count < index ? index : count;
            }

            var values = ArrayPool<TValue>.Shared.Rent(count);
            foreach (var kvp in source)
            {
                values[indices[kvp.Key]] = kvp.Value;
            }

            return new FastSerializableDictionary<TKey, TValue>(keys, values);
        }

        public static FastSerializableDictionary<TKey, TValue> Deserialize(FastSerializableKeys<TKey> keys, ReadOnlySequence<byte> stream)
        {
            var values = ArrayPool<TValue>.Shared.Rent(keys.Length);

            // TODO allocation :(
            var reader = new JsonReader(stream.ToArray());

            if (!reader.ReadIsBeginArray())
            {
                throw new FormatException("Invalid start");
            }

            var index = 0;
            while (!reader.ReadIsEndArray())
            {
                if (index > 0)
                {
                    reader.ReadIsValueSeparatorWithVerify();
                }

                var value = JsonSerializer.Deserialize<TValue>(ref reader);
                values[index++] = value;
            }

            return new FastSerializableDictionary<TKey, TValue>(keys, values);
        }
        public void Serialize(IBufferWriter<byte> stream)
        {
            var writer = new JsonWriter();

            writer.WriteBeginArray();

            for (var index = 0; index < _keys.Keys.Count; index++)
            {
                if (index > 0)
                {
                    writer.WriteValueSeparator();
                }

                var value = _values[index];
                JsonSerializer.Serialize(ref writer, value);
            }

            writer.WriteEndArray();

            var o = writer.GetBuffer();
            var memory = stream.GetMemory(o.Count);
            o.Array.AsSpan(o.Offset, o.Count).CopyTo(memory.Span);
            stream.Advance(o.Count);
        }

        public int Count => _keys.Length;
        public bool ContainsKey(TKey key)
        {
            return _keys.TryGetKey(key, out _);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default;

            if (!_keys.TryGetKey(key, out var index))
            {
                return false;
            }
            if (index >= _values.Length)
            {
                return false;
            }

            value = _values[index];
            return true;
        }

        public TValue this[TKey key]
        {
            get => _keys.TryGetKey(key, out var index) && index < _values.Length ? _values[index] : throw new KeyNotFoundException();
            set
            {
                var index = _keys.GetOrAddKey(key);
                if (index >= _values.Length)
                {
                    ArrayPool<TValue>.Shared.Resize(ref _values, index + 1);
                }

                _values[index] = value;
            }
        }

        public FastSerializableKeys<TKey> FastKeys => _keys;
        public IEnumerable<TKey> Keys => _keys.Keys;
        public IEnumerable<TValue> Values => _values;

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _keys.Keys.Where((_, index) => index < _values.Length).Select((key, index) => new KeyValuePair<TKey, TValue>(key, _values[index])).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            if (_values.Length > 0)
            {
                ArrayPool<TValue>.Shared.Return(_values);
            }
        }
    }

    public class FastSerializableKeys<TKey> where TKey : class
    {
        private readonly List<TKey> _keys;
        private readonly Dictionary<TKey, int> _keyLookup;

        public FastSerializableKeys() : this(new List<TKey>(), new Dictionary<TKey, int>()) { }
        private FastSerializableKeys(List<TKey> keys, Dictionary<TKey, int> lookup)
        {
            _keys = keys;
            _keyLookup = lookup;
        }

        public IReadOnlyList<TKey> Keys => _keys;
        public int Length => _keys.Count;

        public static FastSerializableKeys<TKey> Deserialize(ReadOnlySequence<byte> stream)
        {
            // TODO allocation :(
            var reader = new JsonReader(stream.ToArray());

            if (!reader.ReadIsBeginArray())
            {
                throw new FormatException("Invalid start");
            }

            var length = reader.ReadInt32();

            var keys = new List<TKey>(length);
            var lookup = new Dictionary<TKey, int>();

            var index = 0;
            while (!reader.ReadIsEndArray())
            {
                reader.ReadIsValueSeparatorWithVerify();
                var value = JsonSerializer.Deserialize<TKey>(ref reader);

                keys.Add(value);
                lookup.Add(value, index++);
            }

            if (index != length)
            {
                throw new FormatException($"Failed to correct number of keys (Expected: {length}, but got: {index}");
            }

            return new FastSerializableKeys<TKey>(keys, lookup);
        }
        public void Serialize(IBufferWriter<byte> stream)
        {
            var writer = new JsonWriter();

            writer.WriteBeginArray();

            writer.WriteInt32(_keys.Count);

            foreach (var value in _keys)
            {
                writer.WriteValueSeparator();
                JsonSerializer.Serialize(ref writer, value);
            }

            writer.WriteEndArray();


            var o = writer.GetBuffer();
            var memory = stream.GetMemory(o.Count);
            o.Array.AsSpan(o.Offset, o.Count).CopyTo(memory.Span);
            stream.Advance(o.Count);

        }

        public bool TryGetKey(TKey key, out int index)
        {
            return _keyLookup.TryGetValue(key, out index);
        }

        public int GetOrAddKey(TKey key)
        {
            if (_keyLookup.TryGetValue(key, out var index))
            {
                return index;
            }

            index = _keys.Count;
            _keys.Add(key);
            _keyLookup.Add(key, index);
            return index;
        }
    }
}
