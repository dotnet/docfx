// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    public class TemplateTransformedResult
    {
        public string Result { get; }
        public object Model { get; }
        public TemplateTransformedResult(object model, string result)
        {
            Result = result;
            Model = model;
        }
    }
}
