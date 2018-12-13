using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class RetryUtilityTest
    {
        [Theory]
        [InlineData(3, true, null)]
        [InlineData(3, true, new[] { typeof(InvalidOperationException) })]
        [InlineData(3, false, new[] { typeof(NullReferenceException) })]
        [InlineData(30, false, null)]
        [InlineData(30, false, new[] { typeof(InvalidOperationException) })]
        public async Task RetryTest(int triesNeeded, bool succeed, Type[] exceptions)
        {
            var retry = new Retry(triesNeeded);
            try
            {
                await RetryUtility.Retry(() => retry.Try(), exceptions);
                Assert.True(succeed);
            }
            catch (InvalidOperationException)
            {
                Assert.False(succeed);
            }
        }

        private class Retry
        {
            private int _succeedAfterTries;

            public Retry(int succeddAfterTries)
            {
                _succeedAfterTries = succeddAfterTries;
            }

            public Task<bool> Try()
            {
                if (--_succeedAfterTries > 0)
                {
                    throw new InvalidOperationException();
                }
                return Task.FromResult(true);
            }

        }
    }
}
