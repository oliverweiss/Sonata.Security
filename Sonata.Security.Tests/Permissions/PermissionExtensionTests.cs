using Sonata.Core.Extensions;
using Sonata.Security.Extensions;
using Xunit;

namespace Sonata.Security.Tests.Permissions
{
	public class PermissionExtensionTests
    {
        [Fact]
        public void AsTermReturnsUnderscoreIfArgIsNull()
        {
            var expected = "_";
            var actual = ((string) null).AsTerm();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AsTermReturnsUnderscoreIfArgIsEmpty()
        {
            var expected = "_";
            var actual = "".AsTerm();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AsTermReturnsTermIfArgIsNotEmpty()
        {
            var expected = "xyz";
            var actual = "xyz".AsTerm();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AsQuotedStringPropagatesNull()
        {
            var actual = ((string)null).Quote();

            Assert.Null(actual);
        }

        [Fact]
        public void AsQuotedStringReturnsNullWhenArgIsWhitespace()
        {
            var actual = " \t\r\n".Quote();

            Assert.Equal("' \t\r\n'", actual);
        }

        [Fact]
        public void AsQuotedStringSurroundsStringWithSingleQuotes()
        {
            var actual = "test".Quote();

            Assert.Equal(@"'test'", actual);
        }

    }
}
