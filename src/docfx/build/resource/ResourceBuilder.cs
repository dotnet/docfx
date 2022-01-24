// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class ResourceBuilder
{
    private readonly Input _input;
    private readonly DocumentProvider _documentProvider;
    private readonly Config _config;
    private readonly Output _output;
    private readonly PublishModelBuilder _publishModelBuilder;

    public ResourceBuilder(Input input, DocumentProvider documentProvider, Config config, Output output, PublishModelBuilder publishModelBuilder)
    {
        _input = input;
        _documentProvider = documentProvider;
        _config = config;
        _output = output;
        _publishModelBuilder = publishModelBuilder;
    }

    public void Build(FilePath file)
    {
        var outputPath = _documentProvider.GetOutputPath(file);

        if (!_config.SelfContained && _input.TryGetPhysicalPath(file) is PathString physicalPath)
        {
            // Output path is source file path relative to output folder when copy resource is disabled
            outputPath = PathUtility.NormalizeFile(Path.GetRelativePath(_output.OutputPath, physicalPath));
        }
        else if (!_config.DryRun)
        {
            _output.Copy(outputPath, file);
        }

        _publishModelBuilder.AddOrUpdate(file, metadata: null, outputPath);
    }
}
