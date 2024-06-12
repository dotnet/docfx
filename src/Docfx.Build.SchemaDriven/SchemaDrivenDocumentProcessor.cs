// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Dynamic;
using Docfx.Build.Common;
using Docfx.Build.SchemaDriven.Processors;
using Docfx.Common;
using Docfx.MarkdigEngine;
using Docfx.Plugins;

namespace Docfx.Build.SchemaDriven;

public class SchemaDrivenDocumentProcessor : DisposableDocumentProcessor
{
    #region Fields
    private readonly string _schemaName;
    private readonly DocumentSchema _schema;
    private readonly bool _allowOverwrite;
    private readonly MarkdigMarkdownService _markdigMarkdownService;
    private readonly string _siteHostName;
    #endregion

    public SchemaValidator SchemaValidator { get; }
    #region Constructors

    public SchemaDrivenDocumentProcessor(
        DocumentSchema schema,
        ICompositionContainer container,
        MarkdigMarkdownService markdigMarkdownService,
        string siteHostName = null)
    {
        if (string.IsNullOrWhiteSpace(schema.Title))
        {
            throw new ArgumentException("Title for schema must not be empty");
        }

        ArgumentNullException.ThrowIfNull(markdigMarkdownService);

        _schemaName = schema.Title;
        _schema = schema;
        SchemaValidator = schema.Validator;
        _allowOverwrite = schema.AllowOverwrite;
        _markdigMarkdownService = markdigMarkdownService;
        if (container != null)
        {
            var commonSteps = container.GetExports<IDocumentBuildStep>(nameof(SchemaDrivenDocumentProcessor));
            var schemaSpecificSteps = container.GetExports<IDocumentBuildStep>($"{nameof(SchemaDrivenDocumentProcessor)}.{_schemaName}");
            BuildSteps = commonSteps.Union(schemaSpecificSteps).ToList();
        }
        _siteHostName = siteHostName;

    }

    #endregion

    #region IDocumentProcessor Members

    public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

    public override string Name => _schema.Title;

    public override ProcessingPriority GetProcessingPriority(FileAndType file)
    {
        switch (file.Type)
        {
            case DocumentType.Article:
                if (".yml".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase) ||
                    ".yaml".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
                {
                    var mime = YamlMime.ReadMime(file.File);
                    if (string.Equals(mime, YamlMime.YamlMimePrefix + _schemaName))
                    {
                        return ProcessingPriority.Normal;
                    }

                    return ProcessingPriority.NotSupported;
                }

                break;
            case DocumentType.Overwrite:
                if (_allowOverwrite && ".md".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
                {
                    return ProcessingPriority.Normal;
                }
                break;
            default:
                break;
        }
        return ProcessingPriority.NotSupported;
    }

    public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
    {
        switch (file.Type)
        {
            case DocumentType.Article:
                // TODO: Support dynamic in YAML deserializer
                try
                {
                    // MUST be a dictionary
                    var obj = YamlUtility.Deserialize<Dictionary<string, object>>(file.File);

                    // load overwrite fragments
                    string markdownFragmentsContent = null;
                    var markdownFragmentsFile = file.File + ".md";
                    if (EnvironmentContext.FileAbstractLayer.Exists(markdownFragmentsFile))
                    {
                        markdownFragmentsContent = EnvironmentContext.FileAbstractLayer.ReadAllText(markdownFragmentsFile);
                    }
                    else
                    {
                        // Validate against the schema first, only when markdown fragments don't exist
                        SchemaValidator.Validate(obj);
                    }

                    var content = ConvertToObjectHelper.ConvertToDynamic(obj);
                    if (_schema.MetadataReference.GetValue(content) is not IDictionary<string, object> pageMetadata)
                    {
                        pageMetadata = new ExpandoObject();
                        _schema.MetadataReference.SetValue(ref content, pageMetadata);
                    }
                    foreach (var (key, value) in metadata.OrderBy(item => item.Key))
                    {
                        pageMetadata[key] = value;
                    }

                    var localPathFromRoot = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, EnvironmentContext.FileAbstractLayer.GetPhysicalPath(file.File));

                    var fm = new FileModel(file, content)
                    {
                        LocalPathFromRoot = localPathFromRoot,
                        MarkdownFragmentsModel = new FileModel(
                        new FileAndType(
                            file.BaseDir,
                            markdownFragmentsFile,
                            DocumentType.MarkdownFragments,
                            file.SourceDir,
                            file.DestinationDir),
                        markdownFragmentsContent),
                    };
                    fm.Properties.Schema = _schema;
                    fm.Properties.Metadata = pageMetadata;
                    fm.MarkdownFragmentsModel.Properties.MarkdigMarkdownService = _markdigMarkdownService;
                    fm.MarkdownFragmentsModel.Properties.Metadata = pageMetadata;
                    if (markdownFragmentsContent != null)
                    {
                        fm.MarkdownFragmentsModel.LocalPathFromRoot = PathUtility.MakeRelativePath(
                            EnvironmentContext.BaseDirectory,
                            EnvironmentContext.FileAbstractLayer.GetPhysicalPath(markdownFragmentsFile));
                    }
                    return fm;
                }
                catch (YamlDotNet.Core.YamlException e)
                {
                    var message = $"{file.File} is not in supported format: {e.Message}";
                    Logger.LogError(message, code: ErrorCodes.Build.InvalidYamlFile);
                    throw new DocumentException(message, e);
                }
            case DocumentType.Overwrite:
                return OverwriteDocumentReader.Read(file);
            default:
                throw new NotSupportedException();
        }
    }

    public override SaveResult Save(FileModel model)
    {
        if (model.Type != DocumentType.Article)
        {
            throw new NotSupportedException();
        }

        var result = new SaveResult
        {
            DocumentType = model.DocumentType ?? _schemaName,
            FileWithoutExtension = Path.ChangeExtension(model.File, null),
            LinkToFiles = model.LinkToFiles.ToImmutableArray(),
            LinkToUids = model.LinkToUids,
            FileLinkSources = model.FileLinkSources,
            UidLinkSources = model.UidLinkSources,
            XRefSpecs = ImmutableArray.CreateRange(model.Properties.XRefSpecs),
            ExternalXRefSpecs = ImmutableArray.CreateRange(model.Properties.ExternalXRefSpecs)
        };

        if (((IDictionary<string, object>)model.Properties).TryGetValue("XrefSpec", out var value))
        {
            var xrefSpec = value as XRefSpec;
            if (xrefSpec != null)
            {
                result.XRefSpecs = [xrefSpec];
            }
        }

        return result;
    }

    public override void UpdateHref(FileModel model, IDocumentBuildContext context)
    {
        var content = model.Content;
        var pc = new ProcessContext(null, model, context);
        DocumentSchema schema = model.Properties.Schema;
        model.Content = new SchemaProcessor(
            new HrefInterpreter(false, true, _siteHostName),
            new FileInterpreter(false, true),
            new XrefInterpreter(false, true)
            ).Process(content, schema, pc);
    }

    #endregion
}
