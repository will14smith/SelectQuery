using System;
using OneOf;
using OneOf.Types;

namespace SelectParser
{
    public static class ValueOption
    {
        public static ValueOption<T> Some<T>(T value) => value;
        public static readonly None None = new None();
    }
    
    public struct ValueOption<T>
    {
        private readonly bool _isSome;
        private readonly T _value;

        private ValueOption(T value)
        {
            _isSome = true;
            _value = value;
        }

        public static implicit operator ValueOption<T>(T t) => new ValueOption<T>(t);
        public static implicit operator ValueOption<T>(None t) => new ValueOption<T>();

        public bool IsSome => _isSome;
        public bool IsNone => !_isSome;
        public T Value => _isSome ? _value : throw new InvalidOperationException();

        public ValueOption<U> Select<U>(Func<T, U> mapFn)
        {
            return _isSome ? new ValueOption<U>(mapFn(_value)) : new ValueOption<U>();
        }

        public ValueOption<U> SelectMany<U>(Func<T, ValueOption<U>> mapFn)
        {
            return _isSome ? mapFn(_value) : new ValueOption<U>();
        }
    }
}
