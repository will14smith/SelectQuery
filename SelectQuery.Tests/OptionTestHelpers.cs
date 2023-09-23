using SelectParser;
using Xunit;

namespace SelectQuery.Tests
{
    internal static class OptionTestHelpers
    {
        public static T AssertSome<T>(Option<T> option)
        {
            Assert.NotNull(option);
            Assert.True(option.IsSome, $"Expecting Some({typeof(T).Name}) but got None instead");
            return option.Value;
        }
        public static void AssertNone<T>(Option<T> option)
        {
            Assert.True(option.IsNone, $"Expecting None but got Some({typeof(T).Name}) instead");
        }
    }
}
