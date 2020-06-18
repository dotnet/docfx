// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Microsoft.Docs.Build
{
    internal class ReportModelBuilder
    {
        private readonly ConcurrentHashSet<string> _validationRuleSet = new ConcurrentHashSet<string>();

        public void AddValidationRuleSet(string validationRuleSet)
        {
            _validationRuleSet.TryAdd(validationRuleSet);
        }

        public ReportModel Build()
        {
            return new ReportModel(string.Join(',', _validationRuleSet));
        }
    }
}
