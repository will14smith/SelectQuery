using System;
using OneOf;
using OneOf.Types;

namespace SelectParser
{
    public static class Option
    {
        public static Option<T> Some<T>(T value) => value;
        public static readonly None None = new None();
    }
    
    public class Option<T> : OneOfBase<T, None>
    {
        private Option() : base(1) { }
        private Option(T value) : base(0, value) { }

        public static implicit operator Option<T>(T t) => new Option<T>(t);
        public static implicit operator Option<T>(None t) => new Option<T>();

        public bool IsSome => IsT0;
        public bool IsNone => IsT1;

        public Option<U> Select<U>(Func<T, U> mapFn)
        {
            return Match(val => new Option<U>(mapFn(val)), none => none);
        }

        public Option<U> SelectMany<U>(Func<T, Option<U>> mapFn)
        {
            return Match(mapFn, none => none);
        }
    }
}
