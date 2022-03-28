// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class TocBuilder
{
    private readonly Config _config;
    private readonly TocLoader _tocLoader;
    private readonly ContentValidator _contentValidator;
    private readonly MetadataProvider _metadataProvider;
    private readonly MetadataValidator _metadataValidator;
    private readonly DocumentProvider _documentProvider;
    private readonly MonikerProvider _monikerProvider;
    private readonly PublishModelBuilder _publishModelBuilder;
    private readonly TemplateEngine _templateEngine;
    private readonly Output _output;

    public TocBuilder(
        Config config,
        TocLoader tocLoader,
        ContentValidator contentValidator,
        MetadataProvider metadataProvider,
        MetadataValidator metadataValidator,
        DocumentProvider documentProvider,
        MonikerProvider monikerProvider,
        PublishModelBuilder publishModelBuilder,
        TemplateEngine templateEngine,
        Output output)
    {
        _config = config;
        _tocLoader = tocLoader;
        _contentValidator = contentValidator;
        _metadataProvider = metadataProvider;
        _metadataValidator = metadataValidator;
        _documentProvider = documentProvider;
        _monikerProvider = monikerProvider;
        _publishModelBuilder = publishModelBuilder;
        _templateEngine = templateEngine;
        _output = output;
    }

    public void Build(ErrorBuilder errors, FilePath file)
    {
        // load toc tree
        var (node, _, _, _) = _tocLoader.Load(file);

        _contentValidator.ValidateTocDeprecated(file);

        var metadata = _metadataProvider.GetMetadata(errors, file);
        _metadataValidator.ValidateMetadata(errors, metadata.RawJObject, file);

        var tocMetadata = JsonUtility.ToObject<TocMetadata>(errors, metadata.RawJObject);

        var path = _documentProvider.GetSitePath(file);

        var model = new TocModel(node.Items.Select(item => item.Value).ToArray(), tocMetadata, path);

        var outputPath = _documentProvider.GetOutputPath(file);

        // enable pdf
        if (!_config.IsReferenceRepository && _config.OutputPdf)
        {
            var monikers = _monikerProvider.GetFileLevelMonikers(errors, file);
            model.Metadata.PdfAbsolutePath = "/" +
                UrlUtility.Combine(
                    _config.BasePath, "opbuildpdf", monikers.MonikerGroup ?? "", LegacyUtility.ChangeExtension(path, ".pdf"));
        }

        if (!errors.FileHasError(file) && !_config.DryRun)
        {
            if (_config.OutputType == OutputType.Json)
            {
                _output.WriteJson(outputPath, JsonUtility.ToJObject(model));
            }
            else if (_config.OutputType == OutputType.PageJson)
            {
                var output = _templateEngine.RunJavaScript("toc.json.js", JsonUtility.ToJObject(model));
                _output.WriteJson(outputPath, output);
            }
            else
            {
                if (_documentProvider.GetRenderType(file) == RenderType.Content)
                {
                    var viewModel = _templateEngine.RunJavaScript($"toc.html.js", JsonUtility.ToJObject(model));
                    var html = _templateEngine.RunMustache(errors, $"toc.html", viewModel);
                    _output.WriteText(outputPath, html);
                }

                // Just for current PDF build. toc.json is used for generate PDF outline
                var output = _templateEngine.RunJavaScript("toc.json.js", JsonUtility.ToJObject(model));
                _output.WriteJson(Path.ChangeExtension(outputPath, ".json"), output);
            }
        }

        _publishModelBuilder.AddOrUpdate(file, new JObject(), outputPath);
    }
}
