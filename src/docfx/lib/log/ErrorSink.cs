// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Holds a collection of deduped errors with a maximum bound.
    /// </summary>
    internal class ErrorSink
    {
        private readonly HashSet<Error> _errors = new HashSet<Error>(Error.Comparer);

        public int ErrorCount { get; private set; }

        public int WarningCount { get; private set; }

        public int SuggestionCount { get; private set; }

        public int InfoCount { get; private set; }

        public bool Add(Config? config, Error error, ErrorLevel level)
        {
            lock (_errors)
            {
                if (config is null)
                {
                    return _errors.Add(error);
                }

                var exceedMaxAllowed = level switch
                {
                    ErrorLevel.Error => ErrorCount >= config.MaxFileErrors,
                    ErrorLevel.Warning => WarningCount >= config.MaxFileWarnings,
                    ErrorLevel.Suggestion => SuggestionCount >= config.MaxFileSuggestions,
                    ErrorLevel.Info => InfoCount >= config.MaxFileInfos,
                    _ => false,
                };

                if (exceedMaxAllowed || !_errors.Add(error))
                {
                    return false;
                }

                return level switch
                {
                    ErrorLevel.Error => ++ErrorCount,
                    ErrorLevel.Warning => ++WarningCount,
                    ErrorLevel.Suggestion => ++SuggestionCount,
                    ErrorLevel.Info => ++InfoCount,
                    _ => 0,
                } > 0;
            }
        }
    }
}
