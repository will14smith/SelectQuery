using System;
using OneOf;
using OneOf.Types;

namespace SelectParser
{
    public class Option<T> : OneOfBase<T, None>
    {
        private Option() : base(new None()) { }
        private Option(T value) : base(value) { }

        public static implicit operator Option<T>(T t) => new(t);
        public static implicit operator Option<T>(None _) => new();

        public bool IsSome => IsT0;
        public bool IsNone => IsT1;

        public Option<TOut> Select<TOut>(Func<T, TOut> mapFn)
        {
            return Match(val => new Option<TOut>(mapFn(val)), none => none);
        }

        public Option<TOut> SelectMany<TOut>(Func<T, Option<TOut>> mapFn)
        {
            return Match(mapFn, none => none);
        }
    }
}
