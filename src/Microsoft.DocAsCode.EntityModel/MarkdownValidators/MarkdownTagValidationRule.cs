// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.MarkdownValidators
{
    using System.Collections.Generic;

    public class MarkdownTagValidationRule
    {
        /// <summary>
        /// The names of tag.
        /// </summary>
        public List<string> TagNames { get; set; }
        /// <summary>
        /// Define tag's behavior.
        /// </summary>
        public TagRewriteBehavior Behavior { get; set; }
        /// <summary>
        /// The message formatter for warning and error. '{0}' is name of tag, '{1}' is the full tag.
        /// </summary>
        public string MessageFormatter { get; set; }
        /// <summary>
        /// The contract name for custom validator <see cref="Microsoft.DocAsCode.Plugins.ICustomMarkdownTagValidator"/>.
        /// </summary>
        public string CustomValidatorContractName { get; set; }
        /// <summary>
        /// Only validate opening tag.
        /// </summary>
        public bool OpeningTagOnly { get; set; }
    }
}
