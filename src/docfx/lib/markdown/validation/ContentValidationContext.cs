// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Validations.DocFx.Adapter;

namespace Microsoft.Docs.Build
{
    internal class ContentValidationContext : IValidationContext
    {
        private const int ValidationRequestTimeoutInSeconds = 10;
        private const int ValidationMaxTries = 4;
        private const int ValidationRetryDelaySeconds = 3;
        private const int ValidationMaxTransientFailureCount = 20;

        public string DocfxVersion { get; set; }

        public string MarkdownRulesFilePath { get; set; }

        public string? MetadataRulesFilePath { get; set; }

        public string? RepositoryUrl { get; set; }

        public string? Branch { get; set; }

        public string ApiBase { get; set; }

        public TimeSpan RequestTimeout { get; set; }

        public int MaxTries { get; set; }

        public int RetryDelaySeconds { get; set; }

        public int MaxTransientFailureCount { get; set; }

        public string AllowlistFilePath { get; set; }

        public string DisallowlistFilePath { get; set; }

        public ContentValidationContext(string markdownValidationRulesPath, string allowlistsPath, string disallowlistsPath)
        {
            MarkdownRulesFilePath = markdownValidationRulesPath;
            RepositoryUrl = EnvironmentVariable.RepositoryUrl;
            Branch = EnvironmentVariable.RepositoryBranch;
            DocfxVersion = "v3";
            RequestTimeout = TimeSpan.FromSeconds(ValidationRequestTimeoutInSeconds);
            MaxTries = ValidationMaxTries;
            RetryDelaySeconds = ValidationRetryDelaySeconds;
            MaxTransientFailureCount = ValidationMaxTransientFailureCount;
            AllowlistFilePath = allowlistsPath;
            DisallowlistFilePath = disallowlistsPath;
            ApiBase = "";
        }
    }
}
