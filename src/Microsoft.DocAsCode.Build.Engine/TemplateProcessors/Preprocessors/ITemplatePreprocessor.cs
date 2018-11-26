// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    public interface ITemplatePreprocessor
    {
        bool ContainsGetOptions { get; }

        bool ContainsModelTransformation { get; }

        object GetOptions(object model);

        object TransformModel(object model);

        string Path { get; }

        string Name { get; }
    }
}
