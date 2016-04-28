// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    public class TemplatePreprocessorResource
    {
        public string Content { get; }
        public string ResourceName { get; }
        public TemplatePreprocessorResource(string resourceName, string content)
        {
            ResourceName = resourceName;
            Content = content;
        }
    }
}
