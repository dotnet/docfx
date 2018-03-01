// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.AzureMarkdownRewriterTool
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class RewriterToolArguments
    {
        public RewriterToolArguments(List<AzureTransformArguments> azureTransformArgumentsList, bool isMigration)
        {
            AzureTransformArgumentsList = azureTransformArgumentsList;
            IsMigration = isMigration;
        }

        /// <summary>
        /// Azure transform arguments array list
        /// </summary>
        [JsonProperty("azure_transform_arguments_list")]
        public List<AzureTransformArguments> AzureTransformArgumentsList { get; set; }

        /// <summary>
        /// Indicate whether it is used for migration
        /// </summary>
        [JsonProperty("is_migration")]
        public bool IsMigration { get; set; }
    }
}
