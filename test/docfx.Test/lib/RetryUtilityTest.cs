// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class RetryUtilityTest
    {
        [Theory]
        [InlineData(3, true, new[] { typeof(InvalidOperationException) })]
        [InlineData(3, false, new[] { typeof(NullReferenceException) })]
        [InlineData(30, false, new[] { typeof(InvalidOperationException) })]
        public async Task RetryTest(int triesNeeded, bool succeed, Type[] catches)
        {
            var retry = new Retry(triesNeeded);
            try
            {
                await RetryUtility.Retry(
                    () => retry.Try(),
                    ex => catches is null ? true : catches.Any(e => e.IsInstanceOfType(ex)));
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
