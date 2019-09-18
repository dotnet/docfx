// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Validations.DocFx.Adapter;

namespace Microsoft.Docs.Build
{
    internal class ContentValidationContext : IValidationContext
    {
        private const string ValidationApiBaseUrl = "https://docs.microsoft.com";
        private const int ValidationRequestTimeoutInSeconds = 10;
        private const int ValidationMaxTries = 4;
        private const int ValidationRetryDelaySeconds = 3;
        private const int ValidationMaxTransientFailureCount = 20;

        public string MarkdownRulesFilePath { get; set; }

        public string MetadataRulesFilePath { get; set; }

        public string RepositoryUrl { get; set; }

        public string Branch { get; set; }

        public string ApiBase { get; set; }

        public TimeSpan RequestTimeout { get; set; }

        public int MaxTries { get; set; }

        public int RetryDelaySeconds { get; set; }

        public int MaxTransientFailureCount { get; set; }

        public ContentValidationContext(Config config)
        {
            MarkdownRulesFilePath = config.MarkdownValidationRules;
            RepositoryUrl = EnvironmentVariable.RepositoryUrl;
            Branch = EnvironmentVariable.RepositoryBranch;
            ApiBase = ValidationApiBaseUrl;
            RequestTimeout = TimeSpan.FromSeconds(ValidationRequestTimeoutInSeconds);
            MaxTries = ValidationMaxTries;
            RetryDelaySeconds = ValidationRetryDelaySeconds;
            MaxTransientFailureCount = ValidationMaxTransientFailureCount;
        }
    }
}
