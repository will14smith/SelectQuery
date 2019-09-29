using OneOf;
using OneOf.Types;

namespace SelectQuery
{
    public class Option<T> : OneOfBase<T, None>
    {
        private Option() : base(1) { }
        private Option(T value) : base(0, value) { }

        public static implicit operator Option<T>(T t) => new Option<T>(t);
        public static implicit operator Option<T>(None t) => new Option<T>();
    }
}
