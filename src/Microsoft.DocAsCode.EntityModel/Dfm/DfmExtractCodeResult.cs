// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    internal class DfmExtractCodeResult
    {
        public bool IsSuccessful { get; set; }

        public string[] FencesCodeLines { get; set; }

        public string ErrorMessage { get; set; }
    }
}