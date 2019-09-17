// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Validations.DocFx.Adapter;

namespace Microsoft.Docs.Build
{
    internal class ContentValidationContext : IValidationContext
    {
        private const string ValidationApiBaseUrl = "validationApiBaseUrl";
        private const string DefaultValidationApiBaseUrl = "https://docs.microsoft.com";
        private const string ValidationRequestTimeoutInSeconds = "validationRequestTimeoutInSeconds";
        private const int DefaultValidationRequestTimeoutInSeconds = 10;
        private const string ValidationMaxTries = "validationMaxTries";
        private const int DefaultValidationMaxTries = 4;
        private const string ValidationRetryDelaySeconds = "validationRetryDelaySeconds";
        private const int DefaultValidationRetryDelaySeconds = 3;
        private const string ValidationMaxTransientFailureCount = "validationMaxTransientFailureCount";
        private const int DefaultValidationMaxTransientFailureCount = 20;

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
            MarkdownRulesFilePath = config.ContentValidationRulesFilePath;
            RepositoryUrl = EnvironmentVariable.RepositoryUrl;
            Branch = EnvironmentVariable.RepositoryBranch;
            ApiBase = config.GlobalMetadata.TryGetValue(ValidationApiBaseUrl, out var apiBase) ? apiBase.ToObject<string>() : DefaultValidationApiBaseUrl;
            RequestTimeout = TimeSpan.FromSeconds(config.GlobalMetadata.TryGetValue(ValidationRequestTimeoutInSeconds, out var requestTimeout) ? requestTimeout.ToObject<int>() : DefaultValidationRequestTimeoutInSeconds);
            MaxTries = config.GlobalMetadata.TryGetValue(ValidationMaxTries, out var maxTries) ? maxTries.ToObject<int>() : DefaultValidationMaxTries;
            RetryDelaySeconds = config.GlobalMetadata.TryGetValue(ValidationRetryDelaySeconds, out var retryDelaySeconds) ? retryDelaySeconds.ToObject<int>() : DefaultValidationRetryDelaySeconds;
            MaxTransientFailureCount = config.GlobalMetadata.TryGetValue(ValidationMaxTransientFailureCount, out var maxTransientFailureCount) ? maxTransientFailureCount.ToObject<int>() : DefaultValidationMaxTransientFailureCount;
        }
    }
}
