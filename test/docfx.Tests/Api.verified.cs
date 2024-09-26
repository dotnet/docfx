namespace Docfx
{
    public class BuildOptions
    {
        public BuildOptions() { }
        public System.Func<Markdig.MarkdownPipelineBuilder, Markdig.MarkdownPipelineBuilder>? ConfigureMarkdig { get; init; }
    }
    public static class Docset
    {
        public static System.Threading.Tasks.Task Build(string configPath) { }
        public static System.Threading.Tasks.Task Build(string configPath, Docfx.BuildOptions options) { }
        public static System.Threading.Tasks.Task Pdf(string configPath) { }
        public static System.Threading.Tasks.Task Pdf(string configPath, Docfx.BuildOptions options) { }
    }
}
namespace Docfx.Build.Engine
{
    [System.Flags]
    public enum ApplyTemplateOptions
    {
        None = 0,
        ExportRawModel = 1,
        ExportViewModel = 2,
        TransformDocument = 4,
        All = 7,
    }
    public class ApplyTemplateSettings
    {
        public static readonly Docfx.Build.Engine.ExportSettings DefaultRawModelExportSettings;
        public static readonly Docfx.Build.Engine.ExportSettings DefaultViewModelExportSettings;
        public ApplyTemplateSettings(string inputFolder, string outputFolder) { }
        public ApplyTemplateSettings(string inputFolder, string outputFolder, string debugOutputFolder, bool debugMode) { }
        public bool DebugMode { get; }
        public Docfx.Plugins.ICustomHrefGenerator HrefGenerator { get; set; }
        public string InputFolder { get; }
        public Docfx.Build.Engine.ApplyTemplateOptions Options { get; }
        public string OutputFolder { get; }
        public Docfx.Build.Engine.ExportSettings RawModelExportSettings { get; set; }
        public Docfx.Build.Engine.ExportSettings RawModelExportSettingsForDebug { get; set; }
        public bool TransformDocument { get; set; }
        public Docfx.Build.Engine.ExportSettings ViewModelExportSettings { get; set; }
        public Docfx.Build.Engine.ExportSettings ViewModelExportSettingsForDebug { get; set; }
    }
    public class BasicXRefMapReader : Docfx.Build.Engine.IXRefContainerReader
    {
        public BasicXRefMapReader(Docfx.Build.Engine.XRefMap map) { }
        protected Docfx.Build.Engine.XRefMap Map { get; }
        public virtual Docfx.Plugins.XRefSpec Find(string uid) { }
    }
    public sealed class CompositeResourceReader : Docfx.Build.Engine.ResourceFileReader, System.Collections.Generic.IEnumerable<Docfx.Build.Engine.ResourceFileReader>, System.Collections.IEnumerable
    {
        public CompositeResourceReader(System.Collections.Generic.IEnumerable<Docfx.Build.Engine.ResourceFileReader> declaredReaders) { }
        public override bool IsEmpty { get; }
        public override string Name { get; }
        public override System.Collections.Generic.IEnumerable<string> Names { get; }
        public System.Collections.Generic.IEnumerator<Docfx.Build.Engine.ResourceFileReader> GetEnumerator() { }
        public override System.IO.Stream GetResourceStream(string name) { }
    }
    public sealed class DocumentBuildContext : Docfx.Plugins.IDocumentBuildContext
    {
        public DocumentBuildContext(Docfx.Build.Engine.DocumentBuildParameters parameters, System.Threading.CancellationToken cancellationToken) { }
        public System.Collections.Immutable.ImmutableDictionary<string, Docfx.Plugins.FileAndType> AllSourceFiles { get; }
        public Docfx.Build.Engine.ApplyTemplateSettings ApplyTemplateSettings { get; set; }
        public string BuildOutputFolder { get; }
        public System.Threading.CancellationToken CancellationToken { get; }
        public System.Collections.Immutable.ImmutableArray<string> ExternalReferencePackages { get; }
        public System.Collections.Concurrent.ConcurrentDictionary<string, string> FileMap { get; }
        public Docfx.Plugins.GroupInfo GroupInfo { get; }
        public Docfx.Plugins.ICustomHrefGenerator HrefGenerator { get; }
        public Docfx.Plugins.IMarkdownService MarkdownService { get; set; }
        public int MaxParallelism { get; }
        public string RootTocPath { get; }
        public System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.HashSet<string>> TocMap { get; }
        public string VersionFolder { get; }
        public string VersionName { get; }
        public System.Collections.Generic.HashSet<string> XRef { get; }
        public System.Collections.Concurrent.ConcurrentDictionary<string, Docfx.Plugins.XRefSpec> XRefSpecMap { get; }
        public string GetFilePath(string key) { }
        public System.Collections.Immutable.IImmutableList<string> GetTocFileKeySet(string key) { }
        public System.Collections.Immutable.IImmutableList<Docfx.Plugins.TocInfo> GetTocInfo() { }
        public Docfx.Plugins.XRefSpec GetXrefSpec(string uid) { }
        public void RegisterInternalXrefSpec(Docfx.Plugins.XRefSpec xrefSpec) { }
        public void RegisterInternalXrefSpecBookmark(string uid, string bookmark) { }
        public void RegisterToc(string tocFileKey, string fileKey) { }
        public void RegisterTocInfo(Docfx.Plugins.TocInfo toc) { }
        public void ReportExternalXRefSpec(Docfx.Plugins.XRefSpec spec) { }
        public void ResolveExternalXRefSpec() { }
        public void ResolveExternalXRefSpecForNoneSpecsAsync() { }
        public void SetFilePath(string key, string filePath) { }
        public string UpdateHref(string href) { }
        public string UpdateHref(string href, Docfx.Common.RelativePath fromFile) { }
    }
    public class DocumentBuildParameters
    {
        public DocumentBuildParameters() { }
        public Docfx.Build.Engine.ApplyTemplateSettings ApplyTemplateSettings { get; set; }
        public System.Func<Markdig.MarkdownPipelineBuilder, Markdig.MarkdownPipelineBuilder> ConfigureMarkdig { get; set; }
        public string CustomLinkResolver { get; set; }
        public bool DisableGitFeatures { get; set; }
        public Docfx.Build.Engine.FileMetadata FileMetadata { get; set; }
        public Docfx.Build.Engine.FileCollection Files { get; set; }
        public Docfx.Plugins.GroupInfo GroupInfo { get; set; }
        public Docfx.Plugins.MarkdownServiceProperties MarkdownEngineParameters { get; set; }
        public int MaxParallelism { get; set; }
        public System.Collections.Immutable.ImmutableDictionary<string, object> Metadata { get; set; }
        public string OutputBaseDir { get; set; }
        public System.Collections.Immutable.ImmutableArray<string> PostProcessors { get; set; }
        public string RootTocPath { get; set; }
        public Docfx.Plugins.SitemapOptions SitemapOptions { get; set; }
        public string TemplateDir { get; set; }
        public Docfx.Build.Engine.TemplateManager TemplateManager { get; set; }
        public string VersionDir { get; set; }
        public string VersionName { get; set; }
        public System.Collections.Immutable.ImmutableArray<string> XRefMaps { get; set; }
        public Docfx.Build.Engine.DocumentBuildParameters Clone() { }
    }
    public class DocumentBuilder : System.IDisposable
    {
        public DocumentBuilder(System.Collections.Generic.IEnumerable<System.Reflection.Assembly> assemblies, System.Collections.Immutable.ImmutableArray<string> postProcessorNames) { }
        public void Build(Docfx.Build.Engine.DocumentBuildParameters parameter) { }
        public void Build(System.Collections.Generic.IList<Docfx.Build.Engine.DocumentBuildParameters> parameters, string outputDirectory, System.Threading.CancellationToken cancellationToken = default) { }
        public void Dispose() { }
    }
    public sealed class EmptyResourceReader : Docfx.Build.Engine.ResourceFileReader
    {
        public EmptyResourceReader() { }
        public override bool IsEmpty { get; }
        public override string Name { get; }
        public override System.Collections.Generic.IEnumerable<string> Names { get; }
        public override System.IO.Stream GetResourceStream(string name) { }
    }
    public class ExportSettings
    {
        public ExportSettings() { }
        public ExportSettings(Docfx.Build.Engine.ExportSettings settings) { }
        public bool Export { get; set; }
        public string Extension { get; set; }
        public string OutputFolder { get; set; }
        public System.Func<string, string> PathRewriter { get; set; }
    }
    public class FileCollection
    {
        public FileCollection(Docfx.Build.Engine.FileCollection collection) { }
        public FileCollection(string defaultBaseDir) { }
        public int Count { get; }
        public string DefaultBaseDir { get; set; }
        public void Add(Docfx.Plugins.DocumentType type, System.Collections.Generic.IEnumerable<string> files, string sourceDir = null, string destinationDir = null) { }
        public void Add(Docfx.Plugins.DocumentType type, string baseDir, System.Collections.Generic.IEnumerable<string> files, string sourceDir = null, string destinationDir = null) { }
        public System.Collections.Generic.IEnumerable<Docfx.Plugins.FileAndType> EnumerateFiles() { }
        public void RemoveAll(System.Predicate<Docfx.Plugins.FileAndType> match) { }
    }
    public sealed class FileMetadata : System.Collections.Generic.Dictionary<string, System.Collections.Immutable.ImmutableArray<Docfx.Build.Engine.FileMetadataItem>>
    {
        public FileMetadata(string baseDir) { }
        public FileMetadata(string baseDir, System.Collections.Generic.IDictionary<string, System.Collections.Immutable.ImmutableArray<Docfx.Build.Engine.FileMetadataItem>> dictionary) { }
        public string BaseDir { get; }
        public System.Collections.Generic.IEnumerable<Docfx.Glob.GlobMatcher> GetAllGlobs() { }
    }
    public static class FileMetadataHelper
    {
        public static System.Collections.Generic.IEnumerable<Docfx.Glob.GlobMatcher> GetChangedGlobs(this Docfx.Build.Engine.FileMetadata left, Docfx.Build.Engine.FileMetadata right) { }
    }
    public sealed class FileMetadataItem : System.IEquatable<Docfx.Build.Engine.FileMetadataItem>
    {
        public FileMetadataItem(Docfx.Glob.GlobMatcher glob, string key, object value) { }
        public Docfx.Glob.GlobMatcher Glob { get; }
        public string Key { get; }
        public object Value { get; }
        public bool Equals(Docfx.Build.Engine.FileMetadataItem other) { }
        public override bool Equals(object obj) { }
        public override int GetHashCode() { }
    }
    public abstract class HtmlDocumentHandler : Docfx.Build.Engine.IHtmlDocumentHandler
    {
        protected HtmlDocumentHandler() { }
        public void Handle(HtmlAgilityPack.HtmlDocument document, Docfx.Plugins.ManifestItem manifestItem, string inputFile, string outputFile) { }
        protected abstract void HandleCore(HtmlAgilityPack.HtmlDocument document, Docfx.Plugins.ManifestItem manifestItem, string inputFile, string outputFile);
        public Docfx.Plugins.Manifest PostHandle(Docfx.Plugins.Manifest manifest) { }
        protected virtual Docfx.Plugins.Manifest PostHandleCore(Docfx.Plugins.Manifest manifest) { }
        public Docfx.Plugins.Manifest PreHandle(Docfx.Plugins.Manifest manifest) { }
        protected virtual Docfx.Plugins.Manifest PreHandleCore(Docfx.Plugins.Manifest manifest) { }
    }
    public interface IHtmlDocumentHandler
    {
        void Handle(HtmlAgilityPack.HtmlDocument document, Docfx.Plugins.ManifestItem manifestItem, string inputFile, string outputFile);
        Docfx.Plugins.Manifest PostHandle(Docfx.Plugins.Manifest manifest);
        Docfx.Plugins.Manifest PreHandle(Docfx.Plugins.Manifest manifest);
    }
    public interface ITemplatePreprocessor
    {
        bool ContainsGetOptions { get; }
        bool ContainsModelTransformation { get; }
        string Name { get; }
        string Path { get; }
        object GetOptions(object model);
        object TransformModel(object model);
    }
    public interface ITemplateRenderer
    {
        System.Collections.Generic.IEnumerable<string> Dependencies { get; }
        string Name { get; }
        string Path { get; }
        string Render(object model);
    }
    public interface IXRefContainer
    {
        bool IsEmbeddedRedirections { get; }
        Docfx.Build.Engine.IXRefContainerReader GetReader();
        System.Collections.Generic.IEnumerable<Docfx.Build.Engine.XRefMapRedirection> GetRedirections();
    }
    public interface IXRefContainerReader
    {
        Docfx.Plugins.XRefSpec Find(string uid);
    }
    public class InvalidPreprocessorException : Docfx.Exceptions.DocfxException
    {
        public InvalidPreprocessorException(string message) { }
    }
    public static class JintProcessorHelper
    {
        public static Jint.Native.JsValue ConvertObjectToJsValue(Jint.Engine engine, object raw) { }
    }
    public sealed class LocalFileResourceReader : Docfx.Build.Engine.ResourceFileReader
    {
        public LocalFileResourceReader(string directory, int maxSearchLevel = 5) { }
        public override bool IsEmpty { get; }
        public override string Name { get; }
        public override System.Collections.Generic.IEnumerable<string> Names { get; }
        public override System.IO.Stream GetResourceStream(string name) { }
    }
    public static class MarkupUtility
    {
        public static Docfx.Plugins.MarkupResult Parse(Docfx.Plugins.MarkupResult markupResult, Docfx.Plugins.FileAndType ft, System.Collections.Immutable.ImmutableDictionary<string, Docfx.Plugins.FileAndType> sourceFiles) { }
        public static Docfx.Plugins.MarkupResult Parse(Docfx.Plugins.MarkupResult markupResult, string file, System.Collections.Immutable.ImmutableDictionary<string, Docfx.Plugins.FileAndType> sourceFiles) { }
    }
    public class PreprocessorLoader
    {
        public PreprocessorLoader(Docfx.Build.Engine.ResourceFileReader reader, Docfx.Build.Engine.DocumentBuildContext context, int maxParallelism) { }
        public Docfx.Build.Engine.ITemplatePreprocessor Load(Docfx.Build.Engine.ResourceInfo res, string name = null) { }
        public System.Collections.Generic.IEnumerable<Docfx.Build.Engine.ITemplatePreprocessor> LoadFromRenderer(Docfx.Build.Engine.ITemplateRenderer renderer) { }
        public System.Collections.Generic.IEnumerable<Docfx.Build.Engine.ITemplatePreprocessor> LoadStandalones() { }
    }
    public class RendererLoader
    {
        public RendererLoader(Docfx.Build.Engine.ResourceFileReader reader, int maxParallelism) { }
        public Docfx.Build.Engine.ITemplateRenderer Load(Docfx.Build.Engine.ResourceInfo res) { }
        public Docfx.Build.Engine.ITemplateRenderer Load(string path) { }
        public System.Collections.Generic.IEnumerable<Docfx.Build.Engine.ITemplateRenderer> LoadAll() { }
    }
    public abstract class ResourceFileReader
    {
        protected ResourceFileReader() { }
        public abstract bool IsEmpty { get; }
        public abstract string Name { get; }
        public abstract System.Collections.Generic.IEnumerable<string> Names { get; }
        public virtual string GetResource(string name) { }
        public abstract System.IO.Stream GetResourceStream(string name);
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.IO.Stream>> GetResourceStreams(string selector = null) { }
        public System.Collections.Generic.IEnumerable<Docfx.Build.Engine.ResourceInfo> GetResources(string selector = null) { }
        protected static string GetString(System.IO.Stream stream) { }
    }
    public class ResourceInfo
    {
        public ResourceInfo(string path, string content) { }
        public string Content { get; }
        public string Path { get; }
    }
    public class Template
    {
        public Template(Docfx.Build.Engine.ITemplateRenderer renderer, Docfx.Build.Engine.ITemplatePreprocessor preprocessor) { }
        public bool ContainsGetOptions { get; }
        public bool ContainsModelTransformation { get; }
        public string Extension { get; }
        public string Name { get; }
        public Docfx.Build.Engine.ITemplatePreprocessor Preprocessor { get; }
        public Docfx.Build.Engine.ITemplateRenderer Renderer { get; }
        public System.Collections.Generic.IEnumerable<Docfx.Build.Engine.TemplateResourceInfo> Resources { get; }
        public string ScriptName { get; }
        public Docfx.Build.Engine.TemplateType TemplateType { get; }
        public string Type { get; }
        public Docfx.Build.Engine.TransformModelOptions GetOptions(object model) { }
        public string Transform(object model) { }
        public object TransformModel(object model) { }
    }
    public class TemplateBundle
    {
        public TemplateBundle(string documentType, System.Collections.Generic.IEnumerable<Docfx.Build.Engine.Template> templates) { }
        public string DocumentType { get; }
        public string Extension { get; }
        public System.Collections.Generic.IEnumerable<Docfx.Build.Engine.TemplateResourceInfo> Resources { get; }
        public System.Collections.Generic.IEnumerable<Docfx.Build.Engine.Template> Templates { get; }
    }
    public class TemplateCollection : System.Collections.Generic.Dictionary<string, Docfx.Build.Engine.TemplateBundle>
    {
        public TemplateCollection(Docfx.Build.Engine.ResourceFileReader provider, Docfx.Build.Engine.DocumentBuildContext context, int maxParallelism) { }
        public Docfx.Build.Engine.TemplateBundle this[string key] { get; set; }
        public int MaxParallelism { get; }
        public Docfx.Build.Engine.ResourceFileReader Reader { get; }
    }
    public class TemplateJintPreprocessor : Docfx.Build.Engine.ITemplatePreprocessor
    {
        public const string Extension = ".js";
        public const string StandaloneExtension = ".tmpl.js";
        public TemplateJintPreprocessor(Docfx.Build.Engine.ResourceFileReader resourceCollection, Docfx.Build.Engine.ResourceInfo scriptResource, Docfx.Build.Engine.DocumentBuildContext context, string name = null) { }
        public bool ContainsGetOptions { get; }
        public bool ContainsModelTransformation { get; }
        public string Name { get; }
        public string Path { get; }
        public object GetOptions(object model) { }
        public object TransformModel(object model) { }
    }
    public class TemplateManager
    {
        public TemplateManager(System.Collections.Generic.List<string> templates, System.Collections.Generic.List<string>? themes, string? baseDirectory) { }
        public Docfx.Build.Engine.CompositeResourceReader CreateTemplateResource() { }
        public System.Collections.Generic.IEnumerable<string> GetTemplateDirectories() { }
        public Docfx.Build.Engine.TemplateProcessor GetTemplateProcessor(Docfx.Build.Engine.DocumentBuildContext context, int maxParallelism) { }
        public void ProcessTheme(string outputDirectory, bool overwrite) { }
        public bool TryExportTemplateFiles(string outputDirectory, string? regexFilter = null) { }
    }
    public class TemplateModelTransformer
    {
        public TemplateModelTransformer(Docfx.Build.Engine.DocumentBuildContext context, Docfx.Build.Engine.TemplateCollection templateCollection, Docfx.Build.Engine.ApplyTemplateSettings settings, System.Collections.Generic.IDictionary<string, object> globals) { }
    }
    public class TemplatePageLoader
    {
        public TemplatePageLoader(Docfx.Build.Engine.ResourceFileReader reader, Docfx.Build.Engine.DocumentBuildContext context, int maxParallelism) { }
        public System.Collections.Generic.IEnumerable<Docfx.Build.Engine.Template> LoadAll() { }
    }
    public class TemplateProcessor
    {
        public TemplateProcessor(Docfx.Build.Engine.ResourceFileReader resourceProvider, Docfx.Build.Engine.DocumentBuildContext context, int maxParallelism = 0) { }
        public System.Collections.Generic.IDictionary<string, string> Tokens { get; }
        public void CopyTemplateResources(Docfx.Build.Engine.ApplyTemplateSettings settings) { }
        public Docfx.Build.Engine.TemplateBundle GetTemplateBundle(string documentType) { }
        public bool TryGetFileExtension(string documentType, out string fileExtension) { }
    }
    public static class TemplateProcessorUtility
    {
        public static System.Collections.Generic.IDictionary<string, string> LoadTokens(Docfx.Build.Engine.ResourceFileReader resource) { }
    }
    public sealed class TemplateResourceInfo
    {
        public TemplateResourceInfo(string resourceKey) { }
        public string ResourceKey { get; }
        public override bool Equals(object obj) { }
        public override int GetHashCode() { }
    }
    public enum TemplateType
    {
        Default = 0,
        Primary = 1,
        Auxiliary = 2,
    }
    public class TemplateUtility
    {
        public TemplateUtility(Docfx.Build.Engine.DocumentBuildContext context) { }
        public string GetHrefFromRoot(string originalHref, string sourceFileKey) { }
        public string Markup(string markdown, string sourceFileKey) { }
        public string ResolveSourceRelativePath(string originPath, string currentFileOutputPath) { }
    }
    public class TransformModelOptions
    {
        public TransformModelOptions() { }
        [Newtonsoft.Json.JsonProperty(PropertyName="bookmarks")]
        [System.Text.Json.Serialization.JsonPropertyName("bookmarks")]
        public System.Collections.Generic.Dictionary<string, string> Bookmarks { get; set; }
        [Newtonsoft.Json.JsonProperty(PropertyName="isShared")]
        [System.Text.Json.Serialization.JsonPropertyName("isShared")]
        public bool IsShared { get; set; }
    }
    public sealed class XRefArchive : Docfx.Build.Engine.IXRefContainer, System.IDisposable
    {
        public const string MajorFileName = "xrefmap.yml";
        public System.Collections.Immutable.ImmutableList<string> Entries { get; }
        public string CreateMajor(Docfx.Build.Engine.XRefMap map) { }
        public string CreateMinor(Docfx.Build.Engine.XRefMap map, System.Collections.Generic.IEnumerable<string> names) { }
        public void Delete(string name) { }
        public void DeleteMajor() { }
        public void Dispose() { }
        public Docfx.Build.Engine.XRefMap Get(string name) { }
        public Docfx.Build.Engine.XRefMap GetMajor() { }
        public Docfx.Build.Engine.IXRefContainerReader GetReader() { }
        public bool HasEntry(string name) { }
        public void Update(string name, Docfx.Build.Engine.XRefMap map) { }
        public void UpdateMajor(Docfx.Build.Engine.XRefMap map) { }
        public static Docfx.Build.Engine.XRefArchive Open(string file, Docfx.Build.Engine.XRefArchiveMode mode) { }
    }
    public class XRefArchiveBuilder
    {
        public XRefArchiveBuilder() { }
        public System.Threading.Tasks.Task<bool> DownloadAsync(System.Uri uri, string outputFile, System.Threading.CancellationToken cancellationToken = default) { }
    }
    public enum XRefArchiveMode
    {
        Read = 0,
        Create = 1,
        Update = 2,
    }
    public class XRefArchiveReader : Docfx.Build.Engine.XRefRedirectionReader, System.IDisposable
    {
        public XRefArchiveReader(Docfx.Build.Engine.XRefArchive archive) { }
        public void Dispose() { }
        protected override Docfx.Build.Engine.IXRefContainer GetMap(string name) { }
    }
    public sealed class XRefDetails
    {
        public string Alt { get; }
        public string AltProperty { get; }
        public string Anchor { get; }
        public string DisplayProperty { get; }
        public string Href { get; }
        public string InnerHtml { get; }
        public string Query { get; }
        public string Raw { get; }
        public string RawSource { get; }
        public int SourceEndLineNumber { get; }
        public string SourceFile { get; }
        public int SourceStartLineNumber { get; }
        public Docfx.Plugins.XRefSpec Spec { get; }
        public string TemplatePath { get; }
        public string Text { get; }
        public bool ThrowIfNotResolved { get; }
        public string Title { get; }
        public string Uid { get; }
        public void ApplyXrefSpec(Docfx.Plugins.XRefSpec spec) { }
        [return: System.Runtime.CompilerServices.TupleElementNames(new string[] {
                null,
                "resolved"})]
        public System.ValueTuple<HtmlAgilityPack.HtmlNode, bool> ConvertToHtmlNode(string language, Docfx.Build.Engine.ITemplateRenderer renderer) { }
        public static HtmlAgilityPack.HtmlNode ConvertXrefLinkNodeToXrefNode(HtmlAgilityPack.HtmlNode node) { }
        public static Docfx.Build.Engine.XRefDetails From(HtmlAgilityPack.HtmlNode node) { }
    }
    public class XRefMap : Docfx.Build.Engine.IXRefContainer
    {
        public XRefMap() { }
        [Newtonsoft.Json.JsonProperty("baseUrl")]
        [System.Text.Json.Serialization.JsonPropertyName("baseUrl")]
        [YamlDotNet.Serialization.YamlMember(Alias="baseUrl")]
        public string BaseUrl { get; set; }
        [Newtonsoft.Json.JsonProperty("hrefUpdated")]
        [System.Text.Json.Serialization.JsonPropertyName("hrefUpdated")]
        [YamlDotNet.Serialization.YamlMember(Alias="hrefUpdated")]
        public bool? HrefUpdated { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public bool IsEmbeddedRedirections { get; }
        [Docfx.YamlSerialization.ExtensibleMember]
        [Newtonsoft.Json.JsonExtensionData]
        [System.Text.Json.Serialization.JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, object> Others { get; set; }
        [Newtonsoft.Json.JsonProperty("redirections")]
        [System.Text.Json.Serialization.JsonPropertyName("redirections")]
        [YamlDotNet.Serialization.YamlMember(Alias="redirections")]
        public System.Collections.Generic.List<Docfx.Build.Engine.XRefMapRedirection> Redirections { get; set; }
        [Newtonsoft.Json.JsonProperty("references")]
        [System.Text.Json.Serialization.JsonPropertyName("references")]
        [YamlDotNet.Serialization.YamlMember(Alias="references")]
        public System.Collections.Generic.List<Docfx.Plugins.XRefSpec> References { get; set; }
        [Newtonsoft.Json.JsonProperty("sorted")]
        [System.Text.Json.Serialization.JsonPropertyName("sorted")]
        [YamlDotNet.Serialization.YamlMember(Alias="sorted")]
        public bool? Sorted { get; set; }
        public Docfx.Build.Engine.IXRefContainerReader GetReader() { }
        public System.Collections.Generic.IEnumerable<Docfx.Build.Engine.XRefMapRedirection> GetRedirections() { }
        public void Sort() { }
        public void UpdateHref(System.Uri baseUri) { }
    }
    public sealed class XRefMapDownloader
    {
        public XRefMapDownloader(string baseFolder = null, System.Collections.Generic.IReadOnlyList<string> fallbackFolders = null, int maxParallelism = 16) { }
        public System.Threading.Tasks.Task<Docfx.Build.Engine.IXRefContainer> DownloadAsync(System.Uri uri, System.Threading.CancellationToken token = default) { }
    }
    public sealed class XRefMapReader : Docfx.Build.Engine.XRefRedirectionReader
    {
        public XRefMapReader(string majorKey, System.Collections.Generic.Dictionary<string, Docfx.Build.Engine.IXRefContainer> maps) { }
        protected override Docfx.Build.Engine.IXRefContainer GetMap(string name) { }
    }
    public class XRefMapRedirection
    {
        public XRefMapRedirection() { }
        [Newtonsoft.Json.JsonProperty("href")]
        [System.Text.Json.Serialization.JsonPropertyName("href")]
        [YamlDotNet.Serialization.YamlMember(Alias="href")]
        public string Href { get; set; }
        [Newtonsoft.Json.JsonProperty("uidPrefix")]
        [System.Text.Json.Serialization.JsonPropertyName("uidPrefix")]
        [YamlDotNet.Serialization.YamlMember(Alias="uidPrefix")]
        public string UidPrefix { get; set; }
    }
    public abstract class XRefRedirectionReader : Docfx.Build.Engine.IXRefContainerReader
    {
        protected XRefRedirectionReader(string majorName, System.Collections.Generic.HashSet<string> mapNames) { }
        public Docfx.Plugins.XRefSpec Find(string uid) { }
        protected abstract Docfx.Build.Engine.IXRefContainer GetMap(string name);
    }
    public sealed class XRefSpecUidComparer : System.Collections.Generic.Comparer<Docfx.Plugins.XRefSpec>
    {
        public static readonly Docfx.Build.Engine.XRefSpecUidComparer Instance;
        public XRefSpecUidComparer() { }
        public override int Compare(Docfx.Plugins.XRefSpec x, Docfx.Plugins.XRefSpec y) { }
    }
}
namespace Docfx.Build.ResourceFiles
{
    public interface IResourceFileConfig
    {
        bool IsResourceFile(string fileExtension);
    }
}
namespace Docfx.Build.TableOfContents
{
    public static class MarkdownTocReader
    {
        public static System.Collections.Generic.List<Docfx.DataContracts.Common.TocItemViewModel> LoadToc(string tocContent, string filePath) { }
    }
    public static class TocHelper
    {
        public static Docfx.DataContracts.Common.TocItemViewModel LoadSingleToc(string file) { }
    }
}
namespace Docfx.Build.Common
{
    public abstract class ApplyOverwriteDocument : Docfx.Build.Common.BaseDocumentBuildStep
    {
        protected ApplyOverwriteDocument() { }
        protected abstract void ApplyOverwrite(Docfx.Plugins.IHostService host, System.Collections.Generic.List<Docfx.Plugins.FileModel> overwrites, string uid, System.Collections.Generic.List<Docfx.Plugins.FileModel> articles);
        protected void ApplyOverwrite<T>(Docfx.Plugins.IHostService host, System.Collections.Generic.List<Docfx.Plugins.FileModel> overwrites, string uid, System.Collections.Generic.List<Docfx.Plugins.FileModel> articles, System.Func<Docfx.Plugins.FileModel, string, Docfx.Plugins.IHostService, System.Collections.Generic.IEnumerable<T>> getItemsFromOverwriteDocument, System.Func<Docfx.Plugins.FileModel, string, Docfx.Plugins.IHostService, System.Collections.Generic.IEnumerable<T>> getItemsToOverwrite)
            where T :  class, Docfx.DataContracts.Common.IOverwriteDocumentViewModel { }
        protected virtual void ApplyOverwrites(System.Collections.Immutable.ImmutableList<Docfx.Plugins.FileModel> models, Docfx.Plugins.IHostService host) { }
        protected virtual Docfx.Common.EntityMergers.IMerger GetMerger() { }
        public override void Postbuild(System.Collections.Immutable.ImmutableList<Docfx.Plugins.FileModel> models, Docfx.Plugins.IHostService host) { }
        protected System.Collections.Generic.IEnumerable<T> Transform<T>(Docfx.Plugins.FileModel model, string uid, Docfx.Plugins.IHostService host)
            where T :  class, Docfx.DataContracts.Common.IOverwriteDocumentViewModel { }
    }
    public abstract class BaseDocumentBuildStep : Docfx.Plugins.IDocumentBuildStep
    {
        protected BaseDocumentBuildStep() { }
        public abstract int BuildOrder { get; }
        public abstract string Name { get; }
        public virtual void Build(Docfx.Plugins.FileModel model, Docfx.Plugins.IHostService host) { }
        public virtual void Postbuild(System.Collections.Immutable.ImmutableList<Docfx.Plugins.FileModel> models, Docfx.Plugins.IHostService host) { }
        public virtual System.Collections.Generic.IEnumerable<Docfx.Plugins.FileModel> Prebuild(System.Collections.Immutable.ImmutableList<Docfx.Plugins.FileModel> models, Docfx.Plugins.IHostService host) { }
    }
    public abstract class BaseModelAttributeHandler<T> : Docfx.Build.Common.IModelAttributeHandler
        where T : System.Attribute
    {
        protected readonly Docfx.Build.Common.IModelAttributeHandler Handler;
        protected BaseModelAttributeHandler(System.Type type, Docfx.Build.Common.IModelAttributeHandler handler) { }
        protected virtual System.Collections.Generic.IEnumerable<Docfx.Build.Common.BaseModelAttributeHandler<T>.PropInfo> GetProps(System.Type type) { }
        public object Handle(object obj, Docfx.Build.Common.HandleModelAttributesContext context) { }
        protected abstract object HandleCurrent(object currentObj, object declaringObject, System.Reflection.PropertyInfo currentPropertyInfo, Docfx.Build.Common.HandleModelAttributesContext context);
        protected virtual object HandleDictionaryType(object currentObj, Docfx.Build.Common.HandleModelAttributesContext context) { }
        protected virtual object HandleIEnumerableType(object currentObj, Docfx.Build.Common.HandleModelAttributesContext context) { }
        protected virtual object ProcessNonPrimitiveType(object currentObj, Docfx.Build.Common.HandleModelAttributesContext context) { }
        protected virtual object ProcessPrimitiveType(object currentObj, Docfx.Build.Common.HandleModelAttributesContext context) { }
        protected virtual bool ShouldHandle(object currentObj, object declaringObject, Docfx.Build.Common.BaseModelAttributeHandler<T>.PropInfo currentPropInfo, Docfx.Build.Common.HandleModelAttributesContext context) { }
        protected sealed class PropInfo
        {
            public PropInfo() { }
            public System.Attribute Attr { get; set; }
            public System.Reflection.PropertyInfo Prop { get; set; }
        }
    }
    public abstract class BuildReferenceDocumentBase : Docfx.Build.Common.BaseDocumentBuildStep
    {
        protected BuildReferenceDocumentBase() { }
        public override int BuildOrder { get; }
        public override void Build(Docfx.Plugins.FileModel model, Docfx.Plugins.IHostService host) { }
        protected abstract void BuildArticle(Docfx.Plugins.IHostService host, Docfx.Plugins.FileModel model);
        protected virtual void BuildArticleCore(Docfx.Plugins.IHostService host, Docfx.Plugins.FileModel model, Docfx.Build.Common.IModelAttributeHandler handlers = null, Docfx.Build.Common.HandleModelAttributesContext handlerContext = null, bool shouldSkipMarkup = false) { }
        protected virtual void BuildOverwrite(Docfx.Plugins.IHostService host, Docfx.Plugins.FileModel model) { }
    }
    public class CompositeModelAttributeHandler : Docfx.Build.Common.IModelAttributeHandler
    {
        public CompositeModelAttributeHandler(params Docfx.Build.Common.IModelAttributeHandler[] handlers) { }
        public object Handle(object obj, Docfx.Build.Common.HandleModelAttributesContext context) { }
    }
    public abstract class DisposableDocumentProcessor : Docfx.Plugins.IDocumentProcessor, System.IDisposable
    {
        protected DisposableDocumentProcessor() { }
        public abstract string Name { get; }
        public abstract System.Collections.Generic.IEnumerable<Docfx.Plugins.IDocumentBuildStep> BuildSteps { get; set; }
        public void Dispose() { }
        public abstract Docfx.Plugins.ProcessingPriority GetProcessingPriority(Docfx.Plugins.FileAndType file);
        public abstract Docfx.Plugins.FileModel Load(Docfx.Plugins.FileAndType file, System.Collections.Immutable.ImmutableDictionary<string, object> metadata);
        public abstract Docfx.Plugins.SaveResult Save(Docfx.Plugins.FileModel model);
        public virtual void UpdateHref(Docfx.Plugins.FileModel model, Docfx.Plugins.IDocumentBuildContext context) { }
    }
    public class HandleGenericItemsHelper
    {
        public HandleGenericItemsHelper() { }
        public static bool EnumerateIDictionary(object currentObj, System.Func<object, object> handler) { }
        public static bool EnumerateIEnumerable(object currentObj, System.Func<object, object> handler) { }
        public static bool EnumerateIReadonlyDictionary(object currentObj, System.Func<object, object> handler) { }
        public static bool HandleIDictionary(object currentObj, System.Func<object, object> handler) { }
        public static bool HandleIList(object currentObj, System.Func<object, object> handler) { }
        public sealed class EnumerateIDictionaryItems<TKey, TValue>
        {
            public EnumerateIDictionaryItems(System.Collections.Generic.IDictionary<TKey, TValue> dict) { }
            public void Handle(System.Func<object, object> enumerate) { }
        }
        public sealed class EnumerateIEnumerableItems<TValue>
        {
            public EnumerateIEnumerableItems(System.Collections.Generic.IEnumerable<TValue> list) { }
            public void Handle(System.Func<object, object> enumerate) { }
        }
        public sealed class EnumerateIReadonlyDictionaryItems<TKey, TValue>
        {
            public EnumerateIReadonlyDictionaryItems(System.Collections.Generic.IReadOnlyDictionary<TKey, TValue> dict) { }
            public void Handle(System.Func<object, object> enumerate) { }
        }
        public sealed class HandleIDictionaryItems<TKey, TValue>
        {
            public HandleIDictionaryItems(System.Collections.Generic.IDictionary<TKey, TValue> dict) { }
            public void Handle(System.Func<object, object> handler) { }
        }
        public sealed class HandleIListItems<T>
        {
            public HandleIListItems(System.Collections.Generic.IList<T> list) { }
            public void Handle(System.Func<object, object> handler) { }
        }
    }
    public class HandleModelAttributesContext
    {
        public HandleModelAttributesContext() { }
        public bool ContainsPlaceholder { get; set; }
        public System.Collections.Generic.HashSet<string> Dependency { get; set; }
        public bool EnableContentPlaceholder { get; set; }
        public Docfx.Plugins.FileAndType FileAndType { get; set; }
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Docfx.Plugins.LinkSourceInfo>> FileLinkSources { get; set; }
        public Docfx.Plugins.IHostService Host { get; set; }
        public System.Collections.Generic.HashSet<string> LinkToFiles { get; set; }
        public System.Collections.Generic.HashSet<string> LinkToUids { get; set; }
        public string PlaceholderContent { get; set; }
        public bool SkipMarkup { get; set; }
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Docfx.Plugins.LinkSourceInfo>> UidLinkSources { get; set; }
        public System.Collections.Generic.List<Docfx.Plugins.UidDefinition> Uids { get; set; }
    }
    public interface IModelAttributeHandler
    {
        object Handle(object obj, Docfx.Build.Common.HandleModelAttributesContext context);
    }
    public class MarkdownContentHandler : Docfx.Build.Common.IModelAttributeHandler
    {
        public MarkdownContentHandler() { }
        public object Handle(object obj, Docfx.Build.Common.HandleModelAttributesContext context) { }
    }
    public class MarkdownReader
    {
        public MarkdownReader() { }
        public static System.Collections.Generic.Dictionary<string, object> ReadMarkdownAsConceptual(string file) { }
        public static System.Collections.Generic.IEnumerable<Docfx.Build.Common.OverwriteDocumentModel> ReadMarkdownAsOverwrite(Docfx.Plugins.IHostService host, Docfx.Plugins.FileAndType ft) { }
    }
    public class OverwriteDocumentModel
    {
        public OverwriteDocumentModel() { }
        [Newtonsoft.Json.JsonProperty("conceptual")]
        [System.Text.Json.Serialization.JsonPropertyName("conceptual")]
        [YamlDotNet.Serialization.YamlMember(Alias="conceptual")]
        public string Conceptual { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public System.Collections.Immutable.ImmutableArray<string> Dependency { get; set; }
        [Newtonsoft.Json.JsonProperty("documentation")]
        [System.Text.Json.Serialization.JsonPropertyName("documentation")]
        [YamlDotNet.Serialization.YamlMember(Alias="documentation")]
        public Docfx.DataContracts.Common.SourceDetail Documentation { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Docfx.Plugins.LinkSourceInfo>> FileLinkSources { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public System.Collections.Generic.HashSet<string> LinkToFiles { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public System.Collections.Generic.HashSet<string> LinkToUids { get; set; }
        [Docfx.YamlSerialization.ExtensibleMember]
        [Newtonsoft.Json.JsonExtensionData]
        [System.Text.Json.Serialization.JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; }
        [Newtonsoft.Json.JsonProperty("uid")]
        [System.Text.Json.Serialization.JsonPropertyName("uid")]
        [YamlDotNet.Serialization.YamlMember(Alias="uid")]
        public string Uid { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Docfx.Plugins.LinkSourceInfo>> UidLinkSources { get; set; }
        public T ConvertTo<T>()
            where T :  class { }
    }
    public class OverwriteDocumentReader
    {
        public OverwriteDocumentReader() { }
        public static Docfx.Plugins.FileModel Read(Docfx.Plugins.FileAndType file) { }
        public static System.Collections.Generic.IEnumerable<T> Transform<T>(Docfx.Plugins.FileModel model, string uid, System.Func<T, T> itemBuilder)
            where T :  class, Docfx.DataContracts.Common.IOverwriteDocumentViewModel { }
    }
    public abstract class ReferenceDocumentProcessorBase : Docfx.Build.Common.DisposableDocumentProcessor
    {
        protected ReferenceDocumentProcessorBase() { }
        protected abstract string ProcessedDocumentType { get; }
        public override Docfx.Plugins.FileModel Load(Docfx.Plugins.FileAndType file, System.Collections.Immutable.ImmutableDictionary<string, object> metadata) { }
        protected abstract Docfx.Plugins.FileModel LoadArticle(Docfx.Plugins.FileAndType file, System.Collections.Immutable.ImmutableDictionary<string, object> metadata);
        protected virtual Docfx.Plugins.FileModel LoadOverwrite(Docfx.Plugins.FileAndType file, System.Collections.Immutable.ImmutableDictionary<string, object> metadata) { }
        public override Docfx.Plugins.SaveResult Save(Docfx.Plugins.FileModel model) { }
    }
    public static class ReflectionHelper
    {
        public static object CreateInstance(System.Type type, System.Type[] typeArguments, System.Type[] argumentTypes, object[] arguments) { }
        public static System.Type GetGenericType(System.Type type, System.Type genericTypeDefinition) { }
        public static System.Collections.Generic.List<System.Reflection.PropertyInfo> GetGettableProperties(System.Type type) { }
        public static object GetPropertyValue(object instance, System.Reflection.PropertyInfo prop) { }
        public static System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> GetPublicProperties(System.Type type) { }
        public static System.Collections.Generic.List<System.Reflection.PropertyInfo> GetSettableProperties(System.Type type) { }
        public static bool ImplementsGenericDefinition(System.Type type, System.Type genericTypeDefinition) { }
        public static bool IsDictionaryType(System.Type type) { }
        public static bool IsGenericType(System.Type type, System.Type genericTypeDefinition) { }
        public static bool IsIEnumerableType(System.Type t) { }
        public static void SetPropertyValue(object instance, System.Reflection.PropertyInfo prop, object value) { }
        public static bool TryGetGenericType(System.Type type, System.Type genericTypeDefinition, out System.Type genericType) { }
    }
    public class UniqueIdentityReferenceHandler : Docfx.Build.Common.IModelAttributeHandler
    {
        public UniqueIdentityReferenceHandler() { }
        public object Handle(object obj, Docfx.Build.Common.HandleModelAttributesContext context) { }
    }
    public class UrlContentHandler : Docfx.Build.Common.IModelAttributeHandler
    {
        public UrlContentHandler() { }
        public object Handle(object obj, Docfx.Build.Common.HandleModelAttributesContext context) { }
    }
    public class YamlHtmlPart
    {
        public YamlHtmlPart() { }
        public string Conceptual { get; set; }
        public int EndLine { get; set; }
        public System.Collections.Immutable.ImmutableDictionary<string, System.Collections.Immutable.ImmutableList<Docfx.Plugins.LinkSourceInfo>> FileLinkSources { get; set; }
        public string Html { get; set; }
        public System.Collections.Immutable.ImmutableArray<string> LinkToFiles { get; set; }
        public System.Collections.Immutable.ImmutableHashSet<string> LinkToUids { get; set; }
        public Docfx.Plugins.MarkupResult Origin { get; set; }
        public string SourceFile { get; set; }
        public int StartLine { get; set; }
        public System.Collections.Immutable.ImmutableDictionary<string, System.Collections.Immutable.ImmutableList<Docfx.Plugins.LinkSourceInfo>> UidLinkSources { get; set; }
        public System.Collections.Immutable.ImmutableDictionary<string, object> YamlHeader { get; set; }
        public Docfx.Plugins.MarkupResult ToMarkupResult() { }
        public static System.Collections.Generic.IList<Docfx.Build.Common.YamlHtmlPart> SplitYamlHtml(Docfx.Plugins.MarkupResult origin) { }
    }
}
namespace Docfx.Build.ManagedReference
{
    [System.Composition.Export("ManagedReferenceDocumentProcessor", typeof(Docfx.Plugins.IDocumentBuildStep))]
    public class ApplyOverwriteDocumentForMref : Docfx.Build.Common.ApplyOverwriteDocument
    {
        public ApplyOverwriteDocumentForMref() { }
        public override int BuildOrder { get; }
        public override string Name { get; }
        protected override void ApplyOverwrite(Docfx.Plugins.IHostService host, System.Collections.Generic.List<Docfx.Plugins.FileModel> overwrites, string uid, System.Collections.Generic.List<Docfx.Plugins.FileModel> articles) { }
        public System.Collections.Generic.IEnumerable<Docfx.DataContracts.ManagedReference.ItemViewModel> GetItemsFromOverwriteDocument(Docfx.Plugins.FileModel fileModel, string uid, Docfx.Plugins.IHostService host) { }
        public static System.Collections.Generic.IEnumerable<Docfx.DataContracts.ManagedReference.ItemViewModel> GetItemsToOverwrite(Docfx.Plugins.FileModel fileModel, string uid, Docfx.Plugins.IHostService host) { }
    }
    public class ApplyPlatformVersion : Docfx.Build.Common.BaseDocumentBuildStep
    {
        public ApplyPlatformVersion() { }
        public override int BuildOrder { get; }
        public override string Name { get; }
        public override System.Collections.Generic.IEnumerable<Docfx.Plugins.FileModel> Prebuild(System.Collections.Immutable.ImmutableList<Docfx.Plugins.FileModel> models, Docfx.Plugins.IHostService host) { }
    }
    [System.Composition.Export("ManagedReferenceDocumentProcessor", typeof(Docfx.Plugins.IDocumentBuildStep))]
    public class BuildManagedReferenceDocument : Docfx.Build.Common.BuildReferenceDocumentBase
    {
        public BuildManagedReferenceDocument() { }
        public override string Name { get; }
        protected override void BuildArticle(Docfx.Plugins.IHostService host, Docfx.Plugins.FileModel model) { }
    }
    [System.Composition.Export("ManagedReferenceDocumentProcessor", typeof(Docfx.Plugins.IDocumentBuildStep))]
    public class FillReferenceInformation : Docfx.Build.Common.BaseDocumentBuildStep
    {
        public FillReferenceInformation() { }
        public override int BuildOrder { get; }
        public override string Name { get; }
        public override void Postbuild(System.Collections.Immutable.ImmutableList<Docfx.Plugins.FileModel> models, Docfx.Plugins.IHostService host) { }
    }
    [System.Composition.Export(typeof(Docfx.Plugins.IDocumentProcessor))]
    public class ManagedReferenceDocumentProcessor : Docfx.Build.Common.ReferenceDocumentProcessorBase
    {
        public ManagedReferenceDocumentProcessor() { }
        public override string Name { get; }
        protected override string ProcessedDocumentType { get; }
        [System.Composition.ImportMany("ManagedReferenceDocumentProcessor")]
        public override System.Collections.Generic.IEnumerable<Docfx.Plugins.IDocumentBuildStep> BuildSteps { get; set; }
        public override Docfx.Plugins.ProcessingPriority GetProcessingPriority(Docfx.Plugins.FileAndType file) { }
        protected override Docfx.Plugins.FileModel LoadArticle(Docfx.Plugins.FileAndType file, System.Collections.Immutable.ImmutableDictionary<string, object> metadata) { }
        public override Docfx.Plugins.SaveResult Save(Docfx.Plugins.FileModel model) { }
        protected virtual void UpdateModelContent(Docfx.Plugins.FileModel model) { }
    }
    public class MergeManagedReferenceDocument : Docfx.Build.Common.BaseDocumentBuildStep
    {
        public MergeManagedReferenceDocument() { }
        public override int BuildOrder { get; }
        public override string Name { get; }
        public override System.Collections.Generic.IEnumerable<Docfx.Plugins.FileModel> Prebuild(System.Collections.Immutable.ImmutableList<Docfx.Plugins.FileModel> models, Docfx.Plugins.IHostService host) { }
    }
    [System.Composition.Export("ManagedReferenceDocumentProcessor", typeof(Docfx.Plugins.IDocumentBuildStep))]
    public class SplitClassPageToMemberLevel : Docfx.Build.Common.BaseDocumentBuildStep
    {
        public SplitClassPageToMemberLevel() { }
        public override int BuildOrder { get; }
        public override string Name { get; }
        public override System.Collections.Generic.IEnumerable<Docfx.Plugins.FileModel> Prebuild(System.Collections.Immutable.ImmutableList<Docfx.Plugins.FileModel> models, Docfx.Plugins.IHostService host) { }
    }
    [System.Composition.Export("ManagedReferenceDocumentProcessor", typeof(Docfx.Plugins.IDocumentBuildStep))]
    public class ValidateManagedReferenceDocumentMetadata : Docfx.Build.Common.BaseDocumentBuildStep
    {
        public ValidateManagedReferenceDocumentMetadata() { }
        public override int BuildOrder { get; }
        public override string Name { get; }
        public override void Build(Docfx.Plugins.FileModel model, Docfx.Plugins.IHostService host) { }
    }
}
namespace Docfx.Build.ManagedReference.BuildOutputs
{
    public class ApiBuildOutput
    {
        public ApiBuildOutput() { }
        [Newtonsoft.Json.JsonProperty("additionalNotes")]
        [System.Text.Json.Serialization.JsonPropertyName("additionalNotes")]
        [YamlDotNet.Serialization.YamlMember(Alias="additionalNotes")]
        public Docfx.DataContracts.ManagedReference.AdditionalNotes AdditionalNotes { get; set; }
        [Newtonsoft.Json.JsonProperty("assemblies")]
        [System.Text.Json.Serialization.JsonPropertyName("assemblies")]
        [YamlDotNet.Serialization.YamlMember(Alias="assemblies")]
        public System.Collections.Generic.List<string> AssemblyNameList { get; set; }
        [Newtonsoft.Json.JsonProperty("attributes")]
        [System.Text.Json.Serialization.JsonPropertyName("attributes")]
        [YamlDotNet.Serialization.YamlMember(Alias="attributes")]
        public System.Collections.Generic.List<Docfx.DataContracts.ManagedReference.AttributeInfo> Attributes { get; set; }
        [Newtonsoft.Json.JsonProperty("children")]
        [System.Text.Json.Serialization.JsonPropertyName("children")]
        [YamlDotNet.Serialization.YamlMember(Alias="children")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> Children { get; set; }
        [Newtonsoft.Json.JsonProperty("conceptual")]
        [System.Text.Json.Serialization.JsonPropertyName("conceptual")]
        [YamlDotNet.Serialization.YamlMember(Alias="conceptual")]
        public string Conceptual { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.Ignore)]
        [Newtonsoft.Json.JsonProperty("derivedClasses")]
        [System.Text.Json.Serialization.JsonPropertyName("derivedClasses")]
        [YamlDotNet.Serialization.YamlMember(Alias="derivedClasses")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> DerivedClasses { get; set; }
        [Newtonsoft.Json.JsonProperty("documentation")]
        [System.Text.Json.Serialization.JsonPropertyName("documentation")]
        [YamlDotNet.Serialization.YamlMember(Alias="documentation")]
        public Docfx.DataContracts.Common.SourceDetail Documentation { get; set; }
        [Newtonsoft.Json.JsonProperty("example")]
        [System.Text.Json.Serialization.JsonPropertyName("example")]
        [YamlDotNet.Serialization.YamlMember(Alias="example")]
        public System.Collections.Generic.List<string> Examples { get; set; }
        [Newtonsoft.Json.JsonProperty("exceptions")]
        [System.Text.Json.Serialization.JsonPropertyName("exceptions")]
        [YamlDotNet.Serialization.YamlMember(Alias="exceptions")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiExceptionInfoBuildOutput> Exceptions { get; set; }
        [Newtonsoft.Json.JsonProperty("extensionMethods")]
        [System.Text.Json.Serialization.JsonPropertyName("extensionMethods")]
        [YamlDotNet.Serialization.YamlMember(Alias="extensionMethods")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> ExtensionMethods { get; set; }
        [Newtonsoft.Json.JsonProperty("fullName")]
        [System.Text.Json.Serialization.JsonPropertyName("fullName")]
        [YamlDotNet.Serialization.YamlMember(Alias="fullName")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLanguageValuePair> FullName { get; set; }
        [Newtonsoft.Json.JsonProperty("href")]
        [System.Text.Json.Serialization.JsonPropertyName("href")]
        [YamlDotNet.Serialization.YamlMember(Alias="href")]
        public string Href { get; set; }
        [Newtonsoft.Json.JsonProperty("implements")]
        [System.Text.Json.Serialization.JsonPropertyName("implements")]
        [YamlDotNet.Serialization.YamlMember(Alias="implements")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiNames> Implements { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.Ignore)]
        [Newtonsoft.Json.JsonProperty("inheritance")]
        [System.Text.Json.Serialization.JsonPropertyName("inheritance")]
        [YamlDotNet.Serialization.YamlMember(Alias="inheritance")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> Inheritance { get; set; }
        [Newtonsoft.Json.JsonProperty("inheritedMembers")]
        [System.Text.Json.Serialization.JsonPropertyName("inheritedMembers")]
        [YamlDotNet.Serialization.YamlMember(Alias="inheritedMembers")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> InheritedMembers { get; set; }
        [Newtonsoft.Json.JsonProperty("isEii")]
        [System.Text.Json.Serialization.JsonPropertyName("isEii")]
        [YamlDotNet.Serialization.YamlMember(Alias="isEii")]
        public bool IsExplicitInterfaceImplementation { get; set; }
        [Newtonsoft.Json.JsonProperty("isExtensionMethod")]
        [System.Text.Json.Serialization.JsonPropertyName("isExtensionMethod")]
        [YamlDotNet.Serialization.YamlMember(Alias="isExtensionMethod")]
        public bool IsExtensionMethod { get; set; }
        [Newtonsoft.Json.JsonProperty("level")]
        [System.Text.Json.Serialization.JsonPropertyName("level")]
        [YamlDotNet.Serialization.YamlMember(Alias="level")]
        public int Level { get; }
        [Docfx.YamlSerialization.ExtensibleMember]
        [Newtonsoft.Json.JsonExtensionData]
        [System.Text.Json.Serialization.JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; }
        [Newtonsoft.Json.JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        [YamlDotNet.Serialization.YamlMember(Alias="name")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLanguageValuePair> Name { get; set; }
        [Newtonsoft.Json.JsonProperty("nameWithType")]
        [System.Text.Json.Serialization.JsonPropertyName("nameWithType")]
        [YamlDotNet.Serialization.YamlMember(Alias="nameWithType")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLanguageValuePair> NameWithType { get; set; }
        [Newtonsoft.Json.JsonProperty("namespace")]
        [System.Text.Json.Serialization.JsonPropertyName("namespace")]
        [YamlDotNet.Serialization.YamlMember(Alias="namespace")]
        public Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput NamespaceName { get; set; }
        [Newtonsoft.Json.JsonProperty("overload")]
        [System.Text.Json.Serialization.JsonPropertyName("overload")]
        [YamlDotNet.Serialization.YamlMember(Alias="overload")]
        public Docfx.Build.ManagedReference.BuildOutputs.ApiNames Overload { get; set; }
        [Newtonsoft.Json.JsonProperty("overridden")]
        [System.Text.Json.Serialization.JsonPropertyName("overridden")]
        [YamlDotNet.Serialization.YamlMember(Alias="overridden")]
        public Docfx.Build.ManagedReference.BuildOutputs.ApiNames Overridden { get; set; }
        [Newtonsoft.Json.JsonProperty("parent")]
        [System.Text.Json.Serialization.JsonPropertyName("parent")]
        [YamlDotNet.Serialization.YamlMember(Alias="parent")]
        public Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput Parent { get; set; }
        [Newtonsoft.Json.JsonProperty("platform")]
        [System.Text.Json.Serialization.JsonPropertyName("platform")]
        [YamlDotNet.Serialization.YamlMember(Alias="platform")]
        public System.Collections.Generic.List<string> Platform { get; set; }
        [Newtonsoft.Json.JsonProperty("remarks")]
        [System.Text.Json.Serialization.JsonPropertyName("remarks")]
        [YamlDotNet.Serialization.YamlMember(Alias="remarks")]
        public string Remarks { get; set; }
        [Newtonsoft.Json.JsonProperty("seealso")]
        [System.Text.Json.Serialization.JsonPropertyName("seealso")]
        [YamlDotNet.Serialization.YamlMember(Alias="seealso")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLinkInfoBuildOutput> SeeAlsos { get; set; }
        [Newtonsoft.Json.JsonProperty("source")]
        [System.Text.Json.Serialization.JsonPropertyName("source")]
        [YamlDotNet.Serialization.YamlMember(Alias="source")]
        public Docfx.DataContracts.Common.SourceDetail Source { get; set; }
        [Newtonsoft.Json.JsonProperty("summary")]
        [System.Text.Json.Serialization.JsonPropertyName("summary")]
        [YamlDotNet.Serialization.YamlMember(Alias="summary")]
        public string Summary { get; set; }
        [Newtonsoft.Json.JsonProperty("langs")]
        [System.Text.Json.Serialization.JsonPropertyName("langs")]
        [YamlDotNet.Serialization.YamlMember(Alias="langs")]
        public string[] SupportedLanguages { get; set; }
        [Newtonsoft.Json.JsonProperty("syntax")]
        [System.Text.Json.Serialization.JsonPropertyName("syntax")]
        [YamlDotNet.Serialization.YamlMember(Alias="syntax")]
        public Docfx.Build.ManagedReference.BuildOutputs.ApiSyntaxBuildOutput Syntax { get; set; }
        [Newtonsoft.Json.JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        [YamlDotNet.Serialization.YamlMember(Alias="type")]
        public Docfx.DataContracts.ManagedReference.MemberType? Type { get; set; }
        [Newtonsoft.Json.JsonProperty("uid")]
        [System.Text.Json.Serialization.JsonPropertyName("uid")]
        [YamlDotNet.Serialization.YamlMember(Alias="uid")]
        public string Uid { get; set; }
        public static Docfx.Build.ManagedReference.BuildOutputs.ApiBuildOutput FromModel(Docfx.DataContracts.ManagedReference.PageViewModel model) { }
    }
    public static class ApiBuildOutputUtility
    {
        public static Docfx.Build.ManagedReference.BuildOutputs.ApiNames GetApiNames(string key, System.Collections.Generic.Dictionary<string, Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> references, string[] supportedLanguages) { }
        public static string GetHref(string url, string altText = null) { }
        public static Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput GetReferenceViewModel(string key, System.Collections.Generic.Dictionary<string, Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> references, string[] supportedLanguages) { }
        public static Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput GetReferenceViewModel(string key, System.Collections.Generic.Dictionary<string, Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> references, string[] supportedLanguages, int index) { }
        public static System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLanguageValuePair> GetSpec(string key, System.Collections.Generic.Dictionary<string, Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> references, string[] supportedLanguages) { }
        public static string GetXref(string uid, string text = null) { }
        public static System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLanguageValuePair> TransformToLanguagePairList(string defaultValue, System.Collections.Generic.SortedList<string, string> values, string[] supportedLanguages) { }
    }
    public class ApiExceptionInfoBuildOutput
    {
        public ApiExceptionInfoBuildOutput() { }
        [Newtonsoft.Json.JsonProperty("description")]
        [System.Text.Json.Serialization.JsonPropertyName("description")]
        [YamlDotNet.Serialization.YamlMember(Alias="description")]
        public string Description { get; set; }
        [Newtonsoft.Json.JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        [YamlDotNet.Serialization.YamlMember(Alias="type")]
        public Docfx.Build.ManagedReference.BuildOutputs.ApiNames Type { get; set; }
        public void Expand(System.Collections.Generic.Dictionary<string, Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> references, string[] supportedLanguages) { }
        public static Docfx.Build.ManagedReference.BuildOutputs.ApiExceptionInfoBuildOutput FromModel(Docfx.DataContracts.ManagedReference.ExceptionInfo model) { }
        public static Docfx.Build.ManagedReference.BuildOutputs.ApiExceptionInfoBuildOutput FromModel(Docfx.DataContracts.ManagedReference.ExceptionInfo model, System.Collections.Generic.Dictionary<string, Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> references, string[] supportedLanguages) { }
    }
    public class ApiLanguageValuePair
    {
        public ApiLanguageValuePair() { }
        [Newtonsoft.Json.JsonProperty("lang")]
        [System.Text.Json.Serialization.JsonPropertyName("lang")]
        [YamlDotNet.Serialization.YamlMember(Alias="lang")]
        public string Language { get; set; }
        [Newtonsoft.Json.JsonProperty("value")]
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        [YamlDotNet.Serialization.YamlMember(Alias="value")]
        public string Value { get; set; }
    }
    public class ApiLinkInfoBuildOutput
    {
        public ApiLinkInfoBuildOutput() { }
        [Newtonsoft.Json.JsonProperty("linkType")]
        [System.Text.Json.Serialization.JsonPropertyName("linkType")]
        [YamlDotNet.Serialization.YamlMember(Alias="linkType")]
        public Docfx.DataContracts.ManagedReference.LinkType LinkType { get; set; }
        [Newtonsoft.Json.JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        [YamlDotNet.Serialization.YamlMember(Alias="type")]
        public Docfx.Build.ManagedReference.BuildOutputs.ApiNames Type { get; set; }
        [Newtonsoft.Json.JsonProperty("url")]
        [System.Text.Json.Serialization.JsonPropertyName("url")]
        [YamlDotNet.Serialization.YamlMember(Alias="url")]
        public string Url { get; set; }
        public void Expand(System.Collections.Generic.Dictionary<string, Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> references, string[] supportedLanguages) { }
        public static Docfx.Build.ManagedReference.BuildOutputs.ApiLinkInfoBuildOutput FromModel(Docfx.DataContracts.ManagedReference.LinkInfo model) { }
        public static Docfx.Build.ManagedReference.BuildOutputs.ApiLinkInfoBuildOutput FromModel(Docfx.DataContracts.ManagedReference.LinkInfo model, System.Collections.Generic.Dictionary<string, Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> references, string[] supportedLanguages) { }
    }
    public class ApiNames
    {
        public ApiNames() { }
        [Newtonsoft.Json.JsonProperty("definition")]
        [System.Text.Json.Serialization.JsonPropertyName("definition")]
        [YamlDotNet.Serialization.YamlMember(Alias="definition")]
        public string Definition { get; set; }
        [Newtonsoft.Json.JsonProperty("fullName")]
        [System.Text.Json.Serialization.JsonPropertyName("fullName")]
        [YamlDotNet.Serialization.YamlMember(Alias="fullName")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLanguageValuePair> FullName { get; set; }
        [Newtonsoft.Json.JsonProperty("id")]
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        [YamlDotNet.Serialization.YamlMember(Alias="id")]
        public string Id { get; set; }
        [Newtonsoft.Json.JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        [YamlDotNet.Serialization.YamlMember(Alias="name")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLanguageValuePair> Name { get; set; }
        [Newtonsoft.Json.JsonProperty("nameWithType")]
        [System.Text.Json.Serialization.JsonPropertyName("nameWithType")]
        [YamlDotNet.Serialization.YamlMember(Alias="nameWithType")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLanguageValuePair> NameWithType { get; set; }
        [Newtonsoft.Json.JsonProperty("specName")]
        [System.Text.Json.Serialization.JsonPropertyName("specName")]
        [YamlDotNet.Serialization.YamlMember(Alias="specName")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLanguageValuePair> Spec { get; set; }
        [Newtonsoft.Json.JsonProperty("uid")]
        [System.Text.Json.Serialization.JsonPropertyName("uid")]
        [YamlDotNet.Serialization.YamlMember(Alias="uid")]
        public string Uid { get; set; }
        public static Docfx.Build.ManagedReference.BuildOutputs.ApiNames FromUid(string uid) { }
    }
    public class ApiParameterBuildOutput
    {
        public ApiParameterBuildOutput() { }
        [Newtonsoft.Json.JsonProperty("description")]
        [System.Text.Json.Serialization.JsonPropertyName("description")]
        [YamlDotNet.Serialization.YamlMember(Alias="description")]
        public string Description { get; set; }
        [Newtonsoft.Json.JsonProperty("id")]
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        [YamlDotNet.Serialization.YamlMember(Alias="id")]
        public string Name { get; set; }
        [Newtonsoft.Json.JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        [YamlDotNet.Serialization.YamlMember(Alias="type")]
        public Docfx.Build.ManagedReference.BuildOutputs.ApiNames Type { get; set; }
        public void Expand(System.Collections.Generic.Dictionary<string, Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> references, string[] supportedLanguages) { }
        public static Docfx.Build.ManagedReference.BuildOutputs.ApiParameterBuildOutput FromModel(Docfx.DataContracts.ManagedReference.ApiParameter model) { }
        public static Docfx.Build.ManagedReference.BuildOutputs.ApiParameterBuildOutput FromModel(Docfx.DataContracts.ManagedReference.ApiParameter model, System.Collections.Generic.Dictionary<string, Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> references, string[] supportedLanguages) { }
    }
    public class ApiReferenceBuildOutput
    {
        public ApiReferenceBuildOutput() { }
        [Newtonsoft.Json.JsonProperty("additionalNotes")]
        [System.Text.Json.Serialization.JsonPropertyName("additionalNotes")]
        [YamlDotNet.Serialization.YamlMember(Alias="additionalNotes")]
        public Docfx.DataContracts.ManagedReference.AdditionalNotes AdditionalNotes { get; set; }
        [Newtonsoft.Json.JsonProperty("assemblies")]
        [System.Text.Json.Serialization.JsonPropertyName("assemblies")]
        [YamlDotNet.Serialization.YamlMember(Alias="assemblies")]
        public System.Collections.Generic.List<string> AssemblyNameList { get; set; }
        [Newtonsoft.Json.JsonProperty("attributes")]
        [System.Text.Json.Serialization.JsonPropertyName("attributes")]
        [YamlDotNet.Serialization.YamlMember(Alias="attributes")]
        public System.Collections.Generic.List<Docfx.DataContracts.ManagedReference.AttributeInfo> Attributes { get; set; }
        [Newtonsoft.Json.JsonProperty("conceptual")]
        [System.Text.Json.Serialization.JsonPropertyName("conceptual")]
        [YamlDotNet.Serialization.YamlMember(Alias="conceptual")]
        public string Conceptual { get; set; }
        [Newtonsoft.Json.JsonProperty("definition")]
        [System.Text.Json.Serialization.JsonPropertyName("definition")]
        [YamlDotNet.Serialization.YamlMember(Alias="definition")]
        public string Definition { get; set; }
        [Newtonsoft.Json.JsonProperty("documentation")]
        [System.Text.Json.Serialization.JsonPropertyName("documentation")]
        [YamlDotNet.Serialization.YamlMember(Alias="documentation")]
        public Docfx.DataContracts.Common.SourceDetail Documentation { get; set; }
        [Newtonsoft.Json.JsonProperty("example")]
        [System.Text.Json.Serialization.JsonPropertyName("example")]
        [YamlDotNet.Serialization.YamlMember(Alias="example")]
        public System.Collections.Generic.List<string> Examples { get; set; }
        [Newtonsoft.Json.JsonProperty("exceptions")]
        [System.Text.Json.Serialization.JsonPropertyName("exceptions")]
        [YamlDotNet.Serialization.YamlMember(Alias="exceptions")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiExceptionInfoBuildOutput> Exceptions { get; set; }
        [Newtonsoft.Json.JsonProperty("extensionMethods")]
        [System.Text.Json.Serialization.JsonPropertyName("extensionMethods")]
        [YamlDotNet.Serialization.YamlMember(Alias="extensionMethods")]
        public System.Collections.Generic.List<string> ExtensionMethods { get; set; }
        [Newtonsoft.Json.JsonProperty("fullName")]
        [System.Text.Json.Serialization.JsonPropertyName("fullName")]
        [YamlDotNet.Serialization.YamlMember(Alias="fullName")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLanguageValuePair> FullName { get; set; }
        [Newtonsoft.Json.JsonProperty("href")]
        [System.Text.Json.Serialization.JsonPropertyName("href")]
        [YamlDotNet.Serialization.YamlMember(Alias="href")]
        public string Href { get; set; }
        [Newtonsoft.Json.JsonProperty("implements")]
        [System.Text.Json.Serialization.JsonPropertyName("implements")]
        [YamlDotNet.Serialization.YamlMember(Alias="implements")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiNames> Implements { get; set; }
        [Newtonsoft.Json.JsonProperty("index")]
        [System.Text.Json.Serialization.JsonPropertyName("index")]
        [YamlDotNet.Serialization.YamlMember(Alias="index")]
        public int? Index { get; set; }
        [Newtonsoft.Json.JsonProperty("inheritance")]
        [System.Text.Json.Serialization.JsonPropertyName("inheritance")]
        [YamlDotNet.Serialization.YamlMember(Alias="inheritance")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> Inheritance { get; set; }
        [Newtonsoft.Json.JsonProperty("inheritedMembers")]
        [System.Text.Json.Serialization.JsonPropertyName("inheritedMembers")]
        [YamlDotNet.Serialization.YamlMember(Alias="inheritedMembers")]
        public System.Collections.Generic.List<string> InheritedMembers { get; set; }
        [Newtonsoft.Json.JsonProperty("isEii")]
        [System.Text.Json.Serialization.JsonPropertyName("isEii")]
        [YamlDotNet.Serialization.YamlMember(Alias="isEii")]
        public bool IsExplicitInterfaceImplementation { get; set; }
        [Newtonsoft.Json.JsonProperty("isExtensionMethod")]
        [System.Text.Json.Serialization.JsonPropertyName("isExtensionMethod")]
        [YamlDotNet.Serialization.YamlMember(Alias="isExtensionMethod")]
        public bool IsExtensionMethod { get; set; }
        [Newtonsoft.Json.JsonProperty("isExternal")]
        [System.Text.Json.Serialization.JsonPropertyName("isExternal")]
        [YamlDotNet.Serialization.YamlMember(Alias="isExternal")]
        public bool? IsExternal { get; set; }
        [Newtonsoft.Json.JsonProperty("level")]
        [System.Text.Json.Serialization.JsonPropertyName("level")]
        [YamlDotNet.Serialization.YamlMember(Alias="level")]
        public int Level { get; }
        [Docfx.YamlSerialization.ExtensibleMember]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; }
        [Newtonsoft.Json.JsonExtensionData]
        [System.Text.Json.Serialization.JsonExtensionData]
        [YamlDotNet.Serialization.YamlIgnore]
        public Docfx.Common.CompositeDictionary MetadataJson { get; }
        [Newtonsoft.Json.JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        [YamlDotNet.Serialization.YamlMember(Alias="name")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLanguageValuePair> Name { get; set; }
        [Newtonsoft.Json.JsonProperty("nameWithType")]
        [System.Text.Json.Serialization.JsonPropertyName("nameWithType")]
        [YamlDotNet.Serialization.YamlMember(Alias="nameWithType")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLanguageValuePair> NameWithType { get; set; }
        [Newtonsoft.Json.JsonProperty("namespace")]
        [System.Text.Json.Serialization.JsonPropertyName("namespace")]
        [YamlDotNet.Serialization.YamlMember(Alias="namespace")]
        public string NamespaceName { get; set; }
        [Newtonsoft.Json.JsonProperty("overload")]
        [System.Text.Json.Serialization.JsonPropertyName("overload")]
        [YamlDotNet.Serialization.YamlMember(Alias="overload")]
        public Docfx.Build.ManagedReference.BuildOutputs.ApiNames Overload { get; set; }
        [Newtonsoft.Json.JsonProperty("overridden")]
        [System.Text.Json.Serialization.JsonPropertyName("overridden")]
        [YamlDotNet.Serialization.YamlMember(Alias="overridden")]
        public Docfx.Build.ManagedReference.BuildOutputs.ApiNames Overridden { get; set; }
        [Newtonsoft.Json.JsonProperty("parent")]
        [System.Text.Json.Serialization.JsonPropertyName("parent")]
        [YamlDotNet.Serialization.YamlMember(Alias="parent")]
        public string Parent { get; set; }
        [Newtonsoft.Json.JsonProperty("remarks")]
        [System.Text.Json.Serialization.JsonPropertyName("remarks")]
        [YamlDotNet.Serialization.YamlMember(Alias="remarks")]
        public string Remarks { get; set; }
        [Newtonsoft.Json.JsonProperty("seealso")]
        [System.Text.Json.Serialization.JsonPropertyName("seealso")]
        [YamlDotNet.Serialization.YamlMember(Alias="seealso")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLinkInfoBuildOutput> SeeAlsos { get; set; }
        [Newtonsoft.Json.JsonProperty("source")]
        [System.Text.Json.Serialization.JsonPropertyName("source")]
        [YamlDotNet.Serialization.YamlMember(Alias="source")]
        public Docfx.DataContracts.Common.SourceDetail Source { get; set; }
        [Newtonsoft.Json.JsonProperty("specName")]
        [System.Text.Json.Serialization.JsonPropertyName("specName")]
        [YamlDotNet.Serialization.YamlMember(Alias="specName")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLanguageValuePair> Spec { get; set; }
        [Newtonsoft.Json.JsonProperty("syntax")]
        [System.Text.Json.Serialization.JsonPropertyName("syntax")]
        [YamlDotNet.Serialization.YamlMember(Alias="syntax")]
        public Docfx.Build.ManagedReference.BuildOutputs.ApiSyntaxBuildOutput Syntax { get; set; }
        [Newtonsoft.Json.JsonProperty("uid")]
        [System.Text.Json.Serialization.JsonPropertyName("uid")]
        [YamlDotNet.Serialization.YamlMember(Alias="uid")]
        public string Uid { get; set; }
        public void Expand(System.Collections.Generic.Dictionary<string, Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> references, string[] supportedLanguages) { }
        public static Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput FromModel(Docfx.DataContracts.ManagedReference.ItemViewModel vm) { }
        public static Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput FromModel(Docfx.DataContracts.Common.ReferenceViewModel vm, string[] supportedLanguages) { }
        public static Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput FromUid(string uid) { }
        public static System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLanguageValuePair> GetSpecNames(string xref, string[] supportedLanguages, System.Collections.Generic.SortedList<string, System.Collections.Generic.List<Docfx.DataContracts.Common.SpecViewModel>> specs = null) { }
    }
    public class ApiSyntaxBuildOutput
    {
        public ApiSyntaxBuildOutput() { }
        [Newtonsoft.Json.JsonProperty("content")]
        [System.Text.Json.Serialization.JsonPropertyName("content")]
        [YamlDotNet.Serialization.YamlMember(Alias="content")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiLanguageValuePair> Content { get; set; }
        [Newtonsoft.Json.JsonProperty("parameters")]
        [System.Text.Json.Serialization.JsonPropertyName("parameters")]
        [YamlDotNet.Serialization.YamlMember(Alias="parameters")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiParameterBuildOutput> Parameters { get; set; }
        [Newtonsoft.Json.JsonProperty("return")]
        [System.Text.Json.Serialization.JsonPropertyName("return")]
        [YamlDotNet.Serialization.YamlMember(Alias="return")]
        public Docfx.Build.ManagedReference.BuildOutputs.ApiParameterBuildOutput Return { get; set; }
        [Newtonsoft.Json.JsonProperty("typeParameters")]
        [System.Text.Json.Serialization.JsonPropertyName("typeParameters")]
        [YamlDotNet.Serialization.YamlMember(Alias="typeParameters")]
        public System.Collections.Generic.List<Docfx.Build.ManagedReference.BuildOutputs.ApiParameterBuildOutput> TypeParameters { get; set; }
        public void Expand(System.Collections.Generic.Dictionary<string, Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> references, string[] supportedLanguages) { }
        public static Docfx.Build.ManagedReference.BuildOutputs.ApiSyntaxBuildOutput FromModel(Docfx.DataContracts.ManagedReference.SyntaxDetailViewModel model, string[] supportedLanguages) { }
        public static Docfx.Build.ManagedReference.BuildOutputs.ApiSyntaxBuildOutput FromModel(Docfx.DataContracts.ManagedReference.SyntaxDetailViewModel model, System.Collections.Generic.Dictionary<string, Docfx.Build.ManagedReference.BuildOutputs.ApiReferenceBuildOutput> references, string[] supportedLanguages) { }
    }
}
namespace Docfx.Build.OverwriteDocuments
{
    public class Constants
    {
        public const string OPathLineNumberDataName = "opathLineNumber";
        public const string OPathStringDataName = "opathString";
        public Constants() { }
    }
    public interface IOverwriteBlockRule
    {
        string TokenName { get; }
        bool Parse(Markdig.Syntax.Block block, out string value);
    }
    public class InlineCodeHeadingRule : Docfx.Build.OverwriteDocuments.IOverwriteBlockRule
    {
        public InlineCodeHeadingRule() { }
        public virtual string TokenName { get; }
        protected virtual int Level { get; set; }
        protected virtual bool NeedCheckLevel { get; set; }
        public bool Parse(Markdig.Syntax.Block block, out string value) { }
    }
    public sealed class L1InlineCodeHeadingRule : Docfx.Build.OverwriteDocuments.InlineCodeHeadingRule
    {
        public L1InlineCodeHeadingRule() { }
        protected override int Level { get; }
        protected override bool NeedCheckLevel { get; }
        public override string TokenName { get; }
    }
    public sealed class L2InlineCodeHeadingRule : Docfx.Build.OverwriteDocuments.InlineCodeHeadingRule
    {
        public L2InlineCodeHeadingRule() { }
        protected override int Level { get; }
        protected override bool NeedCheckLevel { get; }
        public override string TokenName { get; }
    }
    public class MarkdownFragment
    {
        public MarkdownFragment() { }
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; }
        public System.Collections.Generic.Dictionary<string, Docfx.Build.OverwriteDocuments.MarkdownProperty> Properties { get; set; }
        public bool Touched { get; set; }
        public string Uid { get; set; }
        public override string ToString() { }
    }
    public class MarkdownFragmentModel
    {
        public MarkdownFragmentModel() { }
        public System.Collections.Generic.List<Docfx.Build.OverwriteDocuments.MarkdownPropertyModel> Contents { get; set; }
        public string Uid { get; set; }
        public Markdig.Syntax.Block UidSource { get; set; }
        public string YamlCodeBlock { get; set; }
        public Markdig.Syntax.Block YamlCodeBlockSource { get; set; }
    }
    public class MarkdownFragmentsCreator
    {
        public MarkdownFragmentsCreator() { }
        public System.Collections.Generic.IEnumerable<Docfx.Build.OverwriteDocuments.MarkdownFragmentModel> Create(Markdig.Syntax.MarkdownDocument document) { }
    }
    public class MarkdownFragmentsException : System.Exception
    {
        public MarkdownFragmentsException(string message) { }
        public MarkdownFragmentsException(string message, int position) { }
        public MarkdownFragmentsException(string message, int position, System.Exception inner) { }
        public int Position { get; }
    }
    public class MarkdownProperty
    {
        public MarkdownProperty() { }
        public string Content { get; set; }
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; }
        public string OPath { get; set; }
        public bool Touched { get; set; }
        public void SerializeTo(System.Text.StringBuilder sb) { }
    }
    public class MarkdownPropertyModel
    {
        public MarkdownPropertyModel() { }
        public string PropertyName { get; set; }
        public Markdig.Syntax.Block PropertyNameSource { get; set; }
        public System.Collections.Generic.List<Markdig.Syntax.Block> PropertyValue { get; set; }
    }
    public class OPathSegment
    {
        public OPathSegment() { }
        public string Key { get; set; }
        public string OriginalSegmentString { get; set; }
        public string SegmentName { get; set; }
        public string Value { get; set; }
    }
    public class OverwriteDocumentModelCreator
    {
        public OverwriteDocumentModelCreator(string file) { }
        public Docfx.Build.Common.OverwriteDocumentModel Create(Docfx.Build.OverwriteDocuments.MarkdownFragmentModel model) { }
    }
    public static class OverwriteUtility
    {
        public static void AddOrUpdateFragmentEntity(this System.Collections.Generic.Dictionary<string, Docfx.Build.OverwriteDocuments.MarkdownFragment> fragments, string uid, System.Collections.Generic.Dictionary<string, object> metadata = null) { }
        public static void AddOrUpdateFragmentProperty(this Docfx.Build.OverwriteDocuments.MarkdownFragment fragment, string oPath, string content = null, System.Collections.Generic.Dictionary<string, object> metadata = null) { }
        public static string GetUidWrapper(string uid) { }
        public static System.Collections.Generic.List<Docfx.Build.OverwriteDocuments.OPathSegment> ParseOPath(string OPathString) { }
        public static Docfx.Build.OverwriteDocuments.MarkdownFragment ToMarkdownFragment(this Docfx.Build.OverwriteDocuments.MarkdownFragmentModel model, string originalContent = null) { }
        public static Docfx.Build.OverwriteDocuments.MarkdownProperty ToMarkdownProperty(this Docfx.Build.OverwriteDocuments.MarkdownPropertyModel model, string originalContent = null) { }
    }
    public sealed class YamlCodeBlockRule : Docfx.Build.OverwriteDocuments.IOverwriteBlockRule
    {
        public YamlCodeBlockRule() { }
        public string TokenName { get; }
        public bool Parse(Markdig.Syntax.Block block, out string value) { }
    }
}
namespace Docfx.Build.SchemaDriven
{
    [System.Composition.Export("SchemaDrivenDocumentProcessor", typeof(Docfx.Plugins.IDocumentBuildStep))]
    public class ApplyOverwriteDocument : Docfx.Build.Common.BaseDocumentBuildStep
    {
        public ApplyOverwriteDocument() { }
        public override int BuildOrder { get; }
        public override string Name { get; }
        public override void Postbuild(System.Collections.Immutable.ImmutableList<Docfx.Plugins.FileModel> models, Docfx.Plugins.IHostService host) { }
    }
    [System.Composition.Export("SchemaDrivenDocumentProcessor", typeof(Docfx.Plugins.IDocumentBuildStep))]
    public class ApplyOverwriteFragments : Docfx.Build.Common.BaseDocumentBuildStep
    {
        public ApplyOverwriteFragments() { }
        public override int BuildOrder { get; }
        public override string Name { get; }
        public override void Build(Docfx.Plugins.FileModel model, Docfx.Plugins.IHostService host) { }
    }
    public class BaseSchema
    {
        public BaseSchema() { }
        public Docfx.Build.SchemaDriven.ContentType ContentType { get; set; }
        public System.Collections.Generic.Dictionary<string, Docfx.Build.SchemaDriven.BaseSchema> Definitions { get; set; }
        public Docfx.Build.SchemaDriven.BaseSchema Items { get; set; }
        public Docfx.Build.SchemaDriven.MergeType MergeType { get; set; }
        public System.Collections.Generic.Dictionary<string, Docfx.Build.SchemaDriven.BaseSchema> Properties { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("$ref")]
        public string Ref { get; set; }
        public Docfx.Build.SchemaDriven.ReferenceType Reference { get; set; }
        public System.Collections.Generic.List<string> Tags { get; set; }
        public string Title { get; set; }
        public Json.Schema.SchemaValueType? Type { get; set; }
        public System.Collections.Generic.List<string> XrefProperties { get; set; }
    }
    [System.Composition.Export("SchemaDrivenDocumentProcessor", typeof(Docfx.Plugins.IDocumentBuildStep))]
    public class BuildSchemaBasedDocument : Docfx.Build.Common.BuildReferenceDocumentBase
    {
        public BuildSchemaBasedDocument() { }
        public override int BuildOrder { get; }
        public override string Name { get; }
        protected override void BuildArticle(Docfx.Plugins.IHostService host, Docfx.Plugins.FileModel model) { }
    }
    public class ContentAnchorParser : Docfx.Build.SchemaDriven.IContentAnchorParser
    {
        public const string AnchorContentName = "*content";
        public ContentAnchorParser(string content) { }
        public bool ContainsAnchor { get; }
        public string Content { get; }
        public string Parse(string input) { }
    }
    public enum ContentType
    {
        Default = 0,
        Uid = 1,
        Xref = 2,
        Href = 3,
        File = 4,
        Markdown = 5,
    }
    public class DocumentSchema : Docfx.Build.SchemaDriven.BaseSchema
    {
        public DocumentSchema() { }
        public bool AllowOverwrite { get; }
        public string Metadata { get; set; }
        public Docfx.Build.SchemaDriven.JsonPointer MetadataReference { get; }
        public Docfx.Build.SchemaDriven.SchemaValidator Validator { get; }
        public static Docfx.Build.SchemaDriven.DocumentSchema Load(string content, string title) { }
    }
    public interface IContentAnchorParser
    {
        bool ContainsAnchor { get; }
        string Content { get; }
        string Parse(string input);
    }
    public interface ISchemaFragmentsHandler
    {
        void HandleProperty(string propertyKey, YamlDotNet.RepresentationModel.YamlMappingNode node, System.Collections.Generic.Dictionary<string, Docfx.Build.OverwriteDocuments.MarkdownFragment> fragments, Docfx.Build.SchemaDriven.BaseSchema schema, string oPathPrefix, string uid);
        void HandleUid(string uidKey, YamlDotNet.RepresentationModel.YamlMappingNode node, System.Collections.Generic.Dictionary<string, Docfx.Build.OverwriteDocuments.MarkdownFragment> fragments, Docfx.Build.SchemaDriven.BaseSchema schema, string oPathPrefix, string uid);
    }
    public class JsonPointer
    {
        public JsonPointer(string[] parts) { }
        public JsonPointer(string raw) { }
        public Docfx.Build.SchemaDriven.BaseSchema FindSchema(Docfx.Build.SchemaDriven.DocumentSchema rootSchema) { }
        public Docfx.Build.SchemaDriven.JsonPointer GetParentPointer() { }
        public object GetValue(object root) { }
        public void SetValue(ref object root, object value) { }
        public override string ToString() { }
        public static object GetChild(object root, string part) { }
        public static Docfx.Build.SchemaDriven.BaseSchema GetChildSchema(Docfx.Build.SchemaDriven.BaseSchema parent, string part) { }
        public static void SetChild(object parent, string part, object value) { }
        public static bool TryCreate(string raw, out Docfx.Build.SchemaDriven.JsonPointer pointer) { }
    }
    public enum MergeType
    {
        Merge = 0,
        Key = 1,
        Replace = 2,
        Ignore = 3,
    }
    public class OverwriteApplier
    {
        public OverwriteApplier(Docfx.Plugins.IHostService host, Docfx.Build.SchemaDriven.OverwriteModelType type) { }
        public object BuildOverwriteWithSchema(Docfx.Plugins.FileModel owModel, Docfx.Build.Common.OverwriteDocumentModel overwrite, Docfx.Build.SchemaDriven.BaseSchema schema) { }
        public void MergeContentWithOverwrite(ref object source, object overwrite, string uid, string path, Docfx.Build.SchemaDriven.BaseSchema schema) { }
        public void UpdateXrefSpec(Docfx.Plugins.FileModel fileModel, Docfx.Build.SchemaDriven.BaseSchema schema) { }
    }
    public enum OverwriteModelType
    {
        Classic = 0,
        MarkdownFragments = 1,
    }
    public enum ReferenceType
    {
        None = 0,
        File = 1,
    }
    public class SchemaDrivenDocumentProcessor : Docfx.Build.Common.DisposableDocumentProcessor
    {
        public SchemaDrivenDocumentProcessor(Docfx.Build.SchemaDriven.DocumentSchema schema, Docfx.Plugins.ICompositionContainer container, Docfx.MarkdigEngine.MarkdigMarkdownService markdigMarkdownService, string siteHostName = null) { }
        public override string Name { get; }
        public Docfx.Build.SchemaDriven.SchemaValidator SchemaValidator { get; }
        public override System.Collections.Generic.IEnumerable<Docfx.Plugins.IDocumentBuildStep> BuildSteps { get; set; }
        public override Docfx.Plugins.ProcessingPriority GetProcessingPriority(Docfx.Plugins.FileAndType file) { }
        public override Docfx.Plugins.FileModel Load(Docfx.Plugins.FileAndType file, System.Collections.Immutable.ImmutableDictionary<string, object> metadata) { }
        public override Docfx.Plugins.SaveResult Save(Docfx.Plugins.FileModel model) { }
        public override void UpdateHref(Docfx.Plugins.FileModel model, Docfx.Plugins.IDocumentBuildContext context) { }
    }
    public static class SchemaExtensions
    {
        public static bool IsLegalInFragments(this Docfx.Build.SchemaDriven.BaseSchema schema) { }
        public static bool IsRequiredInFragments(this Docfx.Build.SchemaDriven.BaseSchema schema) { }
    }
    public class SchemaFragmentsIterator
    {
        public SchemaFragmentsIterator(Docfx.Build.SchemaDriven.ISchemaFragmentsHandler handler) { }
        public void Traverse(YamlDotNet.RepresentationModel.YamlNode node, System.Collections.Generic.Dictionary<string, Docfx.Build.OverwriteDocuments.MarkdownFragment> fragments, Docfx.Build.SchemaDriven.BaseSchema schema) { }
    }
    public class SchemaValidator
    {
        public SchemaValidator(string json) { }
        public void Validate(object obj) { }
    }
    public class ValidateFragmentsHandler : Docfx.Build.SchemaDriven.ISchemaFragmentsHandler
    {
        public ValidateFragmentsHandler() { }
        public void HandleProperty(string propertyKey, YamlDotNet.RepresentationModel.YamlMappingNode node, System.Collections.Generic.Dictionary<string, Docfx.Build.OverwriteDocuments.MarkdownFragment> fragments, Docfx.Build.SchemaDriven.BaseSchema schema, string oPathPrefix, string uid) { }
        public void HandleUid(string uidKey, YamlDotNet.RepresentationModel.YamlMappingNode node, System.Collections.Generic.Dictionary<string, Docfx.Build.OverwriteDocuments.MarkdownFragment> fragments, Docfx.Build.SchemaDriven.BaseSchema schema, string oPathPrefix, string uid) { }
    }
}
namespace Docfx.Build.SchemaDriven.Processors
{
    public class FileIncludeInterpreter : Docfx.Build.SchemaDriven.Processors.IInterpreter
    {
        public FileIncludeInterpreter() { }
        public bool CanInterpret(Docfx.Build.SchemaDriven.BaseSchema schema) { }
        public object Interpret(Docfx.Build.SchemaDriven.BaseSchema schema, object value, Docfx.Build.SchemaDriven.Processors.IProcessContext context, string path) { }
    }
    public class FileInterpreter : Docfx.Build.SchemaDriven.Processors.IInterpreter
    {
        public FileInterpreter(bool exportFileLink, bool updateValue) { }
        public bool CanInterpret(Docfx.Build.SchemaDriven.BaseSchema schema) { }
        public object Interpret(Docfx.Build.SchemaDriven.BaseSchema schema, object value, Docfx.Build.SchemaDriven.Processors.IProcessContext context, string path) { }
    }
    public class FragmentsValidationInterpreter : Docfx.Build.SchemaDriven.Processors.IInterpreter
    {
        public FragmentsValidationInterpreter() { }
        public bool CanInterpret(Docfx.Build.SchemaDriven.BaseSchema schema) { }
        public object Interpret(Docfx.Build.SchemaDriven.BaseSchema schema, object value, Docfx.Build.SchemaDriven.Processors.IProcessContext context, string path) { }
    }
    public class HrefInterpreter : Docfx.Build.SchemaDriven.Processors.IInterpreter
    {
        public HrefInterpreter(bool exportFileLink, bool updateValue, string siteHostName = null) { }
        public bool CanInterpret(Docfx.Build.SchemaDriven.BaseSchema schema) { }
        public object Interpret(Docfx.Build.SchemaDriven.BaseSchema schema, object value, Docfx.Build.SchemaDriven.Processors.IProcessContext context, string path) { }
    }
    public interface IInterpreter
    {
        bool CanInterpret(Docfx.Build.SchemaDriven.BaseSchema schema);
        object Interpret(Docfx.Build.SchemaDriven.BaseSchema schema, object value, Docfx.Build.SchemaDriven.Processors.IProcessContext context, string path);
    }
    public interface IProcessContext
    {
        Docfx.Plugins.IDocumentBuildContext BuildContext { get; }
        Docfx.Build.SchemaDriven.IContentAnchorParser ContentAnchorParser { get; }
        System.Collections.Generic.HashSet<string> Dependency { get; }
        System.Collections.Generic.List<Docfx.Plugins.XRefSpec> ExternalXRefSpecs { get; }
        Docfx.Plugins.FileAndType FileAndType { get; }
        System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Docfx.Plugins.LinkSourceInfo>> FileLinkSources { get; }
        Docfx.Plugins.IHostService Host { get; }
        Docfx.MarkdigEngine.MarkdigMarkdownService MarkdigMarkdownService { get; }
        System.Collections.Generic.IDictionary<string, object> Metadata { get; }
        Docfx.Plugins.FileAndType OriginalFileAndType { get; }
        System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>> PathProperties { get; }
        System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Docfx.Plugins.LinkSourceInfo>> UidLinkSources { get; }
        System.Collections.Generic.List<Docfx.Plugins.UidDefinition> Uids { get; }
        System.Collections.Generic.List<Docfx.Plugins.XRefSpec> XRefSpecs { get; }
        T GetModel<T>();
    }
    public class MarkdownAstInterpreter : Docfx.Build.SchemaDriven.Processors.IInterpreter
    {
        public MarkdownAstInterpreter(Docfx.Build.SchemaDriven.Processors.IInterpreter inner) { }
        public bool CanInterpret(Docfx.Build.SchemaDriven.BaseSchema schema) { }
        public object Interpret(Docfx.Build.SchemaDriven.BaseSchema schema, object value, Docfx.Build.SchemaDriven.Processors.IProcessContext context, string path) { }
    }
    public class MarkdownInterpreter : Docfx.Build.SchemaDriven.Processors.IInterpreter
    {
        public MarkdownInterpreter() { }
        public bool CanInterpret(Docfx.Build.SchemaDriven.BaseSchema schema) { }
        public object Interpret(Docfx.Build.SchemaDriven.BaseSchema schema, object value, Docfx.Build.SchemaDriven.Processors.IProcessContext context, string path) { }
    }
    public class MarkdownWithContentAnchorInterpreter : Docfx.Build.SchemaDriven.Processors.IInterpreter
    {
        public MarkdownWithContentAnchorInterpreter(Docfx.Build.SchemaDriven.Processors.IInterpreter inner) { }
        public bool CanInterpret(Docfx.Build.SchemaDriven.BaseSchema schema) { }
        public object Interpret(Docfx.Build.SchemaDriven.BaseSchema schema, object value, Docfx.Build.SchemaDriven.Processors.IProcessContext context, string path) { }
    }
    public class MergeTypeInterpreter : Docfx.Build.SchemaDriven.Processors.IInterpreter
    {
        public MergeTypeInterpreter() { }
        public int Order { get; }
        public bool CanInterpret(Docfx.Build.SchemaDriven.BaseSchema schema) { }
        public object Interpret(Docfx.Build.SchemaDriven.BaseSchema schema, object value, Docfx.Build.SchemaDriven.Processors.IProcessContext context, string path) { }
    }
    public class ProcessContext : Docfx.Build.SchemaDriven.Processors.IProcessContext
    {
        public ProcessContext(Docfx.Plugins.IHostService hs, Docfx.Plugins.FileModel fm, Docfx.Plugins.IDocumentBuildContext bc = null) { }
        public ProcessContext(Docfx.Plugins.IHostService hs, Docfx.Plugins.FileModel fm, Docfx.Plugins.IDocumentBuildContext bc, Docfx.MarkdigEngine.MarkdigMarkdownService markdigMarkdownService = null) { }
        public Docfx.Plugins.IDocumentBuildContext BuildContext { get; }
        public Docfx.Build.SchemaDriven.IContentAnchorParser ContentAnchorParser { get; set; }
        public System.Collections.Generic.HashSet<string> Dependency { get; }
        public System.Collections.Generic.List<Docfx.Plugins.XRefSpec> ExternalXRefSpecs { get; }
        public Docfx.Plugins.FileAndType FileAndType { get; }
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Docfx.Plugins.LinkSourceInfo>> FileLinkSources { get; }
        public Docfx.Plugins.IHostService Host { get; }
        public Docfx.MarkdigEngine.MarkdigMarkdownService MarkdigMarkdownService { get; set; }
        public System.Collections.Generic.IDictionary<string, object> Metadata { get; }
        public Docfx.Plugins.FileAndType OriginalFileAndType { get; }
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>> PathProperties { get; }
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Docfx.Plugins.LinkSourceInfo>> UidLinkSources { get; }
        public System.Collections.Generic.List<Docfx.Plugins.UidDefinition> Uids { get; }
        public System.Collections.Generic.List<Docfx.Plugins.XRefSpec> XRefSpecs { get; }
        public T GetModel<T>() { }
    }
    public class SchemaProcessor
    {
        public SchemaProcessor(params Docfx.Build.SchemaDriven.Processors.IInterpreter[] interpreters) { }
        public object Process(object raw, Docfx.Build.SchemaDriven.BaseSchema schema, Docfx.Build.SchemaDriven.Processors.IProcessContext context) { }
    }
    public class XrefInterpreter : Docfx.Build.SchemaDriven.Processors.IInterpreter
    {
        public XrefInterpreter(bool aggregateXrefs, bool resolveXref) { }
        public bool CanInterpret(Docfx.Build.SchemaDriven.BaseSchema schema) { }
        public object Interpret(Docfx.Build.SchemaDriven.BaseSchema schema, object value, Docfx.Build.SchemaDriven.Processors.IProcessContext context, string path) { }
    }
    public class XrefPropertiesInterpreter : Docfx.Build.SchemaDriven.Processors.IInterpreter
    {
        public XrefPropertiesInterpreter() { }
        public bool CanInterpret(Docfx.Build.SchemaDriven.BaseSchema schema) { }
        public object Interpret(Docfx.Build.SchemaDriven.BaseSchema schema, object value, Docfx.Build.SchemaDriven.Processors.IProcessContext context, string path) { }
    }
}
namespace Docfx.Exceptions
{
    public class InvalidJsonPointerException : Docfx.Plugins.DocumentException
    {
        public InvalidJsonPointerException() { }
        public InvalidJsonPointerException(string message) { }
        public InvalidJsonPointerException(string message, System.Exception innerException) { }
    }
    public class InvalidOverwriteDocumentException : Docfx.Exceptions.DocfxException
    {
        public InvalidOverwriteDocumentException(string message) { }
    }
    public class InvalidSchemaException : Docfx.Plugins.DocumentException
    {
        public InvalidSchemaException(string message) { }
        public InvalidSchemaException(string message, System.Exception innerException) { }
    }
    public class SchemaKeywordNotSupportedException : Docfx.Exceptions.DocfxException
    {
        public SchemaKeywordNotSupportedException(string keyword) { }
    }
}
namespace Docfx.Common
{
    public static class CollectionExtensions
    {
        public static System.Collections.Generic.IEnumerable<TResult> Merge<TItem, TResult>(this System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IEnumerable<TItem>> sources, System.Collections.Generic.IComparer<TItem> comparer, System.Func<System.Collections.Generic.List<TItem>, TResult> merger) { }
    }
    public static class CollectionUtility
    {
        public static System.Collections.Immutable.ImmutableArray<T> GetLongestCommonSequence<T>(this System.Collections.Immutable.ImmutableArray<T> leftItems, System.Collections.Immutable.ImmutableArray<T> rightItems) { }
        public static void Merge<T>(this System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<T>> left, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Collections.Immutable.ImmutableList<T>>> right) { }
        public static System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<T>> Merge<T>(this System.Collections.Generic.IDictionary<string, System.Collections.Generic.List<T>> left, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<T>>> right) { }
        public static System.Collections.Immutable.ImmutableDictionary<string, System.Collections.Immutable.ImmutableList<T>> Merge<T, TRight>(this System.Collections.Immutable.ImmutableDictionary<string, System.Collections.Immutable.ImmutableList<T>> left, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, TRight>> right)
            where TRight : System.Collections.Generic.IEnumerable<T> { }
    }
    public class CommandInfo
    {
        public CommandInfo() { }
        public string Arguments { get; set; }
        public string Name { get; set; }
        public string WorkingDirectory { get; set; }
    }
    public static class CommandUtility
    {
        public static bool ExistCommand(string commandName, System.Action<string> processOutput = null, System.Action<string> processError = null) { }
        public static int RunCommand(Docfx.Common.CommandInfo commandInfo, System.IO.StreamWriter stdoutWriter = null, System.IO.StreamWriter stderrWriter = null, int timeoutInMilliseconds = -1) { }
    }
    public class CompositeDictionary : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IDictionary<string, object>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.IEnumerable
    {
        public CompositeDictionary() { }
        public int Count { get; }
        public bool IsReadOnly { get; }
        public object this[string key] { get; set; }
        public System.Collections.Generic.ICollection<string> Keys { get; }
        public System.Collections.Generic.ICollection<object> Values { get; }
        public void Add(string key, object value) { }
        public void Clear() { }
        public bool ContainsKey(string key) { }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>> GetEnumerator() { }
        public bool Remove(string key) { }
        public bool TryGetValue(string key, out object value) { }
        public static Docfx.Common.CompositeDictionary.Builder CreateBuilder() { }
        public sealed class Builder
        {
            public Docfx.Common.CompositeDictionary.Builder Add<TValue>(string prefix, System.Collections.Generic.IDictionary<string, TValue> dict, System.Func<object, TValue> valueConverter = null) { }
            public Docfx.Common.CompositeDictionary Create() { }
        }
    }
    public class CompositeLogListener : Docfx.Common.ILoggerListener, System.IDisposable
    {
        public CompositeLogListener() { }
        public CompositeLogListener(System.Collections.Generic.IEnumerable<Docfx.Common.ILoggerListener> listeners) { }
        public int Count { get; }
        public void AddListener(Docfx.Common.ILoggerListener listener) { }
        public void AddListeners(System.Collections.Generic.IEnumerable<Docfx.Common.ILoggerListener> listeners) { }
        public void Dispose() { }
        public Docfx.Common.ILoggerListener FindListener(System.Predicate<Docfx.Common.ILoggerListener> predicate) { }
        public void Flush() { }
        public System.Collections.Generic.IEnumerable<Docfx.Common.ILoggerListener> GetAllListeners() { }
        public void RemoveAllListeners() { }
        public void RemoveListener(Docfx.Common.ILoggerListener listener) { }
        public void WriteLine(Docfx.Common.ILogItem item) { }
    }
    public sealed class ConsoleLogListener : Docfx.Common.ILoggerListener, System.IDisposable
    {
        public ConsoleLogListener() { }
        public void Dispose() { }
        public void Flush() { }
        public void WriteLine(Docfx.Common.ILogItem item) { }
    }
    public static class ConvertToObjectHelper
    {
        public static object ConvertExpandoObjectToObject(object raw) { }
        public static object? ConvertJObjectToObject(object raw) { }
        public static object ConvertStrongTypeToJObject(object raw) { }
        public static object ConvertStrongTypeToObject(object raw) { }
        public static object ConvertToDynamic(object obj) { }
    }
    public static class ErrorCodes
    {
        public static class Build
        {
            public const string FatalError = "FatalError";
            public const string FileNamesMaxLengthExceeded = "FileNamesMaxLengthExceeded";
            public const string InternalUidNotFound = "InternalUidNotFound";
            public const string InvalidHref = "InvalidHref";
            public const string InvalidInputFile = "InvalidInputFile";
            public const string InvalidMarkdown = "InvalidMarkdown";
            public const string InvalidPropertyFormat = "InvalidPropertyFormat";
            public const string InvalidRelativePath = "InvalidRelativePath";
            public const string InvalidYamlFile = "InvalidYamlFile";
            public const string TopicHrefNotset = "TopicHrefNotset";
            public const string UidFoundInMultipleArticles = "UidFoundInMultipleArticles";
            public const string UnsupportedTocHrefType = "UnsupportedTocHrefType";
            public const string ViolateSchema = "ViolateSchema";
        }
        public static class Overwrite
        {
            public const string InvalidOverwriteDocument = "InvalidOverwriteDocument";
            public const string OverwriteDocumentMergeError = "OverwriteDocumentMergeError";
        }
        public static class Template
        {
            public const string ApplyTemplatePreprocessorError = "ApplyTemplatePreprocessorError";
            public const string ApplyTemplateRendererError = "ApplyTemplateRendererError";
        }
        public static class Toc
        {
            public const string CircularTocInclusion = "CircularTocInclusion";
            public const string InvalidMarkdownToc = "InvalidMarkdownToc";
            public const string InvalidTocFile = "InvalidTocFile";
            public const string InvalidTocLink = "InvalidTocLink";
        }
    }
    public class FileAbstractLayer : Docfx.Plugins.IFileAbstractLayer
    {
        public FileAbstractLayer(Docfx.Common.IFileReader reader, Docfx.Common.IFileWriter writer) { }
        public Docfx.Common.IFileReader Reader { get; }
        public Docfx.Common.IFileWriter Writer { get; }
        public void Copy(Docfx.Common.RelativePath sourceFileName, Docfx.Common.RelativePath destFileName) { }
        public void Copy(string sourceFileName, string destFileName) { }
        public System.IO.Stream Create(Docfx.Common.RelativePath file) { }
        public System.IO.Stream Create(string file) { }
        public bool Exists(Docfx.Common.RelativePath file) { }
        public bool Exists(string file) { }
        public System.Collections.Generic.IEnumerable<Docfx.Common.RelativePath> GetAllInputFiles() { }
        public string GetExpectedPhysicalPath(Docfx.Common.RelativePath file) { }
        public string GetExpectedPhysicalPath(string file) { }
        public string GetPhysicalPath(Docfx.Common.RelativePath file) { }
        public string GetPhysicalPath(string file) { }
        public System.IO.Stream OpenRead(Docfx.Common.RelativePath file) { }
        public System.IO.Stream OpenRead(string file) { }
    }
    public class FileAbstractLayerBuilder
    {
        public static readonly Docfx.Common.FileAbstractLayerBuilder Default;
        public Docfx.Common.FileAbstractLayer Create() { }
        public Docfx.Common.FileAbstractLayerBuilder ReadFromManifest(Docfx.Plugins.Manifest manifest, string manifestFolder) { }
        public Docfx.Common.FileAbstractLayerBuilder ReadFromOutput(Docfx.Common.FileAbstractLayer fal) { }
        public Docfx.Common.FileAbstractLayerBuilder ReadFromRealFileSystem(string folder) { }
        public Docfx.Common.FileAbstractLayerBuilder WriteToManifest(Docfx.Plugins.Manifest manifest, string manifestFolder, string outputFolder = null) { }
        public Docfx.Common.FileAbstractLayerBuilder WriteToRealFileSystem(string folder) { }
    }
    public static class FileAbstractLayerExtensions
    {
        public static System.IO.StreamWriter CreateText(this Docfx.Common.FileAbstractLayer fal, Docfx.Common.RelativePath file) { }
        public static string GetOutputPhysicalPath(this Docfx.Common.FileAbstractLayer fal, string file) { }
        public static System.IO.StreamReader OpenReadText(this Docfx.Common.FileAbstractLayer fal, Docfx.Common.RelativePath file) { }
        public static string ReadAllText(this Docfx.Common.FileAbstractLayer fal, Docfx.Common.RelativePath file) { }
        public static string ReadAllText(this Docfx.Common.FileAbstractLayer fal, string file) { }
        public static void WriteAllText(this Docfx.Common.FileAbstractLayer fal, Docfx.Common.RelativePath file, string content) { }
        public static void WriteAllText(this Docfx.Common.FileAbstractLayer fal, string file, string content) { }
    }
    public class FileLinkInfo : Docfx.Plugins.IFileLinkInfo
    {
        public FileLinkInfo() { }
        public FileLinkInfo(string fromFileInSource, string fromFileInDest, string href, Docfx.Plugins.IDocumentBuildContext context) { }
        public bool IsResolved { get; }
        public string FileLinkInDest { get; init; }
        public string FileLinkInSource { get; init; }
        public string FromFileInDest { get; init; }
        public string FromFileInSource { get; init; }
        public Docfx.Plugins.GroupInfo GroupInfo { get; init; }
        public string Href { get; init; }
        public string ToFileInDest { get; init; }
        public string ToFileInSource { get; init; }
    }
    public class FilePathComparer : System.Collections.Generic.IEqualityComparer<string>
    {
        public static readonly Docfx.Common.FilePathComparer OSPlatformSensitiveComparer;
        public static readonly Docfx.Common.FilePathComparer OSPlatformSensitiveRelativePathComparer;
        public static readonly System.StringComparer OSPlatformSensitiveStringComparer;
        public FilePathComparer() { }
        public FilePathComparer(bool ignoreToFullPath) { }
        public bool Equals(string x, string y) { }
        public int GetHashCode(string obj) { }
    }
    public abstract class FileWriterBase : Docfx.Common.IFileWriter
    {
        public FileWriterBase(string outputFolder) { }
        public string ExpandedOutputFolder { get; }
        public string OutputFolder { get; }
        public abstract void Copy(Docfx.Common.PathMapping sourceFileName, Docfx.Common.RelativePath destFileName);
        public abstract System.IO.Stream Create(Docfx.Common.RelativePath filePath);
        public abstract Docfx.Common.IFileReader CreateReader();
        protected static void EnsureFolder(string folder) { }
    }
    public interface IFileReader
    {
        System.Collections.Generic.IEnumerable<Docfx.Common.RelativePath> EnumerateFiles();
        Docfx.Common.PathMapping? FindFile(Docfx.Common.RelativePath file);
        string GetExpectedPhysicalPath(Docfx.Common.RelativePath file);
    }
    public interface IFileWriter
    {
        void Copy(Docfx.Common.PathMapping sourceFileName, Docfx.Common.RelativePath destFileName);
        System.IO.Stream Create(Docfx.Common.RelativePath file);
        Docfx.Common.IFileReader CreateReader();
    }
    public interface ILogItem
    {
        string Code { get; }
        string File { get; }
        string Line { get; }
        Docfx.Common.LogLevel LogLevel { get; }
        string Message { get; }
    }
    public interface ILoggerListener : System.IDisposable
    {
        void Flush();
        void WriteLine(Docfx.Common.ILogItem item);
    }
    public class JObjectDictionaryToObjectDictionaryConverter : Newtonsoft.Json.JsonConverter
    {
        public JObjectDictionaryToObjectDictionaryConverter() { }
        public override bool CanConvert(System.Type objectType) { }
        public override object ReadJson(Newtonsoft.Json.JsonReader reader, System.Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer) { }
        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer) { }
    }
    public static class JsonUtility
    {
        public static T Deserialize<T>(System.IO.TextReader reader) { }
        public static T Deserialize<T>(string path) { }
        public static T FromJsonString<T>(this string json) { }
        public static string Serialize<T>(T graph, bool indented = false) { }
        public static void Serialize<T>(string path, T graph, bool indented = false) { }
        public static string ToJsonString<T>(this T graph) { }
    }
    public enum LogLevel
    {
        Diagnostic = -1,
        Verbose = 0,
        Info = 1,
        Suggestion = 2,
        Warning = 3,
        Error = 4,
    }
    public static class Logger
    {
        public const int WarningThrottling = 10000;
        public static volatile Docfx.Common.LogLevel LogLevelThreshold;
        public static volatile System.Collections.Generic.Dictionary<, > Rules;
        public static volatile bool WarningsAsErrors;
        public static int ErrorCount { get; }
        public static bool HasError { get; }
        public static int WarningCount { get; }
        public static Docfx.Common.ILoggerListener FindListener(System.Predicate<Docfx.Common.ILoggerListener> predicate) { }
        public static void Flush() { }
        public static System.Collections.Generic.IEnumerable<Docfx.Common.ILoggerListener> GetAllListeners() { }
        public static Docfx.Common.ILogItem GetLogItem(Docfx.Common.LogLevel level, string message, string phase = null, string file = null, string line = null, string code = null) { }
        public static void Log(object result) { }
        public static void Log(Docfx.Common.LogLevel level, string message, string phase = null, string file = null, string line = null, string code = null) { }
        public static void LogDiagnostic(string message, string phase = null, string file = null, string line = null, string code = null) { }
        public static void LogError(string message, string phase = null, string file = null, string line = null, string code = null) { }
        public static void LogInfo(string message, string phase = null, string file = null, string line = null, string code = null) { }
        public static void LogSuggestion(string message, string phase = null, string file = null, string line = null, string code = null) { }
        public static void LogVerbose(string message, string phase = null, string file = null, string line = null, string code = null) { }
        public static void LogWarning(string message, string phase = null, string file = null, string line = null, string code = null) { }
        public static void PrintSummary() { }
        public static void RegisterListener(Docfx.Common.ILoggerListener listener) { }
        public static void RegisterListeners(System.Collections.Generic.IEnumerable<Docfx.Common.ILoggerListener> listeners) { }
        public static void ResetCount() { }
        public static void UnregisterAllListeners() { }
        public static void UnregisterListener(Docfx.Common.ILoggerListener listener) { }
    }
    public sealed class LoggerFileScope : System.IDisposable
    {
        public LoggerFileScope(string fileName) { }
        public void Dispose() { }
    }
    public class LruList<T>
    {
        protected LruList(int capacity, System.Action<T> onRemoving, System.Collections.Generic.IEqualityComparer<T> comparer) { }
        public void Access(T item) { }
        protected virtual void AccessNoCheck(T item) { }
        public virtual bool Contains(T item) { }
        public virtual bool TryFind(System.Func<T, bool> func, out T item) { }
        public static Docfx.Common.LruList<T> Create(int capacity, System.Action<T> onRemoving = null, System.Collections.Generic.IEqualityComparer<T> comparer = null) { }
        public static Docfx.Common.LruList<T> CreateSynchronized(int capacity, System.Action<T> onRemoving = null, System.Collections.Generic.IEqualityComparer<T> comparer = null) { }
    }
    public static class ManifestFileHelper
    {
        public static void Dereference(this Docfx.Plugins.Manifest manifest, string manifestFolder, int parallelism) { }
    }
    public class ManifestFileReader : Docfx.Common.IFileReader
    {
        public ManifestFileReader(Docfx.Plugins.Manifest manifest, string manifestFolder) { }
        public System.Collections.Generic.IEnumerable<Docfx.Common.RelativePath> EnumerateFiles() { }
        public Docfx.Common.PathMapping? FindFile(Docfx.Common.RelativePath file) { }
        public string GetExpectedPhysicalPath(Docfx.Common.RelativePath file) { }
    }
    public class ManifestFileWriter : Docfx.Common.FileWriterBase
    {
        public ManifestFileWriter(Docfx.Plugins.Manifest manifest, string manifestFolder, string outputFolder) { }
        public override void Copy(Docfx.Common.PathMapping sourceFileName, Docfx.Common.RelativePath destFileName) { }
        public override System.IO.Stream Create(Docfx.Common.RelativePath file) { }
        public override Docfx.Common.IFileReader CreateReader() { }
    }
    public readonly struct PathMapping
    {
        public PathMapping(Docfx.Common.RelativePath logicalPath, string physicalPath) { }
        public Docfx.Common.RelativePath LogicalPath { get; }
        public string PhysicalPath { get; }
    }
    public static class PathUtility
    {
        public static readonly char[] InvalidFileNameChars;
        public static readonly char[] InvalidPathChars;
        public static string FormatPath(this string path, System.UriKind kind, string basePath = null) { }
        public static bool IsPathCaseInsensitive() { }
        public static bool IsRelativePath(string path) { }
        public static string MakeRelativePath(string basePath, string absolutePath) { }
        public static string ToCleanUrlFileName(this string input, string replacement = "-") { }
    }
    public class RealFileReader : Docfx.Common.IFileReader
    {
        public RealFileReader(string inputFolder) { }
        public System.Collections.Generic.IEnumerable<Docfx.Common.RelativePath> EnumerateFiles() { }
        public Docfx.Common.PathMapping? FindFile(Docfx.Common.RelativePath file) { }
        public string GetExpectedPhysicalPath(Docfx.Common.RelativePath file) { }
    }
    public class RealFileWriter : Docfx.Common.FileWriterBase
    {
        public RealFileWriter(string outputFolder) { }
        public override void Copy(Docfx.Common.PathMapping sourceFileName, Docfx.Common.RelativePath destFileName) { }
        public override System.IO.Stream Create(Docfx.Common.RelativePath file) { }
        public override Docfx.Common.IFileReader CreateReader() { }
    }
    public sealed class RelativePath : System.IEquatable<Docfx.Common.RelativePath>
    {
        public const char WorkingFolderChar = '~';
        public const string WorkingFolderString = "~";
        public static readonly string AltWorkingFolder;
        public static readonly Docfx.Common.RelativePath Empty;
        public static readonly char[] InvalidPartChars;
        public static readonly string NormalizedWorkingFolder;
        public static readonly Docfx.Common.RelativePath WorkingFolder;
        public string FileName { get; }
        public bool IsEmpty { get; }
        public int ParentDirectoryCount { get; }
        public int SubdirectoryCount { get; }
        public Docfx.Common.RelativePath BasedOn(Docfx.Common.RelativePath path) { }
        public Docfx.Common.RelativePath ChangeFileName(string fileName) { }
        public bool Equals(Docfx.Common.RelativePath other) { }
        public override bool Equals(object obj) { }
        public Docfx.Common.RelativePath GetDirectoryPath() { }
        public string GetFileNameWithoutExtension() { }
        public override int GetHashCode() { }
        public Docfx.Common.RelativePath GetPathFromWorkingFolder() { }
        public bool InDirectory(Docfx.Common.RelativePath value) { }
        public bool IsFromWorkingFolder() { }
        public Docfx.Common.RelativePath MakeRelativeTo(Docfx.Common.RelativePath relativeTo) { }
        public Docfx.Common.RelativePath Rebase(Docfx.Common.RelativePath from, Docfx.Common.RelativePath to) { }
        public Docfx.Common.RelativePath RemoveWorkingFolder() { }
        public override string ToString() { }
        public Docfx.Common.RelativePath UrlDecode() { }
        public Docfx.Common.RelativePath UrlDecodeUnsafe() { }
        public Docfx.Common.RelativePath UrlEncode() { }
        public static Docfx.Common.RelativePath FromUrl(string path) { }
        public static string GetPathWithoutWorkingFolderChar(string path) { }
        public static bool IsPathFromWorkingFolder(string path) { }
        public static bool IsRelativePath(string path) { }
        public static Docfx.Common.RelativePath Parse(string path) { }
        public static bool TryGetPathWithoutWorkingFolderChar(string path, out string pathFromWorkingFolder) { }
        public static Docfx.Common.RelativePath TryParse(string path) { }
        public static Docfx.Common.RelativePath op_Explicit(string path) { }
        public static string op_Implicit(Docfx.Common.RelativePath path) { }
        public static bool operator !=(Docfx.Common.RelativePath left, Docfx.Common.RelativePath right) { }
        public static Docfx.Common.RelativePath operator +(Docfx.Common.RelativePath left, Docfx.Common.RelativePath right) { }
        public static Docfx.Common.RelativePath operator -(Docfx.Common.RelativePath left, Docfx.Common.RelativePath right) { }
        public static bool operator ==(Docfx.Common.RelativePath left, Docfx.Common.RelativePath right) { }
    }
    public sealed class ReportLogListener : Docfx.Common.ILoggerListener, System.IDisposable
    {
        public ReportLogListener(string reportPath) { }
        public void Dispose() { }
        public void Flush() { }
        public void WriteLine(Docfx.Common.ILogItem item) { }
    }
    public sealed class ResourceLease<T> : System.IDisposable
        where T :  class
    {
        public T Resource { get; }
        public void Dispose() { }
    }
    public static class ResourcePool
    {
        public static Docfx.Common.ResourcePoolManager<T> Create<T>(System.Func<T> creator, int maxResourceCount)
            where T :  class { }
    }
    public class ResourcePoolManager<TResource> : System.IDisposable
        where TResource :  class
    {
        public ResourcePoolManager(System.Func<TResource> creator, int maxResourceCount) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected override void Finalize() { }
        public Docfx.Common.ResourceLease<TResource> Rent() { }
    }
    public static class StringExtension
    {
        public static string BackSlashToForwardSlash(this string input) { }
        public static string ForwardSlashCombine(this string baseAddress, string relativeAddress) { }
        public static string ToDelimitedString(this System.Collections.Generic.IEnumerable<string> input, string delimiter = ",") { }
        public static string ToDisplayPath(this string path) { }
        public static string ToNormalizedFullPath(this string path) { }
        public static string ToNormalizedPath(this string path) { }
        public static string TrimEnd(this string input, string suffixToRemove) { }
    }
    public static class SuggestionCodes
    {
        public static class Build
        {
            public const string EmptyInputContents = "EmptyInputContents";
            public const string EmptyInputFiles = "EmptyInputFiles";
        }
    }
    public static class TreeIterator
    {
        public static void Preorder<T>(T current, T parent, System.Func<T, System.Collections.Generic.IEnumerable<T>> childrenGetter, System.Func<T, T, bool> action) { }
        public static System.Threading.Tasks.Task PreorderAsync<T>(T current, T parent, System.Func<T, System.Collections.Generic.IEnumerable<T>> childrenGetter, System.Func<T, T, System.Threading.Tasks.Task<bool>> action) { }
        public static T PreorderFirstOrDefault<T>(T current, System.Func<T, System.Collections.Generic.IEnumerable<T>> childrenGetter, System.Func<T, bool> predicate) { }
    }
    public static class UriUtility
    {
        public static string GetFragment(string uriString) { }
        public static string GetNonFragment(string uriString) { }
        public static string GetPath(string uriString) { }
        public static string GetQueryString(string uriString) { }
        public static string GetQueryStringAndFragment(string uriString) { }
        public static bool HasFragment(string uriString) { }
        public static bool HasQueryString(string uriString) { }
        public static string MergeHref(string target, string source) { }
        [return: System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "path",
                "query",
                "fragment"})]
        public static System.ValueTuple<string, string, string> Split(string uri) { }
    }
    public static class WarningCodes
    {
        public static class Build
        {
            public const string DuplicateOutputFiles = "DuplicateOutputFiles";
            public const string DuplicateUids = "DuplicateUids";
            public const string EmptyTocItemName = "EmptyTocItemName";
            public const string EmptyTocItemNode = "EmptyTocItemNode";
            public const string InvalidBookmark = "InvalidBookmark";
            public const string InvalidFileLink = "InvalidFileLink";
            public const string InvalidTagParametersConfig = "InvalidTagParametersConfig";
            public const string InvalidTaggedPropertyType = "InvalidTaggedPropertyType";
            public const string InvalidTocInclude = "InvalidTocInclude";
            public const string ReferencedXrefPropertyNotString = "ReferencedXrefPropertyNotString";
            public const string TooManyWarnings = "TooManyWarnings";
            public const string UidNotFound = "UidNotFound";
            public const string UnknownContentType = "UnknownContentType";
            public const string UnknownContentTypeForTemplate = "UnknownContentTypeForTemplate";
            public const string UnknownUriTemplatePipeline = "UnknownUriTemplatePipeline";
        }
        public static class Markdown
        {
            public const string DifferentTabIdSet = "DifferentTabIdSet";
            public const string DuplicateTabId = "DuplicateTabId";
            public const string InvalidCodeSnippet = "InvalidCodeSnippet";
            public const string InvalidInclude = "InvalidInclude";
            public const string InvalidInlineCodeSnippet = "InvalidInlineCodeSnippet";
            public const string InvalidTabGroup = "InvalidTabGroup";
            public const string InvalidYamlHeader = "InvalidYamlHeader";
            public const string MissingNewLineBelowSectionHeader = "MissingNewLineBelowSectionHeader";
            public const string NoVisibleTab = "NoVisibleTab";
        }
        public static class Overwrite
        {
            public const string DuplicateOPaths = "DuplicateOPaths";
            public const string InvalidMarkdownFragments = "InvalidMarkdownFragments";
            public const string InvalidOPaths = "InvalidOPaths";
            public const string InvalidYamlCodeBlockLanguage = "InvalidYamlCodeBlockLanguage";
        }
        public static class Yaml
        {
            public const string MissingYamlMime = "MissingYamlMime";
        }
    }
    public static class XrefUtility
    {
        public static bool TryGetXrefStringValue(this Docfx.Plugins.XRefSpec spec, string key, out string value) { }
    }
    public class YamlDeserializerWithFallback
    {
        public object Deserialize(System.Func<System.IO.TextReader> reader) { }
        public object Deserialize(string filePath) { }
        public Docfx.Common.YamlDeserializerWithFallback WithFallback<T>() { }
        public static Docfx.Common.YamlDeserializerWithFallback Create<T>() { }
    }
    public static class YamlMime
    {
        public const string ManagedReference = "YamlMime:ManagedReference";
        public const string TableOfContent = "YamlMime:TableOfContent";
        public const string XRefMap = "YamlMime:XRefMap";
        public const string YamlMimePrefix = "YamlMime:";
        public static string? ReadMime(System.IO.TextReader reader) { }
        public static string? ReadMime(string file) { }
    }
    public static class YamlUtility
    {
        public static T ConvertTo<T>(object obj) { }
        public static T Deserialize<T>(System.IO.TextReader reader) { }
        public static T Deserialize<T>(string path) { }
        public static void Serialize(System.IO.TextWriter writer, object graph) { }
        public static void Serialize(System.IO.TextWriter writer, object graph, string comments) { }
        public static void Serialize(string path, object graph, string comments) { }
    }
}
namespace Docfx.Common.EntityMergers
{
    public class DictionaryMerger : Docfx.Common.EntityMergers.MergerDecorator
    {
        public DictionaryMerger(Docfx.Common.EntityMergers.IMerger inner) { }
        public override void Merge(ref object source, object overrides, System.Type type, Docfx.Common.EntityMergers.IMergeContext context) { }
    }
    public interface IMergeContext
    {
        object this[string key] { get; }
        Docfx.Common.EntityMergers.IMerger Merger { get; }
    }
    public interface IMergeHandler
    {
        void Merge(ref object source, object overrides, Docfx.Common.EntityMergers.IMergeContext context);
    }
    public interface IMerger
    {
        void Merge(ref object source, object overrides, System.Type type, Docfx.Common.EntityMergers.IMergeContext context);
        bool TestKey(object source, object overrides, System.Type type, Docfx.Common.EntityMergers.IMergeContext context);
    }
    public class JArrayMerger : Docfx.Common.EntityMergers.MergerDecorator
    {
        public JArrayMerger(Docfx.Common.EntityMergers.IMerger inner) { }
        public override void Merge(ref object source, object overrides, System.Type type, Docfx.Common.EntityMergers.IMergeContext context) { }
    }
    public class JObjectMerger : Docfx.Common.EntityMergers.MergerDecorator
    {
        public JObjectMerger(Docfx.Common.EntityMergers.IMerger inner) { }
        public override void Merge(ref object source, object overrides, System.Type type, Docfx.Common.EntityMergers.IMergeContext context) { }
    }
    public class KeyedListMerger : Docfx.Common.EntityMergers.MergerDecorator
    {
        public KeyedListMerger(Docfx.Common.EntityMergers.IMerger inner) { }
        public override void Merge(ref object source, object overrides, System.Type type, Docfx.Common.EntityMergers.IMergeContext context) { }
    }
    public enum MergeOption
    {
        MergeKey = -2,
        Ignore = -1,
        Merge = 0,
        MergeNullOrDefault = 1,
        Replace = 2,
        ReplaceNullOrDefault = 3,
    }
    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed class MergeOptionAttribute : System.Attribute
    {
        public MergeOptionAttribute(Docfx.Common.EntityMergers.MergeOption option = 0) { }
        public MergeOptionAttribute(System.Type handlerType) { }
        public Docfx.Common.EntityMergers.IMergeHandler Handler { get; }
        public Docfx.Common.EntityMergers.MergeOption Option { get; }
    }
    public abstract class MergerDecorator : Docfx.Common.EntityMergers.IMerger
    {
        protected MergerDecorator(Docfx.Common.EntityMergers.IMerger inner) { }
        public virtual void Merge(ref object source, object overrides, System.Type type, Docfx.Common.EntityMergers.IMergeContext context) { }
        public virtual bool TestKey(object source, object overrides, System.Type type, Docfx.Common.EntityMergers.IMergeContext context) { }
    }
    public class MergerFacade
    {
        public MergerFacade(Docfx.Common.EntityMergers.IMerger merger) { }
        public void Merge<T>(ref T source, T overrides, System.Collections.Generic.IReadOnlyDictionary<string, object> data = null)
            where T :  class { }
    }
    public class ReflectionEntityMerger : Docfx.Common.EntityMergers.IMerger
    {
        public ReflectionEntityMerger() { }
    }
}
namespace Docfx.Common.Git
{
    public class GitDetail : System.IEquatable<Docfx.Common.Git.GitDetail>
    {
        public GitDetail() { }
        [Newtonsoft.Json.JsonProperty("branch")]
        [System.Text.Json.Serialization.JsonPropertyName("branch")]
        [YamlDotNet.Serialization.YamlMember(Alias="branch")]
        public string Branch { get; set; }
        [Newtonsoft.Json.JsonProperty("path")]
        [System.Text.Json.Serialization.JsonPropertyName("path")]
        [YamlDotNet.Serialization.YamlMember(Alias="path")]
        public string Path { get; set; }
        [Newtonsoft.Json.JsonProperty("repo")]
        [System.Text.Json.Serialization.JsonPropertyName("repo")]
        [YamlDotNet.Serialization.YamlMember(Alias="repo")]
        public string Repo { get; set; }
    }
    public class GitSource : System.IEquatable<Docfx.Common.Git.GitSource>
    {
        public GitSource(string Repo, string Branch, string Path, int Line) { }
        public string Branch { get; init; }
        public int Line { get; init; }
        public string Path { get; init; }
        public string Repo { get; init; }
    }
    public static class GitUtility
    {
        public static string? GetSourceUrl(Docfx.Common.Git.GitSource source) { }
        public static string? RawContentUrlToContentUrl(string rawUrl) { }
        public static Docfx.Common.Git.GitDetail? TryGetFileDetail(string filePath) { }
    }
}
namespace Docfx.Exceptions
{
    public class DocfxException : System.Exception
    {
        public DocfxException() { }
        public DocfxException(string message) { }
        public DocfxException(string message, System.Exception innerException) { }
    }
}
namespace Docfx
{
    public class FileItems : System.Collections.Generic.List<string>
    {
        public FileItems(System.Collections.Generic.IEnumerable<string> files) { }
        public FileItems(string file) { }
        public static Docfx.FileItems op_Explicit(string input) { }
    }
    [Newtonsoft.Json.JsonConverter(typeof(Docfx.FileMappingConverter))]
    public class FileMapping
    {
        public FileMapping() { }
        public FileMapping(Docfx.FileMappingItem item) { }
        public FileMapping(System.Collections.Generic.IEnumerable<Docfx.FileMappingItem> items) { }
        public bool Expanded { get; set; }
        public System.Collections.Generic.IReadOnlyList<Docfx.FileMappingItem> Items { get; }
        public string RootTocPath { get; }
        public void Add(Docfx.FileMappingItem item) { }
    }
    public class FileMappingItem
    {
        public FileMappingItem() { }
        public FileMappingItem(params string[] files) { }
        [Newtonsoft.Json.JsonProperty("case")]
        [System.Text.Json.Serialization.JsonPropertyName("case")]
        public bool? Case { get; set; }
        [Newtonsoft.Json.JsonProperty("dest")]
        [System.Text.Json.Serialization.JsonPropertyName("dest")]
        public string Dest { get; set; }
        [Newtonsoft.Json.JsonProperty("noNegate")]
        [System.Text.Json.Serialization.JsonPropertyName("noNegate")]
        public bool? DisableNegate { get; set; }
        [Newtonsoft.Json.JsonProperty("dot")]
        [System.Text.Json.Serialization.JsonPropertyName("dot")]
        public bool? Dot { get; set; }
        [Newtonsoft.Json.JsonProperty("exclude")]
        [System.Text.Json.Serialization.JsonPropertyName("exclude")]
        public Docfx.FileItems Exclude { get; set; }
        [Newtonsoft.Json.JsonProperty("files")]
        [System.Text.Json.Serialization.JsonPropertyName("files")]
        public Docfx.FileItems Files { get; set; }
        [Newtonsoft.Json.JsonProperty("group")]
        [System.Text.Json.Serialization.JsonPropertyName("group")]
        public string Group { get; set; }
        [Newtonsoft.Json.JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }
        [Newtonsoft.Json.JsonProperty("noEscape")]
        [System.Text.Json.Serialization.JsonPropertyName("noEscape")]
        public bool? NoEscape { get; set; }
        [Newtonsoft.Json.JsonProperty("noExpand")]
        [System.Text.Json.Serialization.JsonPropertyName("noExpand")]
        public bool? NoExpand { get; set; }
        [Newtonsoft.Json.JsonProperty("noGlobStar")]
        [System.Text.Json.Serialization.JsonPropertyName("noGlobStar")]
        public bool? NoGlobStar { get; set; }
        [Newtonsoft.Json.JsonProperty("rootTocPath")]
        [System.Text.Json.Serialization.JsonPropertyName("rootTocPath")]
        public string RootTocPath { get; set; }
        [Newtonsoft.Json.JsonProperty("src")]
        [System.Text.Json.Serialization.JsonPropertyName("src")]
        public string Src { get; set; }
    }
}
namespace Docfx.DataContracts.Common
{
    public static class Constants
    {
        public const string ConfigFileName = "docfx.json";
        public const string ContentPlaceholder = "*content";
        public const string PrefixSeparator = ".";
        public const string TocYamlFileName = "toc.yml";
        public const string YamlExtension = ".yml";
        public static class DevLang
        {
            public const string CSharp = "csharp";
            public const string VB = "vb";
        }
        public static class DocumentType
        {
            public const string Conceptual = "Conceptual";
            public const string ManagedReference = "ManagedReference";
            public const string Redirection = "Redirection";
            public const string Resource = "Resource";
            public const string Toc = "Toc";
        }
        public static class EnvironmentVariables
        {
            public const string DOCFX_KEEP_DEBUG_INFO = "DOCFX_KEEP_DEBUG_INFO";
            public const string DOCFX_NO_CHECK_CERTIFICATE_REVOCATION_LIST = "DOCFX_NO_CHECK_CERTIFICATE_REVOCATION_LIST";
            public const string DOCFX_SOURCE_BRANCH_NAME = "DOCFX_SOURCE_BRANCH_NAME";
            public static string? KeepDebugInfo { get; }
            public static bool NoCheckCertificateRevocationList { get; }
            public static string? SourceBranchName { get; }
        }
        public static class ExtensionMemberPrefix
        {
            public const string Assemblies = "assemblies.";
            public const string Children = "children.";
            public const string Content = "content.";
            public const string DerivedClasses = "derivedClasses.";
            public const string Exceptions = "exceptions.";
            public const string ExtensionMethods = "extensionMethods.";
            public const string FullName = "fullName.";
            public const string Implements = "implements.";
            public const string Inheritance = "inheritance.";
            public const string InheritedMembers = "inheritedMembers.";
            public const string Name = "name.";
            public const string NameWithType = "nameWithType.";
            public const string Namespace = "namespace.";
            public const string Overload = "overload.";
            public const string Overridden = "overridden.";
            public const string Parent = "parent.";
            public const string Platform = "platform.";
            public const string Return = "return.";
            public const string Source = "source.";
            public const string Spec = "spec.";
        }
        public static class JsonSchemas
        {
            public const string Docfx = "schemas/docfx.schema.json";
            public const string FilterConfig = "schemas/filterconfig.schema.json";
            public const string Toc = "schemas/toc.schema.json";
            public const string XrefMap = "schemas/xrefmap.schema.json";
        }
        public static class MetadataName
        {
            public const string Version = "version";
        }
        public static class PropertyName
        {
            public const string AdditionalNotes = "additionalNotes";
            public const string Assemblies = "assemblies";
            public const string Children = "children";
            public const string CommentId = "commentId";
            public const string Conceptual = "conceptual";
            public const string Content = "content";
            public const string DerivedClasses = "derivedClasses";
            public const string DisplayName = "displayName";
            public const string DocumentType = "documentType";
            public const string Documentation = "documentation";
            public const string Exceptions = "exceptions";
            public const string ExtensionMethods = "extensionMethods";
            public const string FullName = "fullName";
            public const string Href = "href";
            public const string Id = "id";
            public const string Implements = "implements";
            public const string Inheritance = "inheritance";
            public const string InheritedMembers = "inheritedMembers";
            public const string IsEii = "isEii";
            public const string Name = "name";
            public const string NameWithType = "nameWithType";
            public const string Namespace = "namespace";
            public const string OutputFileName = "outputFileName";
            public const string Overload = "overload";
            public const string Overridden = "overridden";
            public const string Parent = "parent";
            public const string Path = "path";
            public const string Platform = "platform";
            public const string RedirectUrl = "redirect_url";
            public const string Return = "return";
            public const string SeeAlsoContent = "seealsoContent";
            public const string Source = "source";
            public const string Summary = "summary";
            public const string Syntax = "syntax";
            public const string SystemKeys = "_systemKeys";
            public const string Title = "title";
            public const string TitleOverwriteH1 = "titleOverwriteH1";
            public const string TocHref = "tocHref";
            public const string TopicHref = "topicHref";
            public const string TopicUid = "topicUid";
            public const string Type = "type";
            public const string Uid = "uid";
        }
        public static class Switches
        {
            public const string DotnetToolMode = "Docfx.DotnetToolMode";
            public static bool IsDotnetToolsMode { get; }
        }
        public static class TableOfContents
        {
            public const string MarkdownTocFileName = "toc.md";
            public const string YamlTocFileName = "toc.yml";
        }
    }
    public class ExternalReferencePackageCollection : System.IDisposable
    {
        public ExternalReferencePackageCollection(System.Collections.Generic.IEnumerable<string> packageFiles, int maxParallelism, System.Threading.CancellationToken cancellationToken) { }
        public System.Collections.Immutable.ImmutableList<Docfx.DataContracts.Common.ExternalReferencePackageReader> Readers { get; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected override void Finalize() { }
        public bool TryGetReference(string uid, out Docfx.DataContracts.Common.ReferenceViewModel vm) { }
    }
    public class ExternalReferencePackageReader : System.IDisposable
    {
        public ExternalReferencePackageReader(string packageFile) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected override void Finalize() { }
        protected virtual int SeekUidIndex(string uid) { }
        public bool TryGetReference(string uid, out Docfx.DataContracts.Common.ReferenceViewModel vm) { }
        public static Docfx.DataContracts.Common.ExternalReferencePackageReader CreateNoThrow(string packageFile) { }
    }
    public class ExternalReferencePackageWriter : System.IDisposable
    {
        public void AddOrUpdateEntry(string entryName, System.Collections.Generic.List<Docfx.DataContracts.Common.ReferenceViewModel> vm) { }
        public void Dispose() { }
        public static Docfx.DataContracts.Common.ExternalReferencePackageWriter Append(string packageFile, System.Uri baseUri) { }
        public static Docfx.DataContracts.Common.ExternalReferencePackageWriter Create(string packageFile, System.Uri baseUri) { }
    }
    public interface IOverwriteDocumentViewModel
    {
        string Conceptual { get; set; }
        Docfx.DataContracts.Common.SourceDetail Documentation { get; set; }
        string Uid { get; set; }
    }
    public static class JTokenConverter
    {
        public static T Convert<T>(object obj) { }
    }
    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple=false, Inherited=false)]
    public class MarkdownContentAttribute : System.Attribute
    {
        public MarkdownContentAttribute() { }
    }
    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple=false, Inherited=false)]
    public class MarkdownContentIgnoreAttribute : System.Attribute
    {
        public MarkdownContentIgnoreAttribute() { }
    }
    public class ReferenceViewModel
    {
        public ReferenceViewModel() { }
        [Docfx.YamlSerialization.ExtensibleMember]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Collections.Generic.Dictionary<string, object> Additional { get; }
        [Docfx.DataContracts.Common.MarkdownContentIgnore]
        [Docfx.DataContracts.Common.UniqueIdentityReferenceIgnore]
        [Newtonsoft.Json.JsonExtensionData]
        [System.Text.Json.Serialization.JsonExtensionData]
        [YamlDotNet.Serialization.YamlIgnore]
        public Docfx.Common.CompositeDictionary AdditionalJson { get; }
        [Newtonsoft.Json.JsonProperty("commentId")]
        [System.Text.Json.Serialization.JsonPropertyName("commentId")]
        [YamlDotNet.Serialization.YamlMember(Alias="commentId")]
        public string CommentId { get; set; }
        [Newtonsoft.Json.JsonProperty("definition")]
        [System.Text.Json.Serialization.JsonPropertyName("definition")]
        [YamlDotNet.Serialization.YamlMember(Alias="definition")]
        public string Definition { get; set; }
        [Newtonsoft.Json.JsonProperty("fullName")]
        [System.Text.Json.Serialization.JsonPropertyName("fullName")]
        [YamlDotNet.Serialization.YamlMember(Alias="fullName")]
        public string FullName { get; set; }
        [Docfx.YamlSerialization.ExtensibleMember("fullName.")]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Collections.Generic.SortedList<string, string> FullNameInDevLangs { get; }
        [Newtonsoft.Json.JsonProperty("href")]
        [System.Text.Json.Serialization.JsonPropertyName("href")]
        [YamlDotNet.Serialization.YamlMember(Alias="href")]
        public string Href { get; set; }
        [Newtonsoft.Json.JsonProperty("isExternal")]
        [System.Text.Json.Serialization.JsonPropertyName("isExternal")]
        [YamlDotNet.Serialization.YamlMember(Alias="isExternal")]
        public bool? IsExternal { get; set; }
        [Newtonsoft.Json.JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        [YamlDotNet.Serialization.YamlMember(Alias="name")]
        public string Name { get; set; }
        [Docfx.YamlSerialization.ExtensibleMember("name.")]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Collections.Generic.SortedList<string, string> NameInDevLangs { get; }
        [Newtonsoft.Json.JsonProperty("nameWithType")]
        [System.Text.Json.Serialization.JsonPropertyName("nameWithType")]
        [YamlDotNet.Serialization.YamlMember(Alias="nameWithType")]
        public string NameWithType { get; set; }
        [Docfx.YamlSerialization.ExtensibleMember("nameWithType.")]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Collections.Generic.SortedList<string, string> NameWithTypeInDevLangs { get; }
        [Newtonsoft.Json.JsonProperty("parent")]
        [System.Text.Json.Serialization.JsonPropertyName("parent")]
        [YamlDotNet.Serialization.YamlMember(Alias="parent")]
        public string Parent { get; set; }
        [Docfx.YamlSerialization.ExtensibleMember("spec.")]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Collections.Generic.SortedList<string, System.Collections.Generic.List<Docfx.DataContracts.Common.SpecViewModel>> Specs { get; }
        [Newtonsoft.Json.JsonProperty("uid")]
        [System.Text.Json.Serialization.JsonPropertyName("uid")]
        [YamlDotNet.Serialization.YamlMember(Alias="uid")]
        public string Uid { get; set; }
        public Docfx.DataContracts.Common.ReferenceViewModel Clone() { }
    }
    public class SourceDetail
    {
        public SourceDetail() { }
        [Newtonsoft.Json.JsonProperty("endLine")]
        [System.Text.Json.Serialization.JsonPropertyName("endLine")]
        [YamlDotNet.Serialization.YamlMember(Alias="endLine")]
        public int EndLine { get; set; }
        [Newtonsoft.Json.JsonProperty("href")]
        [System.Text.Json.Serialization.JsonPropertyName("href")]
        [YamlDotNet.Serialization.YamlMember(Alias="href")]
        public string Href { get; set; }
        [Newtonsoft.Json.JsonProperty("id")]
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        [YamlDotNet.Serialization.YamlMember(Alias="id")]
        public string Name { get; set; }
        [Newtonsoft.Json.JsonProperty("path")]
        [System.Text.Json.Serialization.JsonPropertyName("path")]
        [YamlDotNet.Serialization.YamlMember(Alias="path")]
        public string Path { get; set; }
        [Newtonsoft.Json.JsonProperty("remote")]
        [System.Text.Json.Serialization.JsonPropertyName("remote")]
        [YamlDotNet.Serialization.YamlMember(Alias="remote")]
        public Docfx.Common.Git.GitDetail Remote { get; set; }
        [Newtonsoft.Json.JsonProperty("startLine")]
        [System.Text.Json.Serialization.JsonPropertyName("startLine")]
        [YamlDotNet.Serialization.YamlMember(Alias="startLine")]
        public int StartLine { get; set; }
    }
    public class SpecViewModel
    {
        public SpecViewModel() { }
        [Newtonsoft.Json.JsonProperty("href")]
        [System.Text.Json.Serialization.JsonPropertyName("href")]
        [YamlDotNet.Serialization.YamlMember(Alias="href")]
        public string Href { get; set; }
        [Newtonsoft.Json.JsonProperty("isExternal")]
        [System.Text.Json.Serialization.JsonPropertyName("isExternal")]
        [YamlDotNet.Serialization.YamlMember(Alias="isExternal")]
        public bool IsExternal { get; set; }
        [Newtonsoft.Json.JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        [YamlDotNet.Serialization.YamlMember(Alias="name")]
        public string Name { get; set; }
        [Newtonsoft.Json.JsonProperty("uid")]
        [System.Text.Json.Serialization.JsonPropertyName("uid")]
        [YamlDotNet.Serialization.YamlMember(Alias="uid")]
        public string Uid { get; set; }
    }
    public class TocItemViewModel
    {
        public TocItemViewModel() { }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public string AggregatedHref { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public string AggregatedUid { get; set; }
        [Newtonsoft.Json.JsonProperty("displayName")]
        [System.Text.Json.Serialization.JsonPropertyName("displayName")]
        [YamlDotNet.Serialization.YamlMember(Alias="displayName")]
        public string DisplayName { get; set; }
        [Newtonsoft.Json.JsonProperty("homepage")]
        [System.Text.Json.Serialization.JsonPropertyName("homepage")]
        [YamlDotNet.Serialization.YamlMember(Alias="homepage")]
        public string Homepage { get; set; }
        [Newtonsoft.Json.JsonProperty("homepageUid")]
        [System.Text.Json.Serialization.JsonPropertyName("homepageUid")]
        [YamlDotNet.Serialization.YamlMember(Alias="homepageUid")]
        public string HomepageUid { get; set; }
        [Newtonsoft.Json.JsonProperty("href")]
        [System.Text.Json.Serialization.JsonPropertyName("href")]
        [YamlDotNet.Serialization.YamlMember(Alias="href")]
        public string Href { get; set; }
        [Newtonsoft.Json.JsonProperty("includedFrom")]
        [System.Text.Json.Serialization.JsonPropertyName("includedFrom")]
        [YamlDotNet.Serialization.YamlMember(Alias="includedFrom")]
        public string IncludedFrom { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public bool IsHrefUpdated { get; set; }
        [Newtonsoft.Json.JsonProperty("items")]
        [System.Text.Json.Serialization.JsonPropertyName("items")]
        [YamlDotNet.Serialization.YamlMember(Alias="items")]
        public System.Collections.Generic.List<Docfx.DataContracts.Common.TocItemViewModel> Items { get; set; }
        [Docfx.YamlSerialization.ExtensibleMember]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; }
        [Newtonsoft.Json.JsonExtensionData]
        [System.Text.Json.Serialization.JsonExtensionData]
        [YamlDotNet.Serialization.YamlIgnore]
        public Docfx.Common.CompositeDictionary MetadataJson { get; }
        [Newtonsoft.Json.JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        [YamlDotNet.Serialization.YamlMember(Alias="name")]
        public string Name { get; set; }
        [Newtonsoft.Json.JsonProperty("order")]
        [System.Text.Json.Serialization.JsonPropertyName("order")]
        [YamlDotNet.Serialization.YamlMember(Alias="order")]
        public int? Order { get; set; }
        [Newtonsoft.Json.JsonProperty("originalHomepage")]
        [System.Text.Json.Serialization.JsonPropertyName("originalHomepage")]
        [YamlDotNet.Serialization.YamlMember(Alias="originalHomepage")]
        public string OriginalHomepage { get; set; }
        [Newtonsoft.Json.JsonProperty("originalHref")]
        [System.Text.Json.Serialization.JsonPropertyName("originalHref")]
        [YamlDotNet.Serialization.YamlMember(Alias="originalHref")]
        public string OriginalHref { get; set; }
        [Newtonsoft.Json.JsonProperty("originalTocHref")]
        [System.Text.Json.Serialization.JsonPropertyName("originalTocHref")]
        [YamlDotNet.Serialization.YamlMember(Alias="originalTocHref")]
        public string OriginalTocHref { get; set; }
        [Newtonsoft.Json.JsonProperty("originalTopicHref")]
        [System.Text.Json.Serialization.JsonPropertyName("originalTopicHref")]
        [YamlDotNet.Serialization.YamlMember(Alias="originalTopicHref")]
        public string OriginalTopicHref { get; set; }
        [Newtonsoft.Json.JsonProperty("tocHref")]
        [System.Text.Json.Serialization.JsonPropertyName("tocHref")]
        [YamlDotNet.Serialization.YamlMember(Alias="tocHref")]
        public string TocHref { get; set; }
        [Newtonsoft.Json.JsonProperty("topicHref")]
        [System.Text.Json.Serialization.JsonPropertyName("topicHref")]
        [YamlDotNet.Serialization.YamlMember(Alias="topicHref")]
        public string TopicHref { get; set; }
        [Newtonsoft.Json.JsonProperty("topicUid")]
        [System.Text.Json.Serialization.JsonPropertyName("topicUid")]
        [YamlDotNet.Serialization.YamlMember(Alias="topicUid")]
        public string TopicUid { get; set; }
        [Newtonsoft.Json.JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        [YamlDotNet.Serialization.YamlMember(Alias="type")]
        public string Type { get; set; }
        [Newtonsoft.Json.JsonProperty("uid")]
        [System.Text.Json.Serialization.JsonPropertyName("uid")]
        [YamlDotNet.Serialization.YamlMember(Alias="uid")]
        public string Uid { get; set; }
        public Docfx.DataContracts.Common.TocItemViewModel Clone() { }
        public override string ToString() { }
    }
    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple=false, Inherited=false)]
    public class UniqueIdentityReferenceAttribute : System.Attribute
    {
        public UniqueIdentityReferenceAttribute() { }
    }
    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple=false, Inherited=false)]
    public class UniqueIdentityReferenceIgnoreAttribute : System.Attribute
    {
        public UniqueIdentityReferenceIgnoreAttribute() { }
    }
    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple=false, Inherited=false)]
    public class UrlContentAttribute : System.Attribute
    {
        public UrlContentAttribute() { }
    }
    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple=false, Inherited=false)]
    public class UrlContentIgnoreAttribute : System.Attribute
    {
        public UrlContentIgnoreAttribute() { }
    }
}
namespace Docfx.DataContracts.ManagedReference
{
    public class AdditionalNotes
    {
        public AdditionalNotes() { }
        [Docfx.DataContracts.Common.MarkdownContent]
        [Newtonsoft.Json.JsonProperty("caller")]
        [System.Text.Json.Serialization.JsonPropertyName("caller")]
        [YamlDotNet.Serialization.YamlMember(Alias="caller")]
        public string Caller { get; set; }
        [Docfx.DataContracts.Common.MarkdownContent]
        [Newtonsoft.Json.JsonProperty("implementer")]
        [System.Text.Json.Serialization.JsonPropertyName("implementer")]
        [YamlDotNet.Serialization.YamlMember(Alias="implementer")]
        public string Implementer { get; set; }
        [Docfx.DataContracts.Common.MarkdownContent]
        [Newtonsoft.Json.JsonProperty("inheritor")]
        [System.Text.Json.Serialization.JsonPropertyName("inheritor")]
        [YamlDotNet.Serialization.YamlMember(Alias="inheritor")]
        public string Inheritor { get; set; }
    }
    public class ApiParameter
    {
        public ApiParameter() { }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.Ignore)]
        [Newtonsoft.Json.JsonProperty("attributes")]
        [System.Text.Json.Serialization.JsonPropertyName("attributes")]
        [YamlDotNet.Serialization.YamlMember(Alias="attributes")]
        public System.Collections.Generic.List<Docfx.DataContracts.ManagedReference.AttributeInfo> Attributes { get; set; }
        [Docfx.DataContracts.Common.MarkdownContent]
        [Newtonsoft.Json.JsonProperty("description")]
        [System.Text.Json.Serialization.JsonPropertyName("description")]
        [YamlDotNet.Serialization.YamlMember(Alias="description")]
        public string Description { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.MergeKey)]
        [Newtonsoft.Json.JsonProperty("id")]
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        [YamlDotNet.Serialization.YamlMember(Alias="id")]
        public string Name { get; set; }
        [Docfx.DataContracts.Common.UniqueIdentityReference]
        [Newtonsoft.Json.JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        [YamlDotNet.Serialization.YamlMember(Alias="type")]
        public string Type { get; set; }
    }
    public class ArgumentInfo
    {
        public ArgumentInfo() { }
        [Docfx.DataContracts.Common.UniqueIdentityReference]
        [Newtonsoft.Json.JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        [YamlDotNet.Serialization.YamlMember(Alias="type")]
        public string Type { get; set; }
        [Newtonsoft.Json.JsonProperty("value")]
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        [YamlDotNet.Serialization.YamlMember(Alias="value")]
        public object Value { get; set; }
    }
    public class AttributeInfo
    {
        public AttributeInfo() { }
        [Newtonsoft.Json.JsonProperty("arguments")]
        [System.Text.Json.Serialization.JsonPropertyName("arguments")]
        [YamlDotNet.Serialization.YamlMember(Alias="arguments")]
        public System.Collections.Generic.List<Docfx.DataContracts.ManagedReference.ArgumentInfo> Arguments { get; set; }
        [Newtonsoft.Json.JsonProperty("ctor")]
        [System.Text.Json.Serialization.JsonPropertyName("ctor")]
        [YamlDotNet.Serialization.YamlMember(Alias="ctor")]
        public string Constructor { get; set; }
        [Newtonsoft.Json.JsonProperty("namedArguments")]
        [System.Text.Json.Serialization.JsonPropertyName("namedArguments")]
        [YamlDotNet.Serialization.YamlMember(Alias="namedArguments")]
        public System.Collections.Generic.List<Docfx.DataContracts.ManagedReference.NamedArgumentInfo> NamedArguments { get; set; }
        [Docfx.DataContracts.Common.UniqueIdentityReference]
        [Newtonsoft.Json.JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        [YamlDotNet.Serialization.YamlMember(Alias="type")]
        public string Type { get; set; }
    }
    public class ExceptionInfo
    {
        public ExceptionInfo() { }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.Ignore)]
        [Newtonsoft.Json.JsonProperty("commentId")]
        [System.Text.Json.Serialization.JsonPropertyName("commentId")]
        [YamlDotNet.Serialization.YamlMember(Alias="commentId")]
        public string CommentId { get; set; }
        [Docfx.DataContracts.Common.MarkdownContent]
        [Newtonsoft.Json.JsonProperty("description")]
        [System.Text.Json.Serialization.JsonPropertyName("description")]
        [YamlDotNet.Serialization.YamlMember(Alias="description")]
        public string Description { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.MergeKey)]
        [Docfx.DataContracts.Common.UniqueIdentityReference]
        [Newtonsoft.Json.JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        [YamlDotNet.Serialization.YamlMember(Alias="type")]
        public string Type { get; set; }
    }
    public class ItemViewModel : Docfx.DataContracts.Common.IOverwriteDocumentViewModel
    {
        public ItemViewModel() { }
        [Newtonsoft.Json.JsonProperty("additionalNotes")]
        [System.Text.Json.Serialization.JsonPropertyName("additionalNotes")]
        [YamlDotNet.Serialization.YamlMember(Alias="additionalNotes")]
        public Docfx.DataContracts.ManagedReference.AdditionalNotes AdditionalNotes { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.Ignore)]
        [Newtonsoft.Json.JsonProperty("assemblies")]
        [System.Text.Json.Serialization.JsonPropertyName("assemblies")]
        [YamlDotNet.Serialization.YamlMember(Alias="assemblies")]
        public System.Collections.Generic.List<string> AssemblyNameList { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.Ignore)]
        [Newtonsoft.Json.JsonProperty("attributes")]
        [System.Text.Json.Serialization.JsonPropertyName("attributes")]
        [YamlDotNet.Serialization.YamlMember(Alias="attributes")]
        public System.Collections.Generic.List<Docfx.DataContracts.ManagedReference.AttributeInfo> Attributes { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.Ignore)]
        [Docfx.DataContracts.Common.UniqueIdentityReference]
        [Newtonsoft.Json.JsonProperty("children")]
        [System.Text.Json.Serialization.JsonPropertyName("children")]
        [YamlDotNet.Serialization.YamlMember(Alias="children")]
        public System.Collections.Generic.List<string> Children { get; set; }
        [Newtonsoft.Json.JsonProperty("commentId")]
        [System.Text.Json.Serialization.JsonPropertyName("commentId")]
        [YamlDotNet.Serialization.YamlMember(Alias="commentId")]
        public string CommentId { get; set; }
        [Docfx.DataContracts.Common.MarkdownContent]
        [Newtonsoft.Json.JsonProperty("conceptual")]
        [System.Text.Json.Serialization.JsonPropertyName("conceptual")]
        [YamlDotNet.Serialization.YamlMember(Alias="conceptual")]
        public string Conceptual { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.Ignore)]
        [Docfx.DataContracts.Common.UniqueIdentityReference]
        [Newtonsoft.Json.JsonProperty("derivedClasses")]
        [System.Text.Json.Serialization.JsonPropertyName("derivedClasses")]
        [YamlDotNet.Serialization.YamlMember(Alias="derivedClasses")]
        public System.Collections.Generic.List<string> DerivedClasses { get; set; }
        [Newtonsoft.Json.JsonProperty("documentation")]
        [System.Text.Json.Serialization.JsonPropertyName("documentation")]
        [YamlDotNet.Serialization.YamlMember(Alias="documentation")]
        public Docfx.DataContracts.Common.SourceDetail Documentation { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.Replace)]
        [Docfx.DataContracts.Common.MarkdownContent]
        [Newtonsoft.Json.JsonProperty("example")]
        [System.Text.Json.Serialization.JsonPropertyName("example")]
        [YamlDotNet.Serialization.YamlMember(Alias="example")]
        public System.Collections.Generic.List<string> Examples { get; set; }
        [Newtonsoft.Json.JsonProperty("exceptions")]
        [System.Text.Json.Serialization.JsonPropertyName("exceptions")]
        [YamlDotNet.Serialization.YamlMember(Alias="exceptions")]
        public System.Collections.Generic.List<Docfx.DataContracts.ManagedReference.ExceptionInfo> Exceptions { get; set; }
        [Docfx.DataContracts.Common.MarkdownContentIgnore]
        [Docfx.DataContracts.Common.UniqueIdentityReferenceIgnore]
        [Newtonsoft.Json.JsonExtensionData]
        [System.Text.Json.Serialization.JsonExtensionData]
        [YamlDotNet.Serialization.YamlIgnore]
        public System.Collections.Generic.IDictionary<string, object> ExtensionData { get; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.Ignore)]
        [Docfx.DataContracts.Common.UniqueIdentityReference]
        [Newtonsoft.Json.JsonProperty("extensionMethods")]
        [System.Text.Json.Serialization.JsonPropertyName("extensionMethods")]
        [YamlDotNet.Serialization.YamlMember(Alias="extensionMethods")]
        public System.Collections.Generic.List<string> ExtensionMethods { get; set; }
        [Newtonsoft.Json.JsonProperty("fullName")]
        [System.Text.Json.Serialization.JsonPropertyName("fullName")]
        [YamlDotNet.Serialization.YamlMember(Alias="fullName")]
        public string FullName { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public string FullNameForCSharp { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public string FullNameForVB { get; set; }
        [Docfx.YamlSerialization.ExtensibleMember("fullName.")]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Collections.Generic.SortedList<string, string> FullNames { get; set; }
        [Newtonsoft.Json.JsonProperty("href")]
        [System.Text.Json.Serialization.JsonPropertyName("href")]
        [YamlDotNet.Serialization.YamlMember(Alias="href")]
        public string Href { get; set; }
        [Newtonsoft.Json.JsonProperty("id")]
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        [YamlDotNet.Serialization.YamlMember(Alias="id")]
        public string Id { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.Ignore)]
        [Docfx.DataContracts.Common.UniqueIdentityReference]
        [Newtonsoft.Json.JsonProperty("implements")]
        [System.Text.Json.Serialization.JsonPropertyName("implements")]
        [YamlDotNet.Serialization.YamlMember(Alias="implements")]
        public System.Collections.Generic.List<string> Implements { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.Ignore)]
        [Docfx.DataContracts.Common.UniqueIdentityReference]
        [Newtonsoft.Json.JsonProperty("inheritance")]
        [System.Text.Json.Serialization.JsonPropertyName("inheritance")]
        [YamlDotNet.Serialization.YamlMember(Alias="inheritance")]
        public System.Collections.Generic.List<string> Inheritance { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.Ignore)]
        [Docfx.DataContracts.Common.UniqueIdentityReference]
        [Newtonsoft.Json.JsonProperty("inheritedMembers")]
        [System.Text.Json.Serialization.JsonPropertyName("inheritedMembers")]
        [YamlDotNet.Serialization.YamlMember(Alias="inheritedMembers")]
        public System.Collections.Generic.List<string> InheritedMembers { get; set; }
        [Newtonsoft.Json.JsonProperty("isEii")]
        [System.Text.Json.Serialization.JsonPropertyName("isEii")]
        [YamlDotNet.Serialization.YamlMember(Alias="isEii")]
        public bool IsExplicitInterfaceImplementation { get; set; }
        [Newtonsoft.Json.JsonProperty("isExtensionMethod")]
        [System.Text.Json.Serialization.JsonPropertyName("isExtensionMethod")]
        [YamlDotNet.Serialization.YamlMember(Alias="isExtensionMethod")]
        public bool IsExtensionMethod { get; set; }
        [Docfx.YamlSerialization.ExtensibleMember]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; }
        [Newtonsoft.Json.JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        [YamlDotNet.Serialization.YamlMember(Alias="name")]
        public string Name { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public string NameForCSharp { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public string NameForVB { get; set; }
        [Newtonsoft.Json.JsonProperty("nameWithType")]
        [System.Text.Json.Serialization.JsonPropertyName("nameWithType")]
        [YamlDotNet.Serialization.YamlMember(Alias="nameWithType")]
        public string NameWithType { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public string NameWithTypeForCSharp { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public string NameWithTypeForVB { get; set; }
        [Docfx.YamlSerialization.ExtensibleMember("name.")]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Collections.Generic.SortedList<string, string> Names { get; set; }
        [Docfx.YamlSerialization.ExtensibleMember("nameWithType.")]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Collections.Generic.SortedList<string, string> NamesWithType { get; set; }
        [Docfx.DataContracts.Common.UniqueIdentityReference]
        [Newtonsoft.Json.JsonProperty("namespace")]
        [System.Text.Json.Serialization.JsonPropertyName("namespace")]
        [YamlDotNet.Serialization.YamlMember(Alias="namespace")]
        public string NamespaceName { get; set; }
        [Docfx.DataContracts.Common.UniqueIdentityReference]
        [Newtonsoft.Json.JsonProperty("overload")]
        [System.Text.Json.Serialization.JsonPropertyName("overload")]
        [YamlDotNet.Serialization.YamlMember(Alias="overload")]
        public string Overload { get; set; }
        [Docfx.DataContracts.Common.UniqueIdentityReference]
        [Newtonsoft.Json.JsonProperty("overridden")]
        [System.Text.Json.Serialization.JsonPropertyName("overridden")]
        [YamlDotNet.Serialization.YamlMember(Alias="overridden")]
        public string Overridden { get; set; }
        [Docfx.DataContracts.Common.UniqueIdentityReference]
        [Newtonsoft.Json.JsonProperty("parent")]
        [System.Text.Json.Serialization.JsonPropertyName("parent")]
        [YamlDotNet.Serialization.YamlMember(Alias="parent")]
        public string Parent { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.Replace)]
        [Newtonsoft.Json.JsonProperty("platform")]
        [System.Text.Json.Serialization.JsonPropertyName("platform")]
        [YamlDotNet.Serialization.YamlMember(Alias="platform")]
        public System.Collections.Generic.List<string> Platform { get; set; }
        [Docfx.DataContracts.Common.MarkdownContent]
        [Newtonsoft.Json.JsonProperty("remarks")]
        [System.Text.Json.Serialization.JsonPropertyName("remarks")]
        [YamlDotNet.Serialization.YamlMember(Alias="remarks")]
        public string Remarks { get; set; }
        [Newtonsoft.Json.JsonProperty("seealso")]
        [System.Text.Json.Serialization.JsonPropertyName("seealso")]
        [YamlDotNet.Serialization.YamlMember(Alias="seealso")]
        public System.Collections.Generic.List<Docfx.DataContracts.ManagedReference.LinkInfo> SeeAlsos { get; set; }
        [Docfx.DataContracts.Common.UniqueIdentityReference]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public System.Collections.Generic.List<string> SeeAlsosUidReference { get; }
        [Newtonsoft.Json.JsonProperty("source")]
        [System.Text.Json.Serialization.JsonPropertyName("source")]
        [YamlDotNet.Serialization.YamlMember(Alias="source")]
        public Docfx.DataContracts.Common.SourceDetail Source { get; set; }
        [Docfx.DataContracts.Common.MarkdownContent]
        [Newtonsoft.Json.JsonProperty("summary")]
        [System.Text.Json.Serialization.JsonPropertyName("summary")]
        [YamlDotNet.Serialization.YamlMember(Alias="summary")]
        public string Summary { get; set; }
        [Newtonsoft.Json.JsonProperty("langs")]
        [System.Text.Json.Serialization.JsonPropertyName("langs")]
        [YamlDotNet.Serialization.YamlMember(Alias="langs")]
        public string[] SupportedLanguages { get; set; }
        [Newtonsoft.Json.JsonProperty("syntax")]
        [System.Text.Json.Serialization.JsonPropertyName("syntax")]
        [YamlDotNet.Serialization.YamlMember(Alias="syntax")]
        public Docfx.DataContracts.ManagedReference.SyntaxDetailViewModel Syntax { get; set; }
        [Newtonsoft.Json.JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        [YamlDotNet.Serialization.YamlMember(Alias="type")]
        public Docfx.DataContracts.ManagedReference.MemberType? Type { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.MergeKey)]
        [Newtonsoft.Json.JsonProperty("uid")]
        [System.Text.Json.Serialization.JsonPropertyName("uid")]
        [YamlDotNet.Serialization.YamlMember(Alias="uid")]
        public string Uid { get; set; }
    }
    public class LinkInfo
    {
        public LinkInfo() { }
        [Newtonsoft.Json.JsonProperty("altText")]
        [System.Text.Json.Serialization.JsonPropertyName("altText")]
        [YamlDotNet.Serialization.YamlMember(Alias="altText")]
        public string AltText { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.Ignore)]
        [Newtonsoft.Json.JsonProperty("commentId")]
        [System.Text.Json.Serialization.JsonPropertyName("commentId")]
        [YamlDotNet.Serialization.YamlMember(Alias="commentId")]
        public string CommentId { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.MergeKey)]
        [Newtonsoft.Json.JsonProperty("linkId")]
        [System.Text.Json.Serialization.JsonPropertyName("linkId")]
        [YamlDotNet.Serialization.YamlMember(Alias="linkId")]
        public string LinkId { get; set; }
        [Docfx.Common.EntityMergers.MergeOption(Docfx.Common.EntityMergers.MergeOption.Ignore)]
        [Newtonsoft.Json.JsonProperty("linkType")]
        [System.Text.Json.Serialization.JsonPropertyName("linkType")]
        [YamlDotNet.Serialization.YamlMember(Alias="linkType")]
        public Docfx.DataContracts.ManagedReference.LinkType LinkType { get; set; }
    }
    public enum LinkType
    {
        CRef = 0,
        HRef = 1,
    }
    public enum MemberType
    {
        Default = 0,
        Toc = 1,
        Assembly = 2,
        Namespace = 3,
        Class = 4,
        Interface = 5,
        Struct = 6,
        Delegate = 7,
        Enum = 8,
        Field = 9,
        Property = 10,
        Event = 11,
        Constructor = 12,
        Method = 13,
        Operator = 14,
        Container = 15,
        AttachedEvent = 16,
        AttachedProperty = 17,
    }
    public class NamedArgumentInfo
    {
        public NamedArgumentInfo() { }
        [Newtonsoft.Json.JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        [YamlDotNet.Serialization.YamlMember(Alias="name")]
        public string Name { get; set; }
        [Docfx.DataContracts.Common.UniqueIdentityReference]
        [Newtonsoft.Json.JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        [YamlDotNet.Serialization.YamlMember(Alias="type")]
        public string Type { get; set; }
        [Newtonsoft.Json.JsonProperty("value")]
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        [YamlDotNet.Serialization.YamlMember(Alias="value")]
        public object Value { get; set; }
    }
    public class PageViewModel
    {
        public PageViewModel() { }
        [Newtonsoft.Json.JsonProperty("items")]
        [System.Text.Json.Serialization.JsonPropertyName("items")]
        [YamlDotNet.Serialization.YamlMember(Alias="items")]
        public System.Collections.Generic.List<Docfx.DataContracts.ManagedReference.ItemViewModel> Items { get; set; }
        [Newtonsoft.Json.JsonProperty("memberLayout")]
        [System.Text.Json.Serialization.JsonPropertyName("memberLayout")]
        [YamlDotNet.Serialization.YamlMember(Alias="memberLayout")]
        public Docfx.MemberLayout MemberLayout { get; set; }
        [Docfx.YamlSerialization.ExtensibleMember]
        [Newtonsoft.Json.JsonExtensionData]
        [System.Text.Json.Serialization.JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; }
        [Docfx.DataContracts.Common.MarkdownContentIgnore]
        [Docfx.DataContracts.Common.UniqueIdentityReferenceIgnore]
        [Newtonsoft.Json.JsonProperty("references")]
        [System.Text.Json.Serialization.JsonPropertyName("references")]
        [YamlDotNet.Serialization.YamlMember(Alias="references")]
        public System.Collections.Generic.List<Docfx.DataContracts.Common.ReferenceViewModel> References { get; set; }
        [Newtonsoft.Json.JsonProperty("shouldSkipMarkup")]
        [System.Text.Json.Serialization.JsonPropertyName("shouldSkipMarkup")]
        [YamlDotNet.Serialization.YamlMember(Alias="shouldSkipMarkup")]
        public bool ShouldSkipMarkup { get; set; }
    }
    public class SyntaxDetailViewModel
    {
        public SyntaxDetailViewModel() { }
        [Newtonsoft.Json.JsonProperty("content")]
        [System.Text.Json.Serialization.JsonPropertyName("content")]
        [YamlDotNet.Serialization.YamlMember(Alias="content")]
        public string Content { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public string ContentForCSharp { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [YamlDotNet.Serialization.YamlIgnore]
        public string ContentForVB { get; set; }
        [Docfx.YamlSerialization.ExtensibleMember("content.")]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Collections.Generic.SortedList<string, string> Contents { get; set; }
        [Docfx.DataContracts.Common.MarkdownContentIgnore]
        [Docfx.DataContracts.Common.UniqueIdentityReferenceIgnore]
        [Newtonsoft.Json.JsonExtensionData]
        [System.Text.Json.Serialization.JsonExtensionData]
        [YamlDotNet.Serialization.YamlIgnore]
        public System.Collections.Generic.IDictionary<string, object> ExtensionData { get; }
        [Newtonsoft.Json.JsonProperty("parameters")]
        [System.Text.Json.Serialization.JsonPropertyName("parameters")]
        [YamlDotNet.Serialization.YamlMember(Alias="parameters")]
        public System.Collections.Generic.List<Docfx.DataContracts.ManagedReference.ApiParameter> Parameters { get; set; }
        [Newtonsoft.Json.JsonProperty("return")]
        [System.Text.Json.Serialization.JsonPropertyName("return")]
        [YamlDotNet.Serialization.YamlMember(Alias="return")]
        public Docfx.DataContracts.ManagedReference.ApiParameter Return { get; set; }
        [Newtonsoft.Json.JsonProperty("typeParameters")]
        [System.Text.Json.Serialization.JsonPropertyName("typeParameters")]
        [YamlDotNet.Serialization.YamlMember(Alias="typeParameters")]
        public System.Collections.Generic.List<Docfx.DataContracts.ManagedReference.ApiParameter> TypeParameters { get; set; }
    }
    public enum SyntaxLanguage
    {
        Default = 0,
        CSharp = 1,
        VB = 2,
    }
}
namespace Docfx.Dotnet
{
    public static class DotnetApiCatalog
    {
        public static System.Threading.Tasks.Task GenerateManagedReferenceYamlFiles(string configPath) { }
        public static System.Threading.Tasks.Task GenerateManagedReferenceYamlFiles(string configPath, Docfx.Dotnet.DotnetApiOptions options) { }
    }
    public class DotnetApiOptions
    {
        public DotnetApiOptions() { }
        public System.Func<Microsoft.CodeAnalysis.ISymbol, Docfx.Dotnet.SymbolIncludeState>? IncludeApi { get; init; }
        public System.Func<Microsoft.CodeAnalysis.ISymbol, Docfx.Dotnet.SymbolIncludeState>? IncludeAttribute { get; init; }
        public System.Func<Docfx.Common.Git.GitSource, string?>? SourceUrl { get; init; }
    }
    public enum SymbolIncludeState
    {
        Default = 0,
        Include = 1,
        Exclude = 2,
    }
}
namespace Docfx
{
    public enum MemberLayout
    {
        SamePage = 0,
        SeparatePages = 1,
    }
}
namespace Docfx.Glob
{
    public class FileGlob
    {
        public FileGlob() { }
        public static System.Collections.Generic.IEnumerable<string> GetFiles(string cwd, System.Collections.Generic.IEnumerable<string> patterns, System.Collections.Generic.IEnumerable<string> excludePatterns, Docfx.Glob.GlobMatcherOptions options = 31) { }
    }
    public class GlobMatcher : System.IEquatable<Docfx.Glob.GlobMatcher>
    {
        public const Docfx.Glob.GlobMatcherOptions DefaultOptions = 31;
        public GlobMatcher(string pattern, Docfx.Glob.GlobMatcherOptions options = 31) { }
        public Docfx.Glob.GlobMatcherOptions Options { get; }
        public string Raw { get; }
        public bool Equals(Docfx.Glob.GlobMatcher other) { }
        public override bool Equals(object obj) { }
        public override int GetHashCode() { }
        public System.Text.RegularExpressions.Regex GetRegex() { }
        public bool Match(string file, bool partial = false) { }
    }
    [System.Flags]
    public enum GlobMatcherOptions
    {
        None = 0,
        IgnoreCase = 1,
        AllowNegate = 2,
        AllowExpand = 4,
        AllowEscape = 8,
        AllowGlobStar = 16,
        AllowDotMatch = 32,
    }
}
namespace Docfx
{
    public class GlobUtility
    {
        public GlobUtility() { }
        public static Docfx.FileMapping ExpandFileMapping(string baseDirectory, Docfx.FileMapping fileMapping) { }
    }
}
namespace Docfx.MarkdigEngine.Extensions
{
    public class YamlHeaderExtension : Markdig.IMarkdownExtension
    {
        public YamlHeaderExtension(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public bool AllowInMiddleOfDocument { get; init; }
        public void Setup(Markdig.MarkdownPipelineBuilder pipeline) { }
        public void Setup(Markdig.MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer) { }
    }
    public class YamlHeaderRenderer : Markdig.Renderers.Html.HtmlObjectRenderer<Markdig.Extensions.Yaml.YamlFrontMatterBlock>
    {
        public YamlHeaderRenderer(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        protected override void Write(Markdig.Renderers.HtmlRenderer renderer, Markdig.Extensions.Yaml.YamlFrontMatterBlock obj) { }
    }
}
namespace Docfx.MarkdigEngine
{
    public class MarkdigMarkdownService : Docfx.Plugins.IMarkdownService
    {
        public MarkdigMarkdownService(Docfx.Plugins.MarkdownServiceParameters parameters, System.Func<Markdig.MarkdownPipelineBuilder, Markdig.MarkdownPipelineBuilder> configureMarkdig = null) { }
        public string Name { get; }
        public Docfx.Plugins.MarkupResult Markup(string content, string filePath) { }
        public Docfx.Plugins.MarkupResult Markup(string content, string filePath, bool multipleYamlHeader) { }
        public Markdig.Syntax.MarkdownDocument Parse(string content, string filePath) { }
        public Markdig.Syntax.MarkdownDocument Parse(string content, string filePath, bool isInline) { }
        public Docfx.Plugins.MarkupResult Render(Markdig.Syntax.MarkdownDocument document) { }
        public Docfx.Plugins.MarkupResult Render(Markdig.Syntax.MarkdownDocument document, bool isInline) { }
    }
}
namespace Docfx.Plugins
{
    public class MarkdownServiceParameters
    {
        public MarkdownServiceParameters() { }
        public string BasePath { get; set; }
        public Docfx.Plugins.MarkdownServiceProperties Extensions { get; set; }
        public string TemplateDir { get; set; }
        public System.Collections.Immutable.ImmutableDictionary<string, string> Tokens { get; set; }
    }
    public class MarkdownServiceProperties
    {
        public MarkdownServiceProperties() { }
        [Newtonsoft.Json.JsonProperty("alerts")]
        [System.Text.Json.Serialization.JsonPropertyName("alerts")]
        public System.Collections.Generic.Dictionary<string, string> Alerts { get; set; }
        [Newtonsoft.Json.JsonProperty("enableSourceInfo")]
        [System.Text.Json.Serialization.JsonPropertyName("enableSourceInfo")]
        public bool EnableSourceInfo { get; set; }
        [Newtonsoft.Json.JsonProperty("fallbackFolders")]
        [System.Text.Json.Serialization.JsonPropertyName("fallbackFolders")]
        public string[] FallbackFolders { get; set; }
        [Newtonsoft.Json.JsonProperty("markdigExtensions")]
        [System.Text.Json.Serialization.JsonPropertyName("markdigExtensions")]
        public Docfx.MarkdigEngine.Extensions.MarkdigExtensionSetting[] MarkdigExtensions { get; set; }
        [Newtonsoft.Json.JsonProperty("plantUml")]
        [System.Text.Json.Serialization.JsonPropertyName("plantUml")]
        public Docfx.MarkdigEngine.Extensions.PlantUmlOptions PlantUml { get; set; }
    }
}
namespace Docfx.MarkdigEngine.Extensions
{
    public class ActiveAndVisibleRewriter : Docfx.MarkdigEngine.Extensions.IMarkdownObjectRewriter
    {
        public ActiveAndVisibleRewriter(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public void PostProcess(Markdig.Syntax.IMarkdownObject markdownObject) { }
        public void PreProcess(Markdig.Syntax.IMarkdownObject markdownObject) { }
        public Markdig.Syntax.IMarkdownObject Rewrite(Markdig.Syntax.IMarkdownObject markdownObject) { }
    }
    public class BlockAggregateContext
    {
        public BlockAggregateContext(Markdig.Syntax.ContainerBlock blocks) { }
        public Markdig.Syntax.Block CurrentBlock { get; }
        public void AggregateTo(Markdig.Syntax.Block block, int blockCount) { }
        public Markdig.Syntax.Block LookAhead(int offset) { }
    }
    public abstract class BlockAggregator<TBlock> : Docfx.MarkdigEngine.Extensions.IBlockAggregator
        where TBlock :  class, Markdig.Syntax.IBlock
    {
        protected BlockAggregator() { }
        public bool Aggregate(Docfx.MarkdigEngine.Extensions.BlockAggregateContext context) { }
        protected abstract bool AggregateCore(TBlock block, Docfx.MarkdigEngine.Extensions.BlockAggregateContext context);
    }
    public class ChromelessFormExtension : Docfx.MarkdigEngine.Extensions.ITripleColonExtensionInfo
    {
        public ChromelessFormExtension() { }
        public bool IsBlock { get; }
        public bool IsInline { get; }
        public string Name { get; }
        public bool SelfClosing { get; }
        public bool Render(Markdig.Renderers.HtmlRenderer renderer, Markdig.Syntax.MarkdownObject markdownObject, System.Action<string> logWarning) { }
        public bool TryProcessAttributes(System.Collections.Generic.IDictionary<string, string> attributes, out Markdig.Renderers.Html.HtmlAttributes htmlAttributes, System.Action<string> logError, System.Action<string> logWarning, Markdig.Syntax.MarkdownObject markdownObject) { }
        public bool TryValidateAncestry(Markdig.Syntax.ContainerBlock container, System.Action<string> logError) { }
    }
    public class CodeExtension : Docfx.MarkdigEngine.Extensions.ITripleColonExtensionInfo
    {
        public CodeExtension(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public bool IsBlock { get; }
        public bool IsInline { get; }
        public string Name { get; }
        public bool SelfClosing { get; }
        public static bool EndingTripleColons { get; }
        public bool Render(Markdig.Renderers.HtmlRenderer renderer, Markdig.Syntax.MarkdownObject markdownObject, System.Action<string> logWarning) { }
        public bool TryProcessAttributes(System.Collections.Generic.IDictionary<string, string> attributes, out Markdig.Renderers.Html.HtmlAttributes htmlAttributes, System.Action<string> logError, System.Action<string> logWarning, Markdig.Syntax.MarkdownObject markdownObject) { }
        public bool TryValidateAncestry(Markdig.Syntax.ContainerBlock container, System.Action<string> logError) { }
    }
    public class CodeRange
    {
        public CodeRange() { }
        public int End { get; set; }
        public int Start { get; set; }
    }
    public class CodeSnippet : Markdig.Syntax.LeafBlock
    {
        public CodeSnippet(Markdig.Parsers.BlockParser parser) { }
        public Docfx.MarkdigEngine.Extensions.CodeRange BookMarkRange { get; set; }
        public string CodePath { get; set; }
        public System.Collections.Generic.List<Docfx.MarkdigEngine.Extensions.CodeRange> CodeRanges { get; set; }
        public int? DedentLength { get; set; }
        public string GitUrl { get; set; }
        public System.Collections.Generic.List<Docfx.MarkdigEngine.Extensions.CodeRange> HighlightRanges { get; set; }
        public bool IsInteractive { get; set; }
        public bool IsNotebookCode { get; set; }
        public string Language { get; set; }
        public string Name { get; set; }
        public string Raw { get; set; }
        public Docfx.MarkdigEngine.Extensions.CodeRange StartEndRange { get; set; }
        public string TagName { get; set; }
        public string Title { get; set; }
        public string GetHighlightLinesString() { }
        public void SetAttributeString() { }
        public string ToAttributeString() { }
    }
    public class CodeSnippetExtension : Markdig.IMarkdownExtension
    {
        public CodeSnippetExtension(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public void Setup(Markdig.MarkdownPipelineBuilder pipeline) { }
        public void Setup(Markdig.MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer) { }
    }
    public class CodeSnippetExtractor
    {
        public const string TagNamePlaceHolder = "{tagname}";
        public CodeSnippetExtractor(string startLineTemplate, string endLineTemplate, bool isEndLineContainsTagName = true) { }
        public System.Collections.Generic.Dictionary<string, Docfx.MarkdigEngine.Extensions.CodeRange> GetAllTags(string[] lines, ref System.Collections.Generic.HashSet<int> tagLines) { }
    }
    public class CodeSnippetInteractiveRewriter : Docfx.MarkdigEngine.Extensions.InteractiveBaseRewriter
    {
        public CodeSnippetInteractiveRewriter() { }
        public override Markdig.Syntax.IMarkdownObject Rewrite(Markdig.Syntax.IMarkdownObject markdownObject) { }
    }
    public class CodeSnippetParser : Markdig.Parsers.BlockParser
    {
        public CodeSnippetParser() { }
        public override Markdig.Parsers.BlockState TryOpen(Markdig.Parsers.BlockProcessor processor) { }
    }
    public static class ExtensionsHelper
    {
        public static readonly System.Text.RegularExpressions.Regex HtmlEscapeWithEncode;
        public static readonly System.Text.RegularExpressions.Regex HtmlEscapeWithoutEncode;
        public static readonly System.Text.RegularExpressions.Regex HtmlUnescape;
        public static string Escape(string html, bool encode = false) { }
        public static bool IsEscaped(Markdig.Helpers.StringSlice slice) { }
        public static bool MatchInclusionEnd(ref Markdig.Helpers.StringSlice slice) { }
        public static bool MatchLink(ref Markdig.Helpers.StringSlice slice, ref string title, ref string path) { }
        public static bool MatchStart(ref Markdig.Helpers.StringSlice slice, string startString, bool isCaseSensitive = true) { }
        public static bool MatchStart(Markdig.Parsers.BlockProcessor processor, string startString, bool isCaseSensitive = true) { }
        public static string NormalizePath(string path) { }
        public static string ReplaceRegex(this string input, System.Text.RegularExpressions.Regex pattern, string replacement) { }
        public static void ResetLineIndent(Markdig.Parsers.BlockProcessor processor) { }
        public static char SkipSpaces(ref Markdig.Helpers.StringSlice slice) { }
        public static void SkipWhitespace(ref Markdig.Helpers.StringSlice slice) { }
        public static string TryGetStringBeforeChars(System.Collections.Generic.IEnumerable<char> chars, ref Markdig.Helpers.StringSlice slice, bool breakOnWhitespace = false) { }
        public static string Unescape(string html) { }
    }
    public class FencedCodeInteractiveRewriter : Docfx.MarkdigEngine.Extensions.InteractiveBaseRewriter
    {
        public FencedCodeInteractiveRewriter() { }
        public override Markdig.Syntax.IMarkdownObject Rewrite(Markdig.Syntax.IMarkdownObject markdownObject) { }
    }
    public class HeadingIdExtension : Markdig.IMarkdownExtension
    {
        public HeadingIdExtension() { }
        public void Setup(Markdig.MarkdownPipelineBuilder pipeline) { }
        public void Setup(Markdig.MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer) { }
    }
    public class HeadingIdRewriter : Docfx.MarkdigEngine.Extensions.IMarkdownObjectRewriter
    {
        public HeadingIdRewriter() { }
        public void PostProcess(Markdig.Syntax.IMarkdownObject markdownObject) { }
        public void PreProcess(Markdig.Syntax.IMarkdownObject markdownObject) { }
        public Markdig.Syntax.IMarkdownObject Rewrite(Markdig.Syntax.IMarkdownObject markdownObject) { }
    }
    public class HtmlCodeSnippetRenderer : Markdig.Renderers.Html.HtmlObjectRenderer<Docfx.MarkdigEngine.Extensions.CodeSnippet>
    {
        public HtmlCodeSnippetRenderer(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public string GetContent(string content, Docfx.MarkdigEngine.Extensions.CodeSnippet obj) { }
        protected override void Write(Markdig.Renderers.HtmlRenderer renderer, Docfx.MarkdigEngine.Extensions.CodeSnippet codeSnippet) { }
        public static string GetLanguageByFileExtension(string extension) { }
        public static bool TryGetLineNumber(string lineNumberString, out int lineNumber, bool withL = true) { }
        public static bool TryGetLineRange(string query, out Docfx.MarkdigEngine.Extensions.CodeRange codeRange, bool withL = true) { }
        public static bool TryGetLineRanges(string query, out System.Collections.Generic.List<Docfx.MarkdigEngine.Extensions.CodeRange> codeRanges) { }
    }
    public class HtmlInclusionBlockRenderer : Markdig.Renderers.Html.HtmlObjectRenderer<Docfx.MarkdigEngine.Extensions.InclusionBlock>
    {
        public HtmlInclusionBlockRenderer(Docfx.MarkdigEngine.Extensions.MarkdownContext context, Markdig.MarkdownPipeline pipeline) { }
        protected override void Write(Markdig.Renderers.HtmlRenderer renderer, Docfx.MarkdigEngine.Extensions.InclusionBlock inclusion) { }
    }
    public class HtmlInclusionInlineRenderer : Markdig.Renderers.Html.HtmlObjectRenderer<Docfx.MarkdigEngine.Extensions.InclusionInline>
    {
        public HtmlInclusionInlineRenderer(Docfx.MarkdigEngine.Extensions.MarkdownContext context, Markdig.MarkdownPipeline inlinePipeline) { }
        protected override void Write(Markdig.Renderers.HtmlRenderer renderer, Docfx.MarkdigEngine.Extensions.InclusionInline inclusion) { }
    }
    public class HtmlTabGroupBlockRenderer : Markdig.Renderers.Html.HtmlObjectRenderer<Docfx.MarkdigEngine.Extensions.TabGroupBlock>
    {
        public HtmlTabGroupBlockRenderer() { }
        protected override void Write(Markdig.Renderers.HtmlRenderer renderer, Docfx.MarkdigEngine.Extensions.TabGroupBlock block) { }
    }
    public class HtmlTabTitleBlockRenderer : Markdig.Renderers.Html.HtmlObjectRenderer<Docfx.MarkdigEngine.Extensions.TabTitleBlock>
    {
        public HtmlTabTitleBlockRenderer() { }
        protected override void Write(Markdig.Renderers.HtmlRenderer renderer, Docfx.MarkdigEngine.Extensions.TabTitleBlock block) { }
    }
    public class HtmlXrefInlineRender : Markdig.Renderers.Html.HtmlObjectRenderer<Docfx.MarkdigEngine.Extensions.XrefInline>
    {
        public HtmlXrefInlineRender() { }
        protected override void Write(Markdig.Renderers.HtmlRenderer renderer, Docfx.MarkdigEngine.Extensions.XrefInline obj) { }
    }
    public interface IBlockAggregator
    {
        bool Aggregate(Docfx.MarkdigEngine.Extensions.BlockAggregateContext context);
    }
    public interface IMarkdownObjectRewriter
    {
        void PostProcess(Markdig.Syntax.IMarkdownObject markdownObject);
        void PreProcess(Markdig.Syntax.IMarkdownObject markdownObject);
        Markdig.Syntax.IMarkdownObject Rewrite(Markdig.Syntax.IMarkdownObject markdownObject);
    }
    public interface IMarkdownObjectRewriterProvider
    {
        System.Collections.Immutable.ImmutableArray<Docfx.MarkdigEngine.Extensions.IMarkdownObjectRewriter> GetRewriters();
    }
    public interface ITripleColonExtensionInfo
    {
        bool IsBlock { get; }
        bool IsInline { get; }
        string Name { get; }
        bool SelfClosing { get; }
        bool Render(Markdig.Renderers.HtmlRenderer renderer, Markdig.Syntax.MarkdownObject markdownObject, System.Action<string> logWarning);
        bool TryProcessAttributes(System.Collections.Generic.IDictionary<string, string> attributes, out Markdig.Renderers.Html.HtmlAttributes htmlAttributes, System.Action<string> logError, System.Action<string> logWarning, Markdig.Syntax.MarkdownObject markdownObject);
        bool TryValidateAncestry(Markdig.Syntax.ContainerBlock container, System.Action<string> logError);
    }
    public class ImageExtension : Docfx.MarkdigEngine.Extensions.ITripleColonExtensionInfo
    {
        public ImageExtension(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public bool IsBlock { get; }
        public bool IsInline { get; }
        public string Name { get; }
        public bool SelfClosing { get; }
        public bool Render(Markdig.Renderers.HtmlRenderer renderer, Markdig.Syntax.MarkdownObject obj, System.Action<string> logWarning) { }
        public bool TryProcessAttributes(System.Collections.Generic.IDictionary<string, string> attributes, out Markdig.Renderers.Html.HtmlAttributes htmlAttributes, System.Action<string> logError, System.Action<string> logWarning, Markdig.Syntax.MarkdownObject markdownObject) { }
        public bool TryValidateAncestry(Markdig.Syntax.ContainerBlock container, System.Action<string> logError) { }
        public static string GetHtmlId(Markdig.Syntax.MarkdownObject obj) { }
        public static bool RequiresClosingTripleColon(System.Collections.Generic.IDictionary<string, string> attributes) { }
    }
    public class InclusionBlock : Markdig.Syntax.ContainerBlock
    {
        public InclusionBlock(Markdig.Parsers.BlockParser parser) { }
        public string IncludedFilePath { get; set; }
        public object ResolvedFilePath { get; set; }
        public string Title { get; set; }
        public string GetRawToken() { }
    }
    public class InclusionBlockParser : Markdig.Parsers.BlockParser
    {
        public InclusionBlockParser() { }
        public override Markdig.Parsers.BlockState TryOpen(Markdig.Parsers.BlockProcessor processor) { }
    }
    public static class InclusionContext
    {
        public static System.Collections.Generic.IEnumerable<object> Dependencies { get; }
        public static object File { get; }
        public static bool IsInclude { get; }
        public static object RootFile { get; }
        public static bool IsCircularReference(object file, out System.Collections.Generic.IEnumerable<object> dependencyChain) { }
        public static void PushDependency(object file) { }
        public static System.IDisposable PushFile(object file) { }
        public static System.IDisposable PushInclusion(object file) { }
    }
    public class InclusionExtension : Markdig.IMarkdownExtension
    {
        public InclusionExtension(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public void Setup(Markdig.MarkdownPipelineBuilder pipeline) { }
        public void Setup(Markdig.MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer) { }
    }
    public class InclusionInline : Markdig.Syntax.Inlines.ContainerInline
    {
        public InclusionInline() { }
        public string IncludedFilePath { get; set; }
        public object ResolvedFilePath { get; set; }
        public string Title { get; set; }
        public string GetRawToken() { }
    }
    public class InclusionInlineParser : Markdig.Parsers.InlineParser
    {
        public InclusionInlineParser() { }
        public override bool Match(Markdig.Parsers.InlineProcessor processor, ref Markdig.Helpers.StringSlice slice) { }
    }
    public class InlineOnlyExtension : Markdig.IMarkdownExtension
    {
        public InlineOnlyExtension() { }
        public void Setup(Markdig.MarkdownPipelineBuilder pipeline) { }
        public void Setup(Markdig.MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer) { }
    }
    public abstract class InteractiveBaseRewriter : Docfx.MarkdigEngine.Extensions.IMarkdownObjectRewriter
    {
        protected const string InteractivePostfix = "-interactive";
        protected InteractiveBaseRewriter() { }
        public void PostProcess(Markdig.Syntax.IMarkdownObject markdownObject) { }
        public void PreProcess(Markdig.Syntax.IMarkdownObject markdownObject) { }
        public abstract Markdig.Syntax.IMarkdownObject Rewrite(Markdig.Syntax.IMarkdownObject markdownObject);
        protected static string GetLanguage(string language, out bool isInteractive) { }
    }
    public class InteractiveCodeExtension : Markdig.IMarkdownExtension
    {
        public InteractiveCodeExtension() { }
        public void Setup(Markdig.MarkdownPipelineBuilder pipeline) { }
        public void Setup(Markdig.MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer) { }
    }
    public class LineNumberExtension : Markdig.IMarkdownExtension
    {
        public LineNumberExtension(System.Func<object, string> getFilePath = null) { }
        public void Setup(Markdig.MarkdownPipelineBuilder pipeline) { }
        public void Setup(Markdig.MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer) { }
    }
    [Newtonsoft.Json.JsonConverter(typeof(Docfx.MarkdigEngine.Extensions.MarkdigExtensionSettingConverter.NewtonsoftJsonConverter))]
    [System.Diagnostics.DebuggerDisplay("Name = {Name}")]
    [System.Text.Json.Serialization.JsonConverter(typeof(Docfx.MarkdigEngine.Extensions.MarkdigExtensionSettingConverter.SystemTextJsonConverter))]
    public class MarkdigExtensionSetting
    {
        public MarkdigExtensionSetting(string name, System.Text.Json.Nodes.JsonNode? options = null) { }
        public string Name { get; init; }
        public System.Text.Json.JsonElement? Options { get; init; }
        public T GetOptions<T>(T fallbackValue) { }
        public static Docfx.MarkdigEngine.Extensions.MarkdigExtensionSetting op_Implicit(string name) { }
    }
    public class MarkdownContext
    {
        public MarkdownContext(System.Func<string, string> getToken = null, Docfx.MarkdigEngine.Extensions.MarkdownContext.LogActionDelegate logInfo = null, Docfx.MarkdigEngine.Extensions.MarkdownContext.LogActionDelegate logSuggestion = null, Docfx.MarkdigEngine.Extensions.MarkdownContext.LogActionDelegate logWarning = null, Docfx.MarkdigEngine.Extensions.MarkdownContext.LogActionDelegate logError = null, Docfx.MarkdigEngine.Extensions.MarkdownContext.ReadFileDelegate readFile = null, Docfx.MarkdigEngine.Extensions.MarkdownContext.GetLinkDelegate getLink = null, Docfx.MarkdigEngine.Extensions.MarkdownContext.GetImageLinkDelegate getImageLink = null) { }
        public Docfx.MarkdigEngine.Extensions.MarkdownContext.GetImageLinkDelegate GetImageLink { get; }
        public Docfx.MarkdigEngine.Extensions.MarkdownContext.GetLinkDelegate GetLink { get; }
        public Docfx.MarkdigEngine.Extensions.MarkdownContext.LogActionDelegate LogError { get; }
        public Docfx.MarkdigEngine.Extensions.MarkdownContext.LogActionDelegate LogInfo { get; }
        public Docfx.MarkdigEngine.Extensions.MarkdownContext.LogActionDelegate LogSuggestion { get; }
        public Docfx.MarkdigEngine.Extensions.MarkdownContext.LogActionDelegate LogWarning { get; }
        public Docfx.MarkdigEngine.Extensions.MarkdownContext.ReadFileDelegate ReadFile { get; }
        public string GetToken(string key) { }
        public delegate string GetImageLinkDelegate(string path, Markdig.Syntax.MarkdownObject origin, string altText);
        public delegate string GetLinkDelegate(string path, Markdig.Syntax.MarkdownObject origin);
        public delegate void LogActionDelegate(string code, string message, Markdig.Syntax.MarkdownObject origin, int? line = default);
        [return: System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "content",
                "file"})]
        public delegate System.ValueTuple<string, object> ReadFileDelegate(string path, Markdig.Syntax.MarkdownObject origin);
    }
    public class MarkdownDocumentAggregatorVisitor
    {
        public MarkdownDocumentAggregatorVisitor(Docfx.MarkdigEngine.Extensions.IBlockAggregator aggregator) { }
        public void Visit(Markdig.Syntax.MarkdownDocument document) { }
    }
    public class MarkdownDocumentVisitor
    {
        public MarkdownDocumentVisitor(Docfx.MarkdigEngine.Extensions.IMarkdownObjectRewriter rewriter) { }
        public void Visit(Markdig.Syntax.MarkdownDocument document) { }
    }
    public static class MarkdownExtensions
    {
        public static Markdig.MarkdownPipelineBuilder UseCodeSnippet(this Markdig.MarkdownPipelineBuilder pipeline, Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public static Markdig.MarkdownPipelineBuilder UseDFMCodeInfoPrefix(this Markdig.MarkdownPipelineBuilder pipeline) { }
        public static Markdig.MarkdownPipelineBuilder UseDocfxExtensions(this Markdig.MarkdownPipelineBuilder pipeline, Docfx.MarkdigEngine.Extensions.MarkdownContext context, System.Collections.Generic.Dictionary<string, string> notes = null, Docfx.MarkdigEngine.Extensions.PlantUmlOptions plantUml = null) { }
        public static Markdig.MarkdownPipelineBuilder UseHeadingIdRewriter(this Markdig.MarkdownPipelineBuilder pipeline) { }
        public static Markdig.MarkdownPipelineBuilder UseIncludeFile(this Markdig.MarkdownPipelineBuilder pipeline, Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public static Markdig.MarkdownPipelineBuilder UseInlineOnly(this Markdig.MarkdownPipelineBuilder pipeline) { }
        public static Markdig.MarkdownPipelineBuilder UseInteractiveCode(this Markdig.MarkdownPipelineBuilder pipeline) { }
        public static Markdig.MarkdownPipelineBuilder UseLineNumber(this Markdig.MarkdownPipelineBuilder pipeline, System.Func<object, string> getFilePath = null) { }
        public static Markdig.MarkdownPipelineBuilder UseMonikerRange(this Markdig.MarkdownPipelineBuilder pipeline, Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public static Markdig.MarkdownPipelineBuilder UseNestedColumn(this Markdig.MarkdownPipelineBuilder pipeline, Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public static Markdig.MarkdownPipelineBuilder UseNoloc(this Markdig.MarkdownPipelineBuilder pipeline) { }
        public static Markdig.MarkdownPipelineBuilder UseOptionalExtensions(this Markdig.MarkdownPipelineBuilder pipeline, Docfx.MarkdigEngine.Extensions.MarkdigExtensionSetting[] optionalExtensions) { }
        public static Markdig.MarkdownPipelineBuilder UsePlantUml(this Markdig.MarkdownPipelineBuilder pipeline, Docfx.MarkdigEngine.Extensions.MarkdownContext context, Docfx.MarkdigEngine.Extensions.PlantUmlOptions options = null) { }
        public static Markdig.MarkdownPipelineBuilder UseQuoteSectionNote(this Markdig.MarkdownPipelineBuilder pipeline, Docfx.MarkdigEngine.Extensions.MarkdownContext context, System.Collections.Generic.Dictionary<string, string> notes = null) { }
        public static Markdig.MarkdownPipelineBuilder UseResolveLink(this Markdig.MarkdownPipelineBuilder pipeline, Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public static Markdig.MarkdownPipelineBuilder UseRow(this Markdig.MarkdownPipelineBuilder pipeline, Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public static Markdig.MarkdownPipelineBuilder UseTabGroup(this Markdig.MarkdownPipelineBuilder pipeline, Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public static Markdig.MarkdownPipelineBuilder UseTripleColon(this Markdig.MarkdownPipelineBuilder pipeline, Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public static Markdig.MarkdownPipelineBuilder UseXref(this Markdig.MarkdownPipelineBuilder pipeline) { }
    }
    public class MonikerRangeBlock : Markdig.Syntax.ContainerBlock
    {
        public MonikerRangeBlock(Markdig.Parsers.BlockParser parser) { }
        public bool Closed { get; set; }
        public int ColonCount { get; set; }
        public string MonikerRange { get; set; }
    }
    public class MonikerRangeExtension : Markdig.IMarkdownExtension
    {
        public MonikerRangeExtension(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public void Setup(Markdig.MarkdownPipelineBuilder pipeline) { }
        public void Setup(Markdig.MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer) { }
    }
    public class MonikerRangeParser : Markdig.Parsers.BlockParser
    {
        public MonikerRangeParser(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public override bool Close(Markdig.Parsers.BlockProcessor processor, Markdig.Syntax.Block block) { }
        public override Markdig.Parsers.BlockState TryContinue(Markdig.Parsers.BlockProcessor processor, Markdig.Syntax.Block block) { }
        public override Markdig.Parsers.BlockState TryOpen(Markdig.Parsers.BlockProcessor processor) { }
    }
    public class MonikerRangeRender : Markdig.Renderers.Html.HtmlObjectRenderer<Docfx.MarkdigEngine.Extensions.MonikerRangeBlock>
    {
        public MonikerRangeRender() { }
        protected override void Write(Markdig.Renderers.HtmlRenderer renderer, Docfx.MarkdigEngine.Extensions.MonikerRangeBlock obj) { }
    }
    public class NestedColumnBlock : Markdig.Syntax.ContainerBlock
    {
        public NestedColumnBlock(Markdig.Parsers.BlockParser parser) { }
        public int ColonCount { get; set; }
        public string ColumnWidth { get; set; }
    }
    public class NestedColumnExtension : Markdig.IMarkdownExtension
    {
        public NestedColumnExtension(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public void Setup(Markdig.MarkdownPipelineBuilder pipeline) { }
        public void Setup(Markdig.MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer) { }
    }
    public class NestedColumnParser : Markdig.Parsers.BlockParser
    {
        public NestedColumnParser(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public override Markdig.Parsers.BlockState TryContinue(Markdig.Parsers.BlockProcessor processor, Markdig.Syntax.Block block) { }
        public override Markdig.Parsers.BlockState TryOpen(Markdig.Parsers.BlockProcessor processor) { }
    }
    public class NestedColumnRender : Markdig.Renderers.Html.HtmlObjectRenderer<Docfx.MarkdigEngine.Extensions.NestedColumnBlock>
    {
        public NestedColumnRender() { }
        protected override void Write(Markdig.Renderers.HtmlRenderer renderer, Docfx.MarkdigEngine.Extensions.NestedColumnBlock obj) { }
    }
    public class NolocExtension : Markdig.IMarkdownExtension
    {
        public NolocExtension() { }
        public void Setup(Markdig.MarkdownPipelineBuilder pipeline) { }
        public void Setup(Markdig.MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer) { }
    }
    public class NolocInline : Markdig.Syntax.Inlines.LeafInline
    {
        public NolocInline() { }
        public string Text { get; set; }
    }
    public class NolocParser : Markdig.Parsers.InlineParser
    {
        public NolocParser() { }
        public override bool Match(Markdig.Parsers.InlineProcessor processor, ref Markdig.Helpers.StringSlice slice) { }
    }
    public class NolocRender : Markdig.Renderers.Html.HtmlObjectRenderer<Docfx.MarkdigEngine.Extensions.NolocInline>
    {
        public NolocRender() { }
        protected override void Write(Markdig.Renderers.HtmlRenderer renderer, Docfx.MarkdigEngine.Extensions.NolocInline obj) { }
    }
    public class PlantUmlOptions
    {
        public PlantUmlOptions() { }
        [Newtonsoft.Json.JsonProperty("delimitor")]
        [System.Text.Json.Serialization.JsonPropertyName("delimitor")]
        public string Delimitor { get; set; }
        [Newtonsoft.Json.JsonProperty("javaPath")]
        [System.Text.Json.Serialization.JsonPropertyName("javaPath")]
        public string JavaPath { get; set; }
        [Newtonsoft.Json.JsonProperty("localGraphvizDotPath")]
        [System.Text.Json.Serialization.JsonPropertyName("localGraphvizDotPath")]
        public string LocalGraphvizDotPath { get; set; }
        [Newtonsoft.Json.JsonProperty("localPlantUmlPath")]
        [System.Text.Json.Serialization.JsonPropertyName("localPlantUmlPath")]
        public string LocalPlantUmlPath { get; set; }
        [Newtonsoft.Json.JsonProperty("outputFormat")]
        [System.Text.Json.Serialization.JsonPropertyName("outputFormat")]
        public PlantUml.Net.OutputFormat OutputFormat { get; set; }
        [Newtonsoft.Json.JsonProperty("remoteUrl")]
        [System.Text.Json.Serialization.JsonPropertyName("remoteUrl")]
        public string RemoteUrl { get; set; }
        [Newtonsoft.Json.JsonProperty("renderingMode")]
        [System.Text.Json.Serialization.JsonPropertyName("renderingMode")]
        public PlantUml.Net.RenderingMode RenderingMode { get; set; }
    }
    public class QuoteSectionNoteBlock : Markdig.Syntax.ContainerBlock
    {
        public QuoteSectionNoteBlock(Markdig.Parsers.BlockParser parser) { }
        public string NoteTypeString { get; set; }
        public char QuoteChar { get; set; }
        public Docfx.MarkdigEngine.Extensions.QuoteSectionNoteType QuoteType { get; set; }
        public string SectionAttributeString { get; set; }
        public string VideoLink { get; set; }
    }
    public class QuoteSectionNoteExtension : Markdig.IMarkdownExtension
    {
        public QuoteSectionNoteExtension(Docfx.MarkdigEngine.Extensions.MarkdownContext context, System.Collections.Generic.Dictionary<string, string> notes) { }
    }
    public class QuoteSectionNoteParser : Markdig.Parsers.BlockParser
    {
        public QuoteSectionNoteParser(Docfx.MarkdigEngine.Extensions.MarkdownContext context, string[] noteTypes = null) { }
        public override Markdig.Parsers.BlockState TryContinue(Markdig.Parsers.BlockProcessor processor, Markdig.Syntax.Block block) { }
        public override Markdig.Parsers.BlockState TryOpen(Markdig.Parsers.BlockProcessor processor) { }
    }
    public class QuoteSectionNoteRender : Markdig.Renderers.Html.HtmlObjectRenderer<Docfx.MarkdigEngine.Extensions.QuoteSectionNoteBlock>
    {
        public QuoteSectionNoteRender(Docfx.MarkdigEngine.Extensions.MarkdownContext context, System.Collections.Generic.Dictionary<string, string> notes) { }
        protected override void Write(Markdig.Renderers.HtmlRenderer renderer, Docfx.MarkdigEngine.Extensions.QuoteSectionNoteBlock obj) { }
        public static string FixUpLink(string link) { }
    }
    public enum QuoteSectionNoteType
    {
        MarkdownQuote = 0,
        DFMSection = 1,
        DFMNote = 2,
        DFMVideo = 3,
    }
    public class ResolveLinkExtension : Markdig.IMarkdownExtension
    {
        public ResolveLinkExtension(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public void Setup(Markdig.MarkdownPipelineBuilder pipeline) { }
        public void Setup(Markdig.MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer) { }
    }
    public class RowBlock : Markdig.Syntax.ContainerBlock
    {
        public RowBlock(Markdig.Parsers.BlockParser parser) { }
        public int ColonCount { get; set; }
    }
    public class RowExtension : Markdig.IMarkdownExtension
    {
        public RowExtension(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public void Setup(Markdig.MarkdownPipelineBuilder pipeline) { }
        public void Setup(Markdig.MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer) { }
    }
    public class RowParser : Markdig.Parsers.BlockParser
    {
        public RowParser(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public override Markdig.Parsers.BlockState TryContinue(Markdig.Parsers.BlockProcessor processor, Markdig.Syntax.Block block) { }
        public override Markdig.Parsers.BlockState TryOpen(Markdig.Parsers.BlockProcessor processor) { }
    }
    public class RowRender : Markdig.Renderers.Html.HtmlObjectRenderer<Docfx.MarkdigEngine.Extensions.RowBlock>
    {
        public RowRender() { }
        protected override void Write(Markdig.Renderers.HtmlRenderer renderer, Docfx.MarkdigEngine.Extensions.RowBlock obj) { }
    }
    public class TabContentBlock : Markdig.Syntax.ContainerBlock
    {
        public TabContentBlock(System.Collections.Generic.List<Markdig.Syntax.Block> blocks) { }
    }
    public class TabGroupAggregator : Docfx.MarkdigEngine.Extensions.BlockAggregator<Markdig.Syntax.HeadingBlock>
    {
        public TabGroupAggregator() { }
        protected override bool AggregateCore(Markdig.Syntax.HeadingBlock headBlock, Docfx.MarkdigEngine.Extensions.BlockAggregateContext context) { }
    }
    public class TabGroupBlock : Markdig.Syntax.ContainerBlock
    {
        public TabGroupBlock(System.Collections.Immutable.ImmutableArray<Docfx.MarkdigEngine.Extensions.TabItemBlock> blocks, int startLine, int startSpan, int activeTabIndex) { }
        public int ActiveTabIndex { get; set; }
        public int Id { get; set; }
        public System.Collections.Immutable.ImmutableArray<Docfx.MarkdigEngine.Extensions.TabItemBlock> Items { get; set; }
    }
    public class TabGroupExtension : Markdig.IMarkdownExtension
    {
        public TabGroupExtension(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public void Setup(Markdig.MarkdownPipelineBuilder pipeline) { }
        public void Setup(Markdig.MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer) { }
    }
    public class TabItemBlock
    {
        public TabItemBlock(string id, string condition, Docfx.MarkdigEngine.Extensions.TabTitleBlock title, Docfx.MarkdigEngine.Extensions.TabContentBlock content, bool visible) { }
        public string Condition { get; }
        public Docfx.MarkdigEngine.Extensions.TabContentBlock Content { get; }
        public string Id { get; }
        public Docfx.MarkdigEngine.Extensions.TabTitleBlock Title { get; }
        public bool Visible { get; set; }
    }
    public class TabTitleBlock : Markdig.Syntax.LeafBlock
    {
        public TabTitleBlock() { }
    }
    public class TripleColonBlock : Markdig.Syntax.ContainerBlock
    {
        public TripleColonBlock(Markdig.Parsers.BlockParser parser) { }
        public System.Collections.Generic.IDictionary<string, string> Attributes { get; set; }
        public string Body { get; set; }
        public bool Closed { get; set; }
        public bool EndingTripleColons { get; set; }
        public Docfx.MarkdigEngine.Extensions.ITripleColonExtensionInfo Extension { get; set; }
    }
    public class TripleColonBlockParser : Markdig.Parsers.BlockParser
    {
        public TripleColonBlockParser(Docfx.MarkdigEngine.Extensions.MarkdownContext context, System.Collections.Generic.IDictionary<string, Docfx.MarkdigEngine.Extensions.ITripleColonExtensionInfo> extensions) { }
        public override bool Close(Markdig.Parsers.BlockProcessor processor, Markdig.Syntax.Block block) { }
        public override Markdig.Parsers.BlockState TryContinue(Markdig.Parsers.BlockProcessor processor, Markdig.Syntax.Block block) { }
        public override Markdig.Parsers.BlockState TryOpen(Markdig.Parsers.BlockProcessor processor) { }
        public static bool TryMatchAttributeValue(ref Markdig.Helpers.StringSlice slice, out string value, string attributeName, System.Action<string> logError) { }
        public static bool TryMatchAttributes(ref Markdig.Helpers.StringSlice slice, out System.Collections.Generic.IDictionary<string, string> attributes, bool selfClosing, System.Action<string> logError) { }
        public static bool TryMatchIdentifier(ref Markdig.Helpers.StringSlice slice, out string name) { }
    }
    public class TripleColonBlockRenderer : Markdig.Renderers.Html.HtmlObjectRenderer<Docfx.MarkdigEngine.Extensions.TripleColonBlock>
    {
        public TripleColonBlockRenderer(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        protected override void Write(Markdig.Renderers.HtmlRenderer renderer, Docfx.MarkdigEngine.Extensions.TripleColonBlock block) { }
    }
    public class TripleColonExtension : Markdig.IMarkdownExtension
    {
        public TripleColonExtension(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        public TripleColonExtension(Docfx.MarkdigEngine.Extensions.MarkdownContext context, params Docfx.MarkdigEngine.Extensions.ITripleColonExtensionInfo[] extensions) { }
        public void Setup(Markdig.MarkdownPipelineBuilder pipeline) { }
        public void Setup(Markdig.MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer) { }
    }
    public class TripleColonInline : Markdig.Syntax.Inlines.Inline
    {
        public TripleColonInline() { }
        public System.Collections.Generic.IDictionary<string, string> Attributes { get; set; }
        public string Body { get; set; }
        public bool Closed { get; set; }
        public int Count { get; }
        public bool EndingTripleColons { get; set; }
        public Docfx.MarkdigEngine.Extensions.ITripleColonExtensionInfo Extension { get; set; }
    }
    public class TripleColonInlineParser : Markdig.Parsers.InlineParser
    {
        public TripleColonInlineParser(Docfx.MarkdigEngine.Extensions.MarkdownContext context, System.Collections.Generic.IDictionary<string, Docfx.MarkdigEngine.Extensions.ITripleColonExtensionInfo> extensions) { }
        public override bool Match(Markdig.Parsers.InlineProcessor processor, ref Markdig.Helpers.StringSlice slice) { }
    }
    public class TripleColonInlineRenderer : Markdig.Renderers.Html.HtmlObjectRenderer<Docfx.MarkdigEngine.Extensions.TripleColonInline>
    {
        public TripleColonInlineRenderer(Docfx.MarkdigEngine.Extensions.MarkdownContext context) { }
        protected override void Write(Markdig.Renderers.HtmlRenderer renderer, Docfx.MarkdigEngine.Extensions.TripleColonInline inline) { }
    }
    public class VideoExtension : Docfx.MarkdigEngine.Extensions.ITripleColonExtensionInfo
    {
        public VideoExtension() { }
        public bool IsBlock { get; }
        public bool IsInline { get; }
        public string Name { get; }
        public bool SelfClosing { get; }
        public bool Render(Markdig.Renderers.HtmlRenderer renderer, Markdig.Syntax.MarkdownObject markdownObject, System.Action<string> logWarning) { }
        public bool TryProcessAttributes(System.Collections.Generic.IDictionary<string, string> attributes, out Markdig.Renderers.Html.HtmlAttributes htmlAttributes, System.Action<string> logError, System.Action<string> logWarning, Markdig.Syntax.MarkdownObject markdownObject) { }
        public bool TryValidateAncestry(Markdig.Syntax.ContainerBlock container, System.Action<string> logError) { }
        public static string GetHtmlId(Markdig.Syntax.MarkdownObject obj) { }
        public static bool RequiresClosingTripleColon(System.Collections.Generic.IDictionary<string, string> attributes) { }
    }
    public class XrefInline : Markdig.Syntax.Inlines.LeafInline
    {
        public XrefInline() { }
        public string Href { get; set; }
    }
    public class XrefInlineExtension : Markdig.IMarkdownExtension
    {
        public XrefInlineExtension() { }
        public void Setup(Markdig.MarkdownPipelineBuilder pipeline) { }
        public void Setup(Markdig.MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer) { }
    }
    public class XrefInlineParser : Markdig.Parsers.InlineParser
    {
        public XrefInlineParser() { }
        public override bool Match(Markdig.Parsers.InlineProcessor processor, ref Markdig.Helpers.StringSlice slice) { }
    }
    public class ZoneExtension : Docfx.MarkdigEngine.Extensions.ITripleColonExtensionInfo
    {
        public ZoneExtension() { }
        public bool IsBlock { get; }
        public bool IsInline { get; }
        public string Name { get; }
        public bool SelfClosing { get; }
        public bool Render(Markdig.Renderers.HtmlRenderer renderer, Markdig.Syntax.MarkdownObject markdownObject, System.Action<string> logWarning) { }
        public bool TryProcessAttributes(System.Collections.Generic.IDictionary<string, string> attributes, out Markdig.Renderers.Html.HtmlAttributes htmlAttributes, System.Action<string> logError, System.Action<string> logWarning, Markdig.Syntax.MarkdownObject markdownObject) { }
        public bool TryValidateAncestry(Markdig.Syntax.ContainerBlock container, System.Action<string> logError) { }
    }
}
namespace Docfx.MarkdigEngine
{
    public class TabGroupIdRewriter : Docfx.MarkdigEngine.Extensions.IMarkdownObjectRewriter
    {
        public TabGroupIdRewriter() { }
        public void PostProcess(Markdig.Syntax.IMarkdownObject markdownObject) { }
        public void PreProcess(Markdig.Syntax.IMarkdownObject markdownObject) { }
        public Markdig.Syntax.IMarkdownObject Rewrite(Markdig.Syntax.IMarkdownObject markdownObject) { }
    }
}
namespace Docfx.Plugins
{
    public class DefaultFileAbstractLayer : Docfx.Plugins.IFileAbstractLayer
    {
        public DefaultFileAbstractLayer() { }
        public void Copy(string sourceFileName, string destFileName) { }
        public System.IO.Stream Create(string file) { }
        public bool Exists(string file) { }
        public System.Collections.Generic.IEnumerable<string> GetAllInputFiles() { }
        public string GetExpectedPhysicalPath(string file) { }
        public string GetPhysicalPath(string file) { }
        public System.Collections.Immutable.ImmutableDictionary<string, string> GetProperties(string file) { }
        public System.IO.Stream OpenRead(string file) { }
        public static string GetOutputPhysicalPath(string file) { }
    }
    public class DocumentException : System.Exception
    {
        public DocumentException() { }
        public DocumentException(string message) { }
        public DocumentException(string message, System.Exception inner) { }
    }
    public static class DocumentExceptionExtensions
    {
        public static void RunAll<TElement>(this System.Collections.Generic.IEnumerable<TElement> elements, System.Action<TElement> action, System.Threading.CancellationToken cancellationToken = default) { }
        public static void RunAll<TElement>(this System.Collections.Generic.IReadOnlyList<TElement> elements, System.Action<TElement> action, System.Threading.CancellationToken cancellationToken = default) { }
        public static void RunAll<TElement>(this System.Collections.Generic.IEnumerable<TElement> elements, System.Action<TElement> action, int parallelism, System.Threading.CancellationToken cancellationToken = default) { }
        public static void RunAll<TElement>(this System.Collections.Generic.IReadOnlyList<TElement> elements, System.Action<TElement> action, int parallelism, System.Threading.CancellationToken cancellationToken = default) { }
        public static TResult[] RunAll<TElement, TResult>(this System.Collections.Generic.IReadOnlyList<TElement> elements, System.Func<TElement, TResult> func, System.Threading.CancellationToken cancellationToken = default) { }
    }
    public enum DocumentType
    {
        Article = 0,
        Overwrite = 1,
        Resource = 2,
        Metadata = 3,
        MarkdownFragments = 4,
    }
    public static class EnvironmentContext
    {
        public static string BaseDirectory { get; }
        public static Docfx.Plugins.IFileAbstractLayer FileAbstractLayer { get; }
        public static bool GitFeaturesDisabled { get; }
        public static string OutputDirectory { get; }
        public static Docfx.Plugins.IFileAbstractLayer FileAbstractLayerImpl { get; set; }
        public static void Clean() { }
        public static void SetBaseDirectory(string dir) { }
        public static void SetGitFeaturesDisabled(bool disabled) { }
        public static void SetOutputDirectory(string dir) { }
    }
    public static class FileAbstractLayerExtensions
    {
        public static System.IO.StreamReader OpenReadText(this Docfx.Plugins.IFileAbstractLayer fal, string file) { }
        public static string ReadAllText(this Docfx.Plugins.IFileAbstractLayer fal, string file) { }
    }
    public sealed class FileAndType : System.IEquatable<Docfx.Plugins.FileAndType>
    {
        [Newtonsoft.Json.JsonConstructor]
        [System.Text.Json.Serialization.JsonConstructor]
        public FileAndType(string baseDir, string file, Docfx.Plugins.DocumentType type, string sourceDir = null, string destinationDir = null) { }
        [Newtonsoft.Json.JsonProperty("baseDir")]
        [System.Text.Json.Serialization.JsonPropertyName("baseDir")]
        public string BaseDir { get; }
        [Newtonsoft.Json.JsonProperty("destinationDir")]
        [System.Text.Json.Serialization.JsonPropertyName("destinationDir")]
        public string DestinationDir { get; set; }
        [Newtonsoft.Json.JsonProperty("file")]
        [System.Text.Json.Serialization.JsonPropertyName("file")]
        public string File { get; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string FullPath { get; }
        [Newtonsoft.Json.JsonProperty("sourceDir")]
        [System.Text.Json.Serialization.JsonPropertyName("sourceDir")]
        public string SourceDir { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public System.StringComparer StringComparer { get; }
        [Newtonsoft.Json.JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public Docfx.Plugins.DocumentType Type { get; }
        public Docfx.Plugins.FileAndType ChangeBaseDir(string baseDir) { }
        public Docfx.Plugins.FileAndType ChangeFile(string file) { }
        public Docfx.Plugins.FileAndType ChangeType(Docfx.Plugins.DocumentType type) { }
        public bool Equals(Docfx.Plugins.FileAndType other) { }
        public override bool Equals(object obj) { }
        public override int GetHashCode() { }
        public static bool operator !=(Docfx.Plugins.FileAndType left, Docfx.Plugins.FileAndType right) { }
        public static bool operator ==(Docfx.Plugins.FileAndType left, Docfx.Plugins.FileAndType right) { }
    }
    public sealed class FileModel
    {
        public FileModel(Docfx.Plugins.FileAndType ft, object content, Docfx.Plugins.FileAndType original = null) { }
        public FileModel(Docfx.Plugins.FileAndType ft, object content, Docfx.Plugins.FileAndType original, string key) { }
        public string BaseDir { get; set; }
        public object Content { get; set; }
        public string DocumentType { get; set; }
        public string File { get; set; }
        public Docfx.Plugins.FileAndType FileAndType { get; }
        public System.Collections.Immutable.ImmutableDictionary<string, System.Collections.Immutable.ImmutableList<Docfx.Plugins.LinkSourceInfo>> FileLinkSources { get; set; }
        public string Key { get; }
        public System.Collections.Immutable.ImmutableHashSet<string> LinkToFiles { get; set; }
        public System.Collections.Immutable.ImmutableHashSet<string> LinkToUids { get; set; }
        public string LocalPathFromRoot { get; set; }
        [System.Runtime.CompilerServices.Dynamic]
        public object ManifestProperties { get; }
        public Docfx.Plugins.FileModel MarkdownFragmentsModel { get; set; }
        public Docfx.Plugins.FileAndType OriginalFileAndType { get; }
        [System.Runtime.CompilerServices.Dynamic]
        public object Properties { get; }
        public Docfx.Plugins.DocumentType Type { get; }
        public System.Collections.Immutable.ImmutableDictionary<string, System.Collections.Immutable.ImmutableList<Docfx.Plugins.LinkSourceInfo>> UidLinkSources { get; set; }
        public System.Collections.Immutable.ImmutableArray<Docfx.Plugins.UidDefinition> Uids { get; set; }
    }
    public class GroupInfo
    {
        public GroupInfo() { }
        public string Destination { get; set; }
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; }
        public string Name { get; set; }
    }
    public interface ICompositionContainer
    {
        T GetExport<T>();
        T GetExport<T>(string name);
        System.Collections.Generic.IEnumerable<T> GetExports<T>();
        System.Collections.Generic.IEnumerable<T> GetExports<T>(string name);
    }
    public interface ICustomHrefGenerator
    {
        string GenerateHref(Docfx.Plugins.IFileLinkInfo href);
    }
    public interface IDocumentBuildContext
    {
        System.Threading.CancellationToken CancellationToken { get; }
        Docfx.Plugins.GroupInfo GroupInfo { get; }
        Docfx.Plugins.ICustomHrefGenerator HrefGenerator { get; }
        string RootTocPath { get; }
        string VersionFolder { get; }
        string VersionName { get; }
        string GetFilePath(string key);
        System.Collections.Immutable.IImmutableList<string> GetTocFileKeySet(string key);
        System.Collections.Immutable.IImmutableList<Docfx.Plugins.TocInfo> GetTocInfo();
        Docfx.Plugins.XRefSpec GetXrefSpec(string uid);
        void RegisterInternalXrefSpec(Docfx.Plugins.XRefSpec xrefSpec);
        void RegisterInternalXrefSpecBookmark(string uid, string bookmark);
        void RegisterToc(string tocFileKey, string fileKey);
        void RegisterTocInfo(Docfx.Plugins.TocInfo toc);
        void SetFilePath(string key, string filePath);
    }
    public interface IDocumentBuildStep
    {
        int BuildOrder { get; }
        string Name { get; }
        void Build(Docfx.Plugins.FileModel model, Docfx.Plugins.IHostService host);
        void Postbuild(System.Collections.Immutable.ImmutableList<Docfx.Plugins.FileModel> models, Docfx.Plugins.IHostService host);
        System.Collections.Generic.IEnumerable<Docfx.Plugins.FileModel> Prebuild(System.Collections.Immutable.ImmutableList<Docfx.Plugins.FileModel> models, Docfx.Plugins.IHostService host);
    }
    public interface IDocumentProcessor
    {
        System.Collections.Generic.IEnumerable<Docfx.Plugins.IDocumentBuildStep> BuildSteps { get; }
        string Name { get; }
        Docfx.Plugins.ProcessingPriority GetProcessingPriority(Docfx.Plugins.FileAndType file);
        Docfx.Plugins.FileModel Load(Docfx.Plugins.FileAndType file, System.Collections.Immutable.ImmutableDictionary<string, object> metadata);
        Docfx.Plugins.SaveResult Save(Docfx.Plugins.FileModel model);
        void UpdateHref(Docfx.Plugins.FileModel model, Docfx.Plugins.IDocumentBuildContext context);
    }
    public interface IFileAbstractLayer
    {
        void Copy(string sourceFileName, string destFileName);
        System.IO.Stream Create(string file);
        bool Exists(string file);
        System.Collections.Generic.IEnumerable<string> GetAllInputFiles();
        string GetExpectedPhysicalPath(string file);
        string GetPhysicalPath(string file);
        System.IO.Stream OpenRead(string file);
    }
    public interface IFileLinkInfo
    {
        string FileLinkInDest { get; }
        string FileLinkInSource { get; }
        string FromFileInDest { get; }
        string FromFileInSource { get; }
        Docfx.Plugins.GroupInfo GroupInfo { get; }
        string Href { get; }
        bool IsResolved { get; }
        string ToFileInDest { get; }
        string ToFileInSource { get; }
    }
    public interface IHostService
    {
        Docfx.Plugins.GroupInfo GroupInfo { get; }
        bool HasMetadataValidation { get; }
        string MarkdownServiceName { get; }
        Docfx.Plugins.IDocumentProcessor Processor { get; }
        System.Collections.Immutable.ImmutableDictionary<string, Docfx.Plugins.FileAndType> SourceFiles { get; }
        System.Collections.Immutable.ImmutableList<Docfx.Plugins.TreeItemRestructure> TableOfContentRestructions { get; set; }
        string VersionName { get; }
        string VersionOutputFolder { get; }
        System.Collections.Immutable.ImmutableHashSet<string> GetAllUids();
        System.Collections.Immutable.ImmutableList<Docfx.Plugins.FileModel> GetModels(Docfx.Plugins.DocumentType? type = default);
        void LogDiagnostic(string message, string file = null, string line = null);
        void LogError(string message, string file = null, string line = null);
        void LogInfo(string message, string file = null, string line = null);
        void LogSuggestion(string message, string file = null, string line = null);
        void LogVerbose(string message, string file = null, string line = null);
        void LogWarning(string message, string file = null, string line = null);
        System.Collections.Immutable.ImmutableList<Docfx.Plugins.FileModel> LookupByUid(string uid);
        Docfx.Plugins.MarkupResult Markup(string markdown, Docfx.Plugins.FileAndType ft);
        Docfx.Plugins.MarkupResult Markup(string markdown, Docfx.Plugins.FileAndType ft, bool omitParse);
        Docfx.Plugins.MarkupResult Parse(Docfx.Plugins.MarkupResult markupResult, Docfx.Plugins.FileAndType ft);
        void ValidateInputMetadata(string file, System.Collections.Immutable.ImmutableDictionary<string, object> metadata);
    }
    public interface IInputMetadataValidator
    {
        void Validate(string sourceFile, System.Collections.Immutable.ImmutableDictionary<string, object> metadata);
    }
    public interface IMarkdownService
    {
        string Name { get; }
        Docfx.Plugins.MarkupResult Markup(string src, string path);
    }
    public interface IPostProcessor
    {
        System.Collections.Immutable.ImmutableDictionary<string, object> PrepareMetadata(System.Collections.Immutable.ImmutableDictionary<string, object> metadata);
        Docfx.Plugins.Manifest Process(Docfx.Plugins.Manifest manifest, string outputFolder, System.Threading.CancellationToken cancellationToken);
    }
    public readonly struct LinkSourceInfo
    {
        public string Anchor { get; init; }
        public int LineNumber { get; init; }
        public string SourceFile { get; init; }
        public string Target { get; init; }
    }
    public class Manifest
    {
        public Manifest() { }
        public Manifest(System.Collections.Generic.IEnumerable<Docfx.Plugins.ManifestItem> files) { }
        [Newtonsoft.Json.JsonProperty("files")]
        [System.Text.Json.Serialization.JsonPropertyName("files")]
        public System.Collections.Generic.List<Docfx.Plugins.ManifestItem> Files { get; }
        [Newtonsoft.Json.JsonProperty("groups")]
        [System.Text.Json.Serialization.JsonPropertyName("groups")]
        public System.Collections.Generic.List<Docfx.Plugins.ManifestGroupInfo> Groups { get; set; }
        [Newtonsoft.Json.JsonProperty("sitemap")]
        [System.Text.Json.Serialization.JsonPropertyName("sitemap")]
        public Docfx.Plugins.SitemapOptions Sitemap { get; set; }
        [Newtonsoft.Json.JsonProperty("source_base_path")]
        [System.Text.Json.Serialization.JsonPropertyName("source_base_path")]
        public string SourceBasePath { get; set; }
        [Newtonsoft.Json.JsonProperty("xrefmap")]
        [System.Obsolete]
        [System.Text.Json.Serialization.JsonPropertyName("xrefmap")]
        public object Xrefmap { get; set; }
    }
    public class ManifestGroupInfo
    {
        public ManifestGroupInfo(Docfx.Plugins.GroupInfo groupInfo) { }
        [Newtonsoft.Json.JsonProperty("dest")]
        [System.Text.Json.Serialization.JsonPropertyName("dest")]
        public string Destination { get; set; }
        [Newtonsoft.Json.JsonExtensionData]
        [System.Text.Json.Serialization.JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; }
        [Newtonsoft.Json.JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }
        [Newtonsoft.Json.JsonProperty("xrefmap")]
        [System.Text.Json.Serialization.JsonPropertyName("xrefmap")]
        public string XRefmap { get; set; }
    }
    public class ManifestItem
    {
        public ManifestItem() { }
        [Newtonsoft.Json.JsonProperty("group")]
        [System.Text.Json.Serialization.JsonPropertyName("group")]
        public string Group { get; set; }
        [Newtonsoft.Json.JsonExtensionData]
        [System.Text.Json.Serialization.JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; }
        [Newtonsoft.Json.JsonProperty("output")]
        [System.Text.Json.Serialization.JsonPropertyName("output")]
        public System.Collections.Generic.Dictionary<string, Docfx.Plugins.OutputFileInfo> Output { get; }
        [Newtonsoft.Json.JsonProperty("source_relative_path")]
        [System.Text.Json.Serialization.JsonPropertyName("source_relative_path")]
        public string SourceRelativePath { get; set; }
        [Newtonsoft.Json.JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; }
        [Newtonsoft.Json.JsonProperty("version")]
        [System.Text.Json.Serialization.JsonPropertyName("version")]
        public string Version { get; set; }
    }
    public class MarkupResult
    {
        public MarkupResult() { }
        public System.Collections.Immutable.ImmutableArray<string> Dependency { get; set; }
        public System.Collections.Immutable.ImmutableDictionary<string, System.Collections.Immutable.ImmutableList<Docfx.Plugins.LinkSourceInfo>> FileLinkSources { get; set; }
        public string Html { get; set; }
        public System.Collections.Immutable.ImmutableArray<string> LinkToFiles { get; set; }
        public System.Collections.Immutable.ImmutableHashSet<string> LinkToUids { get; set; }
        public System.Collections.Immutable.ImmutableDictionary<string, System.Collections.Immutable.ImmutableList<Docfx.Plugins.LinkSourceInfo>> UidLinkSources { get; set; }
        public System.Collections.Immutable.ImmutableDictionary<string, object> YamlHeader { get; set; }
        public Docfx.Plugins.MarkupResult Clone() { }
    }
    public class OutputFileInfo
    {
        public OutputFileInfo() { }
        [Newtonsoft.Json.JsonProperty("link_to_path")]
        [System.Text.Json.Serialization.JsonPropertyName("link_to_path")]
        public string LinkToPath { get; set; }
        [Newtonsoft.Json.JsonExtensionData]
        [System.Text.Json.Serialization.JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; }
        [Newtonsoft.Json.JsonProperty("relative_path")]
        [System.Text.Json.Serialization.JsonPropertyName("relative_path")]
        public string RelativePath { get; set; }
    }
    public enum PageChangeFrequency
    {
        Always = 0,
        Hourly = 1,
        Daily = 2,
        Weekly = 3,
        Monthly = 4,
        Yearly = 5,
        Never = 6,
    }
    public enum ProcessingPriority
    {
        NotSupported = -1,
        Lowest = 0,
        Low = 64,
        BelowNormal = 128,
        Normal = 256,
        AboveNormal = 512,
        High = 1024,
        Highest = 2147483647,
    }
    public class RootedFileAbstractLayer : Docfx.Plugins.IFileAbstractLayer
    {
        public RootedFileAbstractLayer(Docfx.Plugins.IFileAbstractLayer impl) { }
        public void Copy(string sourceFileName, string destFileName) { }
        public System.IO.Stream Create(string file) { }
        public bool Exists(string file) { }
        public System.Collections.Generic.IEnumerable<string> GetAllInputFiles() { }
        public string GetExpectedPhysicalPath(string file) { }
        public string GetPhysicalPath(string file) { }
        public System.IO.Stream OpenRead(string file) { }
    }
    public class SaveResult
    {
        public SaveResult() { }
        public string DocumentType { get; set; }
        public System.Collections.Immutable.ImmutableArray<Docfx.Plugins.XRefSpec> ExternalXRefSpecs { get; set; }
        public System.Collections.Immutable.ImmutableDictionary<string, System.Collections.Immutable.ImmutableList<Docfx.Plugins.LinkSourceInfo>> FileLinkSources { get; set; }
        public string FileWithoutExtension { get; set; }
        public System.Collections.Immutable.ImmutableArray<string> LinkToFiles { get; set; }
        public System.Collections.Immutable.ImmutableHashSet<string> LinkToUids { get; set; }
        public string ResourceFile { get; set; }
        public System.Collections.Immutable.ImmutableDictionary<string, System.Collections.Immutable.ImmutableList<Docfx.Plugins.LinkSourceInfo>> UidLinkSources { get; set; }
        public System.Collections.Immutable.ImmutableArray<Docfx.Plugins.XRefSpec> XRefSpecs { get; set; }
    }
    public class SitemapElementOptions
    {
        public SitemapElementOptions() { }
        [Newtonsoft.Json.JsonProperty("baseUrl")]
        [System.Text.Json.Serialization.JsonPropertyName("baseUrl")]
        public string BaseUrl { get; set; }
        [Newtonsoft.Json.JsonProperty("changefreq")]
        [System.Text.Json.Serialization.JsonPropertyName("changefreq")]
        public Docfx.Plugins.PageChangeFrequency? ChangeFrequency { get; set; }
        [Newtonsoft.Json.JsonProperty("lastmod")]
        [System.Text.Json.Serialization.JsonPropertyName("lastmod")]
        public System.DateTime? LastModified { get; set; }
        [Newtonsoft.Json.JsonProperty("priority")]
        [System.Text.Json.Serialization.JsonPropertyName("priority")]
        public double? Priority { get; set; }
    }
    public class SitemapOptions : Docfx.Plugins.SitemapElementOptions
    {
        public SitemapOptions() { }
        [Newtonsoft.Json.JsonProperty("fileOptions")]
        [System.Text.Json.Serialization.JsonPropertyName("fileOptions")]
        public System.Collections.Generic.Dictionary<string, Docfx.Plugins.SitemapElementOptions> FileOptions { get; set; }
    }
    public class SourceFileInfo
    {
        public SourceFileInfo() { }
        public string DocumentType { get; }
        public string SourceRelativePath { get; }
        public static Docfx.Plugins.SourceFileInfo FromManifestItem(Docfx.Plugins.ManifestItem manifestItem) { }
    }
    public class TocInfo
    {
        public TocInfo() { }
        public int Order { get; init; }
        public string TocFileKey { get; init; }
    }
    public class TreeItem
    {
        public TreeItem() { }
        [Newtonsoft.Json.JsonProperty("items")]
        [System.Text.Json.Serialization.JsonPropertyName("items")]
        public System.Collections.Generic.List<Docfx.Plugins.TreeItem> Items { get; set; }
        [Newtonsoft.Json.JsonExtensionData]
        [System.Text.Json.Serialization.JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; }
    }
    public enum TreeItemActionType
    {
        ReplaceSelf = 0,
        DeleteSelf = 1,
        AppendChild = 2,
        PrependChild = 3,
        InsertAfter = 4,
        InsertBefore = 5,
    }
    public enum TreeItemKeyType
    {
        TopicUid = 0,
        TopicHref = 1,
    }
    public class TreeItemRestructure
    {
        public TreeItemRestructure() { }
        public Docfx.Plugins.TreeItemActionType ActionType { get; set; }
        public string Key { get; set; }
        public System.Collections.Immutable.IImmutableList<Docfx.Plugins.TreeItem> RestructuredItems { get; set; }
        public System.Collections.Immutable.IImmutableList<Docfx.Plugins.FileAndType> SourceFiles { get; set; }
        public Docfx.Plugins.TreeItemKeyType TypeOfKey { get; set; }
    }
    public class UidDefinition
    {
        [Newtonsoft.Json.JsonConstructor]
        [System.Text.Json.Serialization.JsonConstructor]
        public UidDefinition(string name, string file, int? line = default, int? column = default, string path = null) { }
        [Newtonsoft.Json.JsonProperty("column")]
        [System.Text.Json.Serialization.JsonPropertyName("column")]
        public int? Column { get; }
        [Newtonsoft.Json.JsonProperty("file")]
        [System.Text.Json.Serialization.JsonPropertyName("file")]
        public string File { get; }
        [Newtonsoft.Json.JsonProperty("line")]
        [System.Text.Json.Serialization.JsonPropertyName("line")]
        public int? Line { get; }
        [Newtonsoft.Json.JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; }
        [Newtonsoft.Json.JsonProperty("path")]
        [System.Text.Json.Serialization.JsonPropertyName("path")]
        public string Path { get; }
    }
    public sealed class XRefSpec : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IDictionary<string, object>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.IEnumerable
    {
        public const string CommentIdKey = "commentId";
        public const string HrefKey = "href";
        public const string IsSpecKey = "isSpec";
        public const string NameKey = "name";
        public const string UidKey = "uid";
        public XRefSpec() { }
        public XRefSpec(Docfx.Plugins.XRefSpec spec) { }
        public XRefSpec(System.Collections.Generic.IDictionary<string, object> dictionary) { }
        public string CommentId { get; set; }
        public int Count { get; }
        public string Href { get; set; }
        public bool IsReadOnly { get; }
        public bool IsSpec { get; set; }
        public object this[string key] { get; set; }
        public System.Collections.Generic.ICollection<string> Keys { get; }
        public string Name { get; set; }
        public string Uid { get; set; }
        public System.Collections.Generic.ICollection<object> Values { get; }
        public void Add(string key, object value) { }
        public void Clear() { }
        public bool ContainsKey(string key) { }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>> GetEnumerator() { }
        public bool Remove(string key) { }
        public Docfx.Plugins.XRefSpec ToReadOnly() { }
        public bool TryGetValue(string key, out object value) { }
        public static Docfx.Plugins.XRefSpec Merge(Docfx.Plugins.XRefSpec left, Docfx.Plugins.XRefSpec right) { }
        public static Docfx.Plugins.XRefSpec operator +(Docfx.Plugins.XRefSpec left, Docfx.Plugins.XRefSpec right) { }
    }
}
namespace Docfx.YamlSerialization
{
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class ExtensibleMemberAttribute : System.Attribute
    {
        public ExtensibleMemberAttribute() { }
        public ExtensibleMemberAttribute(string prefix) { }
        public string Prefix { get; }
    }
    [System.Flags]
    public enum SerializationOptions
    {
        None = 0,
        Roundtrip = 1,
        DisableAliases = 2,
        EmitDefaults = 4,
        JsonCompatible = 8,
        DefaultToStaticType = 16,
    }
    public sealed class YamlDeserializer
    {
        public YamlDeserializer(YamlDotNet.Serialization.IObjectFactory objectFactory = null, YamlDotNet.Serialization.INamingConvention namingConvention = null, bool ignoreUnmatched = false, bool ignoreNotFoundAnchor = true) { }
        public System.Collections.Generic.IList<YamlDotNet.Serialization.INodeDeserializer> NodeDeserializers { get; }
        public System.Collections.Generic.IList<YamlDotNet.Serialization.INodeTypeResolver> TypeResolvers { get; }
        public YamlDotNet.Serialization.IValueDeserializer ValueDeserializer { get; }
        public object Deserialize(System.IO.TextReader input, YamlDotNet.Serialization.IValueDeserializer deserializer = null) { }
        public object Deserialize(YamlDotNet.Core.IParser reader, YamlDotNet.Serialization.IValueDeserializer deserializer = null) { }
        public object Deserialize(System.IO.TextReader input, System.Type type, YamlDotNet.Serialization.IValueDeserializer deserializer = null) { }
        public object Deserialize(YamlDotNet.Core.IParser parser, System.Type type, YamlDotNet.Serialization.IValueDeserializer deserializer = null) { }
        public T Deserialize<T>(System.IO.TextReader input, YamlDotNet.Serialization.IValueDeserializer deserializer = null) { }
        public T Deserialize<T>(YamlDotNet.Core.IParser reader, YamlDotNet.Serialization.IValueDeserializer deserializer = null) { }
        public void RegisterTagMapping(string tag, System.Type type) { }
        public void RegisterTypeConverter(YamlDotNet.Serialization.IYamlTypeConverter typeConverter) { }
    }
    public class YamlSerializer
    {
        public YamlSerializer(Docfx.YamlSerialization.SerializationOptions options = 0, YamlDotNet.Serialization.INamingConvention namingConvention = null) { }
        public void Serialize(System.IO.TextWriter writer, object graph) { }
        public void Serialize(YamlDotNet.Core.IEmitter emitter, object graph) { }
        public void SerializeValue(YamlDotNet.Core.IEmitter emitter, object value, System.Type type) { }
    }
}
namespace Docfx.YamlSerialization.NodeDeserializers
{
    public class EmitArrayNodeDeserializer : YamlDotNet.Serialization.INodeDeserializer
    {
        public EmitArrayNodeDeserializer() { }
        public static TItem[] DeserializeHelper<TItem>(YamlDotNet.Core.IParser reader, System.Type expectedType, System.Func<YamlDotNet.Core.IParser, System.Type, object> nestedObjectDeserializer) { }
    }
    public class EmitGenericCollectionNodeDeserializer : YamlDotNet.Serialization.INodeDeserializer
    {
        public EmitGenericCollectionNodeDeserializer(YamlDotNet.Serialization.IObjectFactory objectFactory) { }
        public static void DeserializeHelper<TItem>(YamlDotNet.Core.IParser reader, System.Type expectedType, System.Func<YamlDotNet.Core.IParser, System.Type, object> nestedObjectDeserializer, System.Collections.Generic.ICollection<TItem> result) { }
    }
    public class EmitGenericDictionaryNodeDeserializer : YamlDotNet.Serialization.INodeDeserializer
    {
        public EmitGenericDictionaryNodeDeserializer(YamlDotNet.Serialization.IObjectFactory objectFactory) { }
        public static void DeserializeHelper<TKey, TValue>(YamlDotNet.Core.IParser reader, System.Type expectedType, System.Func<YamlDotNet.Core.IParser, System.Type, object> nestedObjectDeserializer, System.Collections.Generic.IDictionary<TKey, TValue> result) { }
    }
    public sealed class ExtensibleObjectNodeDeserializer : YamlDotNet.Serialization.INodeDeserializer
    {
        public ExtensibleObjectNodeDeserializer(YamlDotNet.Serialization.IObjectFactory objectFactory, YamlDotNet.Serialization.ITypeInspector typeDescriptor, bool ignoreUnmatched) { }
    }
}
namespace Docfx.YamlSerialization.ObjectDescriptors
{
    public class BetterObjectDescriptor : YamlDotNet.Serialization.IObjectDescriptor
    {
        public BetterObjectDescriptor(object value, System.Type type, System.Type staticType) { }
        public BetterObjectDescriptor(object value, System.Type type, System.Type staticType, YamlDotNet.Core.ScalarStyle scalarStyle) { }
        public YamlDotNet.Core.ScalarStyle ScalarStyle { get; }
        public System.Type StaticType { get; }
        public System.Type Type { get; }
        public object Value { get; }
    }
}
namespace Docfx.YamlSerialization.ObjectFactories
{
    public class DefaultEmitObjectFactory : YamlDotNet.Serialization.ObjectFactories.ObjectFactoryBase
    {
        public DefaultEmitObjectFactory() { }
        public override object Create(System.Type type) { }
    }
}
namespace Docfx.YamlSerialization.ObjectGraphTraversalStrategies
{
    public class FullObjectGraphTraversalStrategy : YamlDotNet.Serialization.IObjectGraphTraversalStrategy
    {
        public FullObjectGraphTraversalStrategy(Docfx.YamlSerialization.YamlSerializer serializer, YamlDotNet.Serialization.ITypeInspector typeDescriptor, YamlDotNet.Serialization.ITypeResolver typeResolver, int maxRecursion, YamlDotNet.Serialization.INamingConvention namingConvention) { }
        protected Docfx.YamlSerialization.YamlSerializer Serializer { get; }
        protected virtual void Traverse<TContext>(YamlDotNet.Serialization.IObjectDescriptor value, YamlDotNet.Serialization.IObjectGraphVisitor<TContext> visitor, int currentDepth, TContext context) { }
        protected virtual void TraverseDictionary<TContext>(YamlDotNet.Serialization.IObjectDescriptor dictionary, object visitor, int currentDepth, object context) { }
        protected virtual void TraverseObject<TContext>(YamlDotNet.Serialization.IObjectDescriptor value, YamlDotNet.Serialization.IObjectGraphVisitor<TContext> visitor, int currentDepth, TContext context) { }
        protected virtual void TraverseProperties<TContext>(YamlDotNet.Serialization.IObjectDescriptor value, object visitor, int currentDepth, object context) { }
        public static void TraverseGenericDictionaryHelper<TKey, TValue, TContext>(Docfx.YamlSerialization.ObjectGraphTraversalStrategies.FullObjectGraphTraversalStrategy self, System.Collections.Generic.IDictionary<TKey, TValue> dictionary, object visitor, int currentDepth, YamlDotNet.Serialization.INamingConvention namingConvention, object context) { }
    }
    public class RoundtripObjectGraphTraversalStrategy : Docfx.YamlSerialization.ObjectGraphTraversalStrategies.FullObjectGraphTraversalStrategy
    {
        public RoundtripObjectGraphTraversalStrategy(Docfx.YamlSerialization.YamlSerializer serializer, YamlDotNet.Serialization.ITypeInspector typeDescriptor, YamlDotNet.Serialization.ITypeResolver typeResolver, int maxRecursion) { }
        protected override void TraverseProperties<TContext>(YamlDotNet.Serialization.IObjectDescriptor value, object visitor, int currentDepth, object context) { }
    }
}
namespace Docfx.YamlSerialization.TypeInspectors
{
    public class EmitTypeInspector : Docfx.YamlSerialization.TypeInspectors.ExtensibleTypeInspectorSkeleton
    {
        public EmitTypeInspector(YamlDotNet.Serialization.ITypeResolver resolver) { }
        public override System.Collections.Generic.IEnumerable<YamlDotNet.Serialization.IPropertyDescriptor> GetProperties(System.Type type, object container) { }
        public override YamlDotNet.Serialization.IPropertyDescriptor GetProperty(System.Type type, object container, string name) { }
    }
    public sealed class ExtensibleNamingConventionTypeInspector : Docfx.YamlSerialization.TypeInspectors.ExtensibleTypeInspectorSkeleton
    {
        public ExtensibleNamingConventionTypeInspector(Docfx.YamlSerialization.TypeInspectors.IExtensibleTypeInspector innerTypeDescriptor, YamlDotNet.Serialization.INamingConvention namingConvention) { }
        public override System.Collections.Generic.IEnumerable<YamlDotNet.Serialization.IPropertyDescriptor> GetProperties(System.Type type, object container) { }
        public override YamlDotNet.Serialization.IPropertyDescriptor GetProperty(System.Type type, object container, string name) { }
    }
    public sealed class ExtensibleReadableAndWritablePropertiesTypeInspector : Docfx.YamlSerialization.TypeInspectors.ExtensibleTypeInspectorSkeleton
    {
        public ExtensibleReadableAndWritablePropertiesTypeInspector(Docfx.YamlSerialization.TypeInspectors.IExtensibleTypeInspector innerTypeDescriptor) { }
        public override System.Collections.Generic.IEnumerable<YamlDotNet.Serialization.IPropertyDescriptor> GetProperties(System.Type type, object container) { }
        public override YamlDotNet.Serialization.IPropertyDescriptor GetProperty(System.Type type, object container, string name) { }
    }
    public abstract class ExtensibleTypeInspectorSkeleton : Docfx.YamlSerialization.TypeInspectors.IExtensibleTypeInspector, YamlDotNet.Serialization.ITypeInspector
    {
        protected ExtensibleTypeInspectorSkeleton() { }
        public abstract System.Collections.Generic.IEnumerable<YamlDotNet.Serialization.IPropertyDescriptor> GetProperties(System.Type type, object container);
        public virtual YamlDotNet.Serialization.IPropertyDescriptor GetProperty(System.Type type, object container, string name) { }
        public YamlDotNet.Serialization.IPropertyDescriptor GetProperty(System.Type type, object container, string name, bool ignoreUnmatched) { }
    }
    public sealed class ExtensibleYamlAttributesTypeInspector : Docfx.YamlSerialization.TypeInspectors.ExtensibleTypeInspectorSkeleton
    {
        public ExtensibleYamlAttributesTypeInspector(Docfx.YamlSerialization.TypeInspectors.IExtensibleTypeInspector innerTypeDescriptor) { }
        public override System.Collections.Generic.IEnumerable<YamlDotNet.Serialization.IPropertyDescriptor> GetProperties(System.Type type, object container) { }
        public override YamlDotNet.Serialization.IPropertyDescriptor GetProperty(System.Type type, object container, string name) { }
    }
    public interface IExtensibleTypeInspector : YamlDotNet.Serialization.ITypeInspector
    {
        YamlDotNet.Serialization.IPropertyDescriptor GetProperty(System.Type type, object container, string name);
    }
}