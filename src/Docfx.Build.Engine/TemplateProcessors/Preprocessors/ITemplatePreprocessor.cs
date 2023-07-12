// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.Engine;

public interface ITemplatePreprocessor
{
    bool ContainsGetOptions { get; }

    bool ContainsModelTransformation { get; }

    object GetOptions(object model);

    object TransformModel(object model);

    string Path { get; }

    string Name { get; }
}
