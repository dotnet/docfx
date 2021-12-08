// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class LegacyContext
{
    public Config Config { get; }

    public BuildOptions BuildOptions { get; }

    public Output Output { get; }

    public SourceMap SourceMap { get; }

    public MonikerProvider MonikerProvider { get; }

    public DocumentProvider DocumentProvider { get; }

    public LegacyContext(
        Config config,
        BuildOptions buildOptions,
        Output output,
        SourceMap sourceMap,
        MonikerProvider monikerProvider,
        DocumentProvider documentProvider)
    {
        Config = config;
        BuildOptions = buildOptions;
        Output = output;
        SourceMap = sourceMap;
        MonikerProvider = monikerProvider;
        DocumentProvider = documentProvider;
    }
}
